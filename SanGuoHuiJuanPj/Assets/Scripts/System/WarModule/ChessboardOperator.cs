using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using Microsoft.Extensions.Logging;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋盘处理器。主要处理<see cref="IChessOperator"/>的交互与进程逻辑。
    /// </summary>
    public abstract class ChessboardOperator 
    {
        protected enum ActivityReference
        {
            RoundStart,
            ChessActivity,
            Inner,
            RoundEnd
        }
        protected ActivityReference ActivityRef { get; private set; }
        public const int HeroDodgeLimit = 75;
        public const int HeroArmorLimit = 90;
        public ChessGrid Grid { get; }

        public IReadOnlyList<ChessRound> Rounds => rounds;
        private ChessRound currentRound;
        private Activity currentActivity;
        protected abstract Dictionary<ChessOperator,ChessStatus> StatusMap { get; }
        public IEnumerable<TerrainSprite> ChessSprites => Sprites;
        protected abstract List<TerrainSprite> Sprites { get; }

        public int ChallengerGold { get; protected set; }
        public int OpponentGold { get; protected set; }
        public List<int> ChallengerChests { get; set; } = new List<int>();
        public List<int> OpponentChests { get; set; } = new List<int>();
        protected abstract BondOperator[] JiBan { get; }
        
        public bool IsInit => ProcessSeed > 0;
        
        private static Random random = new Random();
        private int RecursiveActionCount
        {
            get => _recursiveActionCount;
            set
            {
                if (_recursiveActionCount > RecursiveActionsLimit)
                    throw new StackOverflowException($"{nameof(RecursiveActionCount)}>={nameof(RecursiveActionsLimit)}");
                _recursiveActionCount = value;
            }
        }
        private const int RecursiveActionsLimit = 99999;

        private readonly ChessPosProcess[] EmptyProcess = Array.Empty<ChessPosProcess>();

        private readonly List<ChessRound> rounds;
        private List<ChessPosProcess> currentProcesses;
        private int ProcessSeed = 0;
        private int ActivitySeed = 0;
        private int ChessSpriteSeed = 0;
        private int _recursiveActionCount;
        public bool IsGameOver => IsChallengerWin || IsOppositeWin;
        public bool IsChallengerWin { get; private set; }
        public bool IsOppositeWin { get; private set; }
        private ILogger Logger { get; }
        /// <summary>
        /// 单体 一列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        protected static T[] Singular<T>(T t) => new[] {t};

        #region Helper
        /// <summary>
        /// 计算比率值(不包括原来的值)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private static float GetRatio(float value, int ratio) => ratio * 0.01f * value;
        #endregion

        protected ChessboardOperator(ChessGrid grid, ILogger log = null)
        {
            rounds = new List<ChessRound>();
            Grid = grid;
            Logger = log;
        }

        public ChessStatus GetStatus(IChessOperator op) => GetStatus(GetOperator(op.InstanceId));
        public ChessStatus GetStatus(ChessOperator op)
        {
            if (!StatusMap.ContainsKey(op))
                throw new KeyNotFoundException(
                    $"{nameof(ChessOperator)}.{nameof(GetStatus)}() Op instance = {op.InstanceId} status not found!");
            return StatusMap[op];
        }

        public int GetCondition(IChessOperator op, CardState.Cons con) => GetCondition(GetOperator(op.InstanceId), con);
        public int GetCondition(ChessOperator op,CardState.Cons con)
        {
            return GetStatus(op).GetBuff(con) +
                   GetChessPos(op).Terrain.GetServed(con, op);
        }

        protected void Log(string message) => Logger?.Log(LogLevel.Information, message);
        public ChessRound StartRound()
        {
            if (IsGameOver) return null;
            //instance Round
            //invoke pre-action
            //Get all sorted this operators
            //invoke this operations
            //invoke finalization
            RecursiveActionCount = 0;
            ActivatedJiBan.Clear();
            currentRound = new ChessRound
            {
                InstanceId = Rounds.Count,
                PreAction = new RoundAction(),
                FinalAction = new RoundAction(),
                ChallengerJiBans = new List<int>(),
                OppositeJiBans = new List<int>()
            };
            Log($"开始回合[{currentRound.InstanceId}]");
            var currentOps = StatusMap.Where(o => !o.Value.IsDeath).Select(o => o.Key).ToList();
            currentProcesses = new List<ChessPosProcess>();
            RefreshChessPosses();
            ActivityRef = ActivityReference.RoundStart;
            GetPreRoundTriggerByOperators();
            ActivityRef = ActivityReference.ChessActivity;
            do
            {
                var op = GetSortedOperator(currentOps); //根据排列逻辑获取执行代理
                if (op == null) break;
                if (GetStatus(op).IsDeath)
                {
                    currentOps.Remove(op);
                    continue;
                }

                InstanceProcess(GetStatus(op).Pos, op.IsChallenger);

                Log(OperatorText(op.InstanceId));
                op.MainActivity();

                currentOps.Remove(op);
                CheckIsGameOver();
                if(IsGameOver) break;
                //移除死亡的棋子
                foreach (var death in StatusMap.Where(o => o.Value.IsDeath && o.Value.Pos >= 0))
                {
                    var deathOp = death.Key;
                    PosOperator(deathOp, -1);
                }

            } while (currentOps.Count > 0);
            currentRound.Processes = currentProcesses.ToArray();
            if (IsGameOver)
            {
                var winner = string.Empty;
                if (IsChallengerWin)
                    winner += "玩家胜利!";
                if (IsOppositeWin)
                    winner += "对方胜利!";
                Log($"{winner}");
                return currentRound;
            }
            ActivityRef = ActivityReference.RoundEnd;
            GetRoundEndTriggerByOperators();

            return currentRound;
        }

        private void RefreshChessPosses()
        {
            foreach (var pos in Grid.Challenger.Concat(Grid.Opposite).Select(c => c.Value))
            {
                if (!pos.IsPostedAlive) continue;
                var o = GetOperator(pos.Operator.InstanceId);
                var status = GetStatus(o);
                if (status.Pos != pos.Pos)
                    PosOperator(o,pos.Pos);
                UpdateTerrain(pos);
            }
        }

        private void InstanceProcess(int pos, bool isChallenger)
        {
            CurrentProcess = ChessPosProcess.Instance(ProcessSeed, pos, isChallenger);
            currentProcesses.Add(CurrentProcess);
            ProcessSeed++;
            Log($"生成{CurrentProcess}");
        }

        private void CheckIsGameOver()
        {
            IsOppositeWin = Grid.Challenger.Values
                .Where(p => p.Operator != null && p.Operator.CardType == GameCardType.Base).All(p => GetStatus(p.Operator).IsDeath);
            IsChallengerWin = Grid.Opposite.Values
                .Where(p => p.Operator != null && p.Operator.CardType == GameCardType.Base).All(p => GetStatus(p.Operator).IsDeath);
        }

        #region RoundActivities

        protected abstract void GetRoundEndTriggerByOperators();

        protected abstract void GetPreRoundTriggerByOperators();

        protected bool JiBanActivation(BondOperator jb, ChessOperator[] list)
        {
            var map = new Dictionary<int, bool>();
            foreach (var jId in jb.BondList)
                map.Add(jId, list.Any(o => o.CardId == jId));
            var isActivate = map.All(m => m.Value);
            if(isActivate)
            {
                var op = list.First();
                ActivatedJiBan.Add(new JiBanController
                {
                    Operator = jb,
                    IsChallenger = op.IsChallenger,
                    Chessmen = list.Where(o=>o.CardType == GameCardType.Hero)
                        .Join(map,o=>o.CardId,m=>m.Key,(o,_)=>o).ToList()
                });
                if (op.IsChallenger)
                    currentRound.ChallengerJiBans.Add(jb.BondId);
                else
                    currentRound.OppositeJiBans.Add(jb.BondId);
            }
            return isActivate;
        }

        /// <summary>
        /// 回合结束加金币
        /// </summary>
        /// <param name="op"></param>
        /// <param name="toChallenger"></param>
        /// <param name="resourceId">金币-1，宝箱id=正数</param>
        /// <param name="value"></param>
        public void RegResources(ChessOperator op, bool toChallenger, int resourceId, int value)
        {
            InstanceActivity(op.IsChallenger, op, toChallenger ? -1 : -2, Activity.PlayerResource,
                Singular(CombatConduct.InstancePlayerResource(resourceId, value)), 0, -1);
            if (resourceId == -1)
                if (toChallenger)
                    ChallengerGold += value;
                else OpponentGold += value;
            if (resourceId >= 0)
            {
                if (toChallenger)
                    ChallengerChests.AddRange(WarChests(resourceId, value));
                else OpponentChests.AddRange(WarChests(resourceId, value));
            }

            ActivitySeed++;

            List<int> WarChests(int id, int amt)
            {
                var list = new List<int>();
                for (int i = 0; i < amt; i++)
                {
                    list.Add(id);
                }

                return list;
            }
        }

        #endregion

        private ChessPosProcess CurrentProcess { get; set; }

        protected abstract ChessOperator GetOperator(int id);
        //执行代理的排列逻辑
        private ChessOperator GetSortedOperator(IEnumerable<ChessOperator> list) =>
            list.Where(o => !GetStatus(o).IsDeath)
                .OrderBy(o => GetStatus(o).Pos)
                .ThenByDescending(o => o.IsChallenger)
                .FirstOrDefault();

        /// <summary>
        /// 棋盘指令，非棋子执行类，一般用于羁绊，buff
        /// </summary>
        /// <param name="fromChallenger"></param>
        /// <param name="target"></param>
        /// <param name="intent">参考<see cref="RoundAction"/>的值来表示</param>
        /// <param name="conducts"></param>
        /// <param name="skill"></param>
        /// <param name="rePos"></param>
        public void InstanceChessboardActivity(bool fromChallenger, IChessOperator target, int intent,
            CombatConduct[] conducts, int skill = 0, int rePos = -1)
        {
            var activity = InstanceActivity(fromChallenger, target.InstanceId, intent, conducts, skill, rePos);
            ProcessActivityResult(activity);
        }

        /// <summary>
        /// 棋子指令，为当前的行动添加上一个行动指令
        /// </summary>
        /// <param name="offender"></param>
        /// <param name="target"></param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="skill">如果是普通攻击，标记0，大于0将会是技能值</param>
        /// <param name="rePos"></param>
        /// <returns></returns>
        public void AppendOpInnerActivity(ChessOperator offender, IChessPos target, int intent,
            CombatConduct[] conducts, int skill, int rePos = -1)
        {
            if (CurrentProcess == null) return;
            if (currentActivity == null)
            {
                ActivityRef = ActivityReference.ChessActivity;
                AppendOpActivity(offender, target, intent, conducts, skill, rePos);
                return;
            }
            var temp = ActivityRef;
            ActivityRef = ActivityReference.Inner;
            var activity = InstanceActivity(offender.IsChallenger, offender, target.Operator.InstanceId, intent,
                conducts, skill, rePos);
            ActivityRef = temp;
            ProcessActivityResult(activity);
        }

        private string OperatorText(int id)
        {
            var op = GetOperator(id);
            var stat = GetStatus(op);
            return $"{op.InstanceId}.{op}[{stat.Hp}/{stat.MaxHp}]Pos({stat.Pos})";
        }

        private void ProcessActivityResult(Activity activity)
        {
            var target = GetOperator(activity.To);
            var offender = activity.From < 0 ? null : GetOperator(activity.From);
            Log($"-->{OperatorText(target.InstanceId)}");
            activity.Result = GetOperator(target.InstanceId).Respond(activity, offender);
            activity.OffenderStatus = GetStatus(target).Clone();
            Log(activity.ToString());
            Log(activity.Result.ToString());
            UpdateTerrain(GetChessPos(target));
        }

        public ActivityResult AppendOpActivity(ChessOperator offender, IChessPos target, int intent,
            CombatConduct[] conducts,int skill ,int rePos = -1)
        {
            if (target == null) return null;
            currentActivity = InstanceActivity(offender.IsChallenger, offender, target.Operator.InstanceId, intent,
                conducts, skill, rePos);
            var op = GetOperator(target.Operator.InstanceId);
            if (op == null)
                throw new NullReferenceException(
                    $"Target Pos({target.Pos}) is null! from offender Pos({GetStatus(offender).Pos}) as IsChallenger[{offender?.IsChallenger}] type[{offender?.GetType().Name}]");
            ProcessActivityResult(currentActivity);
            return currentActivity.Result;
        }

        private Activity InstanceActivity(bool fromChallenger, int targetInstance, int intent, CombatConduct[] conducts,
            int skill, int rePos) =>
            InstanceActivity(fromChallenger,null, targetInstance, intent, conducts, skill, rePos);

        private Activity InstanceActivity(bool fromChallenger, ChessOperator offender, int targetInstance, int intent,
            CombatConduct[] conducts, int skill, int rePos)
        {

            RecursiveActionCount++;
            ActivitySeed++;
            var processId = ActivityRef == ActivityReference.ChessActivity ||
                            ActivityRef == ActivityReference.Inner
                ? CurrentProcess.InstanceId
                : -1;
            var fromId = offender == null ? fromChallenger ? -1 : -2 : offender.InstanceId;
            var activity = Activity.Instance(
                ActivitySeed,
                processId,
                fromId,
                fromChallenger ? 0 : 1,
                targetInstance,
                intent, conducts, skill, rePos);
            switch (ActivityRef)
            {
                case ActivityReference.RoundStart:
                    currentRound.PreAction.Activities.Add(activity);
                    break;
                case ActivityReference.ChessActivity:
                    CurrentProcess.Activities.Add(activity);
                    break;
                case ActivityReference.RoundEnd:
                    currentRound.FinalAction.Activities.Add(activity);
                    break;
                case ActivityReference.Inner:
                {
                    if (currentActivity == null)
                    {

                    }
                    currentActivity.Inner.Add(activity);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //Log($"生成{activity}");
            return activity;
        }

        //private List<Activity> InnerActivities { get; set; }

        /// <summary>
        /// 生成棋盘精灵
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="lasting"></param>
        /// <param name="value"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public T InstanceSprite<T>(IChessPos target, TerrainSprite.LastingType lasting ,int typeId, int value)
            where T : TerrainSprite, new()
        {
            var sprite =
                TerrainSprite.Instance<T>(ChessSpriteSeed,
                    lasting: lasting,
                    value: value,
                    pos: target.Pos,
                    typeId: typeId,
                    isChallenger: target.IsChallenger);
            RegSprite(sprite);
            ChessSpriteSeed++;
            return sprite;
        }

        private Activity SpriteActivity(TerrainSprite sprite,bool isAdd)
        {
            var conduct = isAdd
                ? CombatConduct.AddSprite(sprite.Value, sprite.InstanceId, sprite.TypeId)
                : CombatConduct.RemoveSprite(sprite.InstanceId);
            var activity = Activity.Instance(
                id: ActivitySeed,
                processId: CurrentProcess.InstanceId, 
                @from: sprite.IsChallenger ? -1 : -2,
                isChallenger: sprite.IsChallenger ? 0 : 1, 
                to: sprite.Pos,
                intent: Activity.Sprite,
                conducts: Helper.Singular(conduct),
                skill: 1,
                rePos: -1);
            ActivitySeed++;
            return activity;
        }
        public void RegSprite(TerrainSprite sprite)
        {
            Log($"添加{sprite}");
            var activity = SpriteActivity(sprite, true);
            activity.Result = ActivityResult.Instance(ActivityResult.Types.Undefined);
            CurrentProcess.Activities.Add(activity);
            Sprites.Add(sprite);
            Grid.GetScope(sprite.IsChallenger)[sprite.Pos].Terrain.AddSprite(sprite);
        }

        public void RemoveSprite(TerrainSprite sprite)
        {
            Log($"移除{sprite}");
            var activity = SpriteActivity(sprite, false);
            activity.Result = ActivityResult.Instance(ActivityResult.Types.Undefined);
            CurrentProcess.Activities.Add(activity);
            Sprites.Remove(sprite);
            Grid.GetScope(sprite.IsChallenger)[sprite.Pos].Terrain.RemoveSprite(sprite);
        }

        public int GetHeroBuffDamage(ChessOperator op)
        {
            var ratio = GetCondition(op, CardState.Cons.StrengthUp);
            return (int)(op.GetStrength + op.GetStrength * 0.01f * ratio);
        }
        /// <summary>
        /// 完全动态伤害=进攻方伤害转化(buff,羁绊)
        /// todo 注意这个已经包括羁绊，羁绊获取伤害不可以透过这个方法，否则无限循环
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public int GetCompleteDamageWithBond(ChessOperator op)
        {
            var damage = GetHeroBuffDamage(op);
            var bonds = ActivatedJiBan
                .Where(j => j.IsChallenger == op.IsChallenger
                            && j.Chessmen.Contains(op)).ToList();
            if (bonds.Count > 0)
                damage += bonds.Sum(b => b.Operator.OnDamageAddOn(b.Chessmen.ToArray(), op, damage));
            return damage;
        }

        private void UpdateTerrain(IChessPos pos)
        {
            foreach (var sprite in pos.Terrain.Sprites.Where(s => s.Lasting == TerrainSprite.LastingType.Relation).ToArray())
            {
                if (GetOperator(sprite.Value).IsAlive) continue;
                RemoveSprite(sprite);
            }
        }

        public bool OnCounterTriggerPass(ChessOperator op,ChessOperator offender)
        {
            if (GetStatus(offender).IsDeath ||
                GetStatus(op).IsDeath ||
                offender.Style.Type == CombatStyle.Types.Range ||
                offender.Style.ArmedType < 0 ||
                op.Style.Type == CombatStyle.Types.Range)
                return false;
            return !GetBuffOperator(b => b.IsDisableCounter(op)).Any();
        }

        public bool OnDodgeTriggerPass(ChessOperator op, ChessOperator offender)
        {
            if (op.CardType != GameCardType.Hero) return false;
            var rate = op.GetDodgeRate() + GetCondition(op, CardState.Cons.DodgeUp);
            foreach (var bo in GetBuffOperator(o=>o.IsDodgeRateTrigger(op))) 
                rate += bo.OnAppendDodgeRate(op, offender);
            rate = Math.Min(rate, HeroDodgeLimit);
            return IsRandomPass(rate);
        }

        private ActivityResult.Types DetermineSufferResult(Activity activity, ChessOperator op, ChessOperator offender)
        {
            if (GetCondition(op,CardState.Cons.Shield) > 0)
                return ActivityResult.Types.Shield;
            if (GetCondition(op,CardState.Cons.EaseShield) > 0) // 缓冲盾
                return ActivityResult.Types.EaseShield;
            return ActivityResult.Types.Suffer;
        }

        public int OnBuffingConvert(ChessOperator op, CombatConduct conduct)
        {
            if (op.CardType != GameCardType.Hero) return 0;
            var value = (int)conduct.Total;
            foreach (var bo in GetBuffOperator((CardState.Cons) conduct.Element, op))
            {
                 value = bo.OnBuffConvert(op, value);
                 if (value <= 0) return 0;
            }
            return value;
        }

        public bool OnInvincibleTrigger(ChessOperator op) => GetCondition(op, CardState.Cons.Invincible) > 0;

        public ActivityResult OnOffensiveActivity(Activity activity, ChessOperator op, IChessOperator off)
        {
            var offender = GetOperator(off.InstanceId);
            var result = ActivityResult.Instance(ActivityResult.Types.Suffer);
            //闪避判定
            if (offender.CardType == GameCardType.Hero &&
                OnDodgeTriggerPass(op, offender))
                result.Result = (int)ActivityResult.Types.Dodge;
            else
            {
                result.Result = (int)DetermineSufferResult(activity, op, offender);
                /***执行Activities***/
                op.ProceedActivity(activity);
            }

            result.Status = GetStatus(op).Clone();
            return result;
        }

        /// <summary>
        /// 盾类的过滤判定
        /// </summary>
        /// <param name="op"></param>
        /// <param name="activity"></param>
        /// <returns></returns>
        public IEnumerable<CombatConduct> OnShieldFilter(ChessOperator op, Activity activity)
        {
            var conducts = activity.Conducts;

            var shields = GetBuffOperator(CardState.Cons.Shield, op).ToArray();
            if (shields.Any())
                foreach (var bo in shields)
                {
                    conducts = bo.OnCombatConductFilter(conducts);
                    if (!conducts.Any()) return conducts;
                }

            return conducts;
        }

        public int OnHealConvert(ChessOperator op, CombatConduct conduct)
        {
            var value = (int)conduct.Total;
            foreach (var bo in GetBuffOperator(b => b.IsHealingTrigger(op)))
            {
                value = bo.OnHealingConvert(value);
                if (value <= 0) return 0;
            }
            return value;
        }

        /// <summary>
        /// 当被伤害的时候，伤害值转化
        /// </summary>
        /// <param name="op"></param>
        /// <param name="conduct"></param>
        /// <returns></returns>
        public void OnArmorReduction(ChessOperator op, CombatConduct conduct)
        {
            float armor = Damage.GetKind(conduct) == Damage.Kinds.Physical ? op.GetPhysicArmor() : op.GetMagicArmor();
            //加护甲
            armor += GetCondition(op, CardState.Cons.DefendUp);
            //卸甲状态
            if (GetBuffOperator(CardState.Cons.Disarmed, op).Any() || 
                GetCondition(op, CardState.Cons.Disarmed) > 0)
                armor *= 0.5f;
            conduct.TimesRate(1 - armor * 0.01f);
            //伤害转化buff 例如：流血
            foreach (var bo in GetBuffOperator(b => b.IsDamageConvertTrigger(op)))
                bo.OnDamageConvertTrigger(op, conduct);
        }

        public bool OnMainProcessAvailable(ChessOperator op)
        {
            var isDisable = false;
            isDisable = GetBuffOperator(b => b.IsMainActionTrigger && b.IsDisableMainAction(op)).Any();
            return !isDisable;
        }

        public bool OnHeroPerformAvailable(HeroOperator op)
        {
            var isDisable = false;
            isDisable = GetBuffOperator(b => b.IsHeroPerformTrigger && b.IsDisableHeroPerform(op)).Any();
            return !isDisable;
        }

        private IEnumerable<BuffOperator> GetBuffOperator(CardState.Cons con, ChessOperator op) =>
            GetBuffOperator(b => b.Buff == con && GetCondition(op, con) > 0);

        protected abstract IEnumerable<BuffOperator> GetBuffOperator(Func<BuffOperator, bool> func);

        /// <summary>
        /// 找出该回合中指定的对象
        /// </summary>
        /// <param name="op"></param>
        /// <param name="isSelf"></param>
        /// <returns></returns>
        private int ResourceTarget(ChessOperator op, bool isSelf)
        {
            var isChallenger = op.IsChallenger;
            if (isSelf) isChallenger = !isChallenger;
            return isChallenger ? -1 : -2;
        }

        /// <summary>
        /// 从数据表调出触发率，并给出随机判断
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool RandomFromConfigTable(int id) => IsRandomPass(DataTable.GetGameValue(id));

        /// <summary>
        /// 调出数值的百分比转化成点数
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public float ConfigPercentage(int id) => ConfigValue(id) * 0.01f;
        /// <summary>
        /// 调出数值表的值
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public float ConfigValue(int id) => DataTable.GetGameValue(id);

        /// <summary>
        /// Randomize
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool IsRandomPass(int ratio, int range = 100) => random.Next(0, range) <= ratio;

        /// <summary>
        /// Randomize
        /// </summary>
        /// <param name="excludedMax"></param>
        /// <returns></returns>
        public int Randomize(int excludedMax) => random.Next(excludedMax);

        public bool IsRouseDamagePass(ChessOperator op)
        {
            var ratio = 0;
            if (op.CardType == GameCardType.Hero)
                ratio += DataTable.Hero[op.CardId].RouseRatio;
            foreach (var bo in GetBuffOperator(b=>b.IsRouseRatioTrigger)) 
                ratio += bo.OnRouseRatio(op);
            return IsRandomPass(ratio);
        }

        public bool IsCriticalDamagePass(ChessOperator op)
        {
            var ratio = 0;
            if (op.CardType == GameCardType.Hero)
                ratio += DataTable.Hero[op.CardId].CriticalRatio;
            foreach (var bo in GetBuffOperator(b => b.IsCriticalRatioTrigger))
                ratio += bo.OnCriticalRatio(op);
            return IsRandomPass(ratio);
        }

        #region Grid Proxy
        public int[] FrontRows => Grid.FrontRows;
        /// <summary>
        /// 放置棋子到棋位上
        /// </summary>
        /// <param name="op"></param>
        /// <param name="pos"></param>
        public void PosOperator(ChessOperator op, int pos)
        {
            var oldPos = GetChessPos(op);
            if (oldPos != null)
            {
                Grid.Remove(GetStatus(op).Pos, op.IsChallenger);
                if (IsInit) UpdateTerrain(oldPos);
            }
            GetStatus(op).SetPos(pos);
            if (pos < 0) return;
            var replace = Grid.Replace(pos, op);
            if (replace != null)
                throw new InvalidOperationException(
                    $"Pos({pos}) has [{replace.CardId}({replace.CardType})] exist!");
            if (!IsInit) return;
            var chessPos = GetChessPos(op);
            op.OnPostingTrigger(chessPos);
            UpdateTerrain(chessPos);
        }

        public IChessPos GetChessPos(IChessOperator op) => Grid.GetChessPos(op, GetStatus(op).Pos);

        public IChessPos GetContraTarget(ChessOperator op,Func<IChessPos,bool> condition = null) => Grid.GetContraPositionInSequence(op, GetStatus(op).Pos, condition);

        public IEnumerable<IChessPos> GetAttackPath(ChessOperator op) => Grid.GetAttackPath(GetStatus(op).Pos).Select(i => Grid.GetChessPos(i, !op.IsChallenger));

        public IEnumerable<IChessPos> GetFriendlyNeighbors(ChessOperator op) => Grid.GetFriendlyNeighbors(op, GetStatus(op).Pos);
        
        public IEnumerable<IChessPos> GetContraNeighbors(ChessOperator op) => Grid.GetNeighbors(GetStatus(op).Pos, !op.IsChallenger);

        public IEnumerable<IChessPos> GetRivals(ChessOperator op, Func<IChessPos, bool> condition = null) =>
            Grid.GetRivalScope(op).Values.Where(condition == null ? p => p.IsPostedAlive : condition);

        public IEnumerable<IChessPos> GetNeighbors(IChessPos pos, bool includeUnPost, int surround= 1) => Grid.GetNeighbors(pos, includeUnPost, surround);
        public IEnumerable<IChessPos> GetFriendly(ChessOperator op, Func<IChessPos, bool> condition = null) =>
            Grid.GetScope(op).Values.Where(condition == null ? p => p.IsPostedAlive : condition);
        public IChessPos BackPos(IChessPos pos) => Grid.BackPos(pos);
        #endregion

        protected abstract void OnPlayerResourcesActivity(Activity activity);

        private List<JiBanController> ActivatedJiBan { get; } = new List<JiBanController>();
        private class JiBanController
        {
            public bool IsChallenger;
            public BondOperator Operator;
            public List<ChessOperator> Chessmen;
        }
    }
}