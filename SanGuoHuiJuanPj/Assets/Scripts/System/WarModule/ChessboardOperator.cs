using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋盘处理器。主要处理<see cref="IChessOperator"/>的交互与进程逻辑。
    /// </summary>
    public abstract class ChessboardOperator 
    {
        public const int HeroDodgeLimit = 75;
        public const int HeroArmorLimit = 90;
        public ChessGrid Grid { get; }

        private bool IsChallengerOdd => _isChallengerOdd;

        public IReadOnlyList<ChessRound> Rounds => rounds;
        private ChessRound currentRound;
        protected abstract Dictionary<ChessOperator,ChessStatus> StatusMap { get; }
        public IEnumerable<TerrainSprite> ChessSprites => Sprites;
        protected abstract List<TerrainSprite> Sprites { get; }
        public bool IsInit => ProcessSeed > 0;
        #region Static Fields
        private static Random random = new Random();
        private static readonly ChessPosProcess[] EmptyProcess = new ChessPosProcess[0];
        private readonly bool _isChallengerOdd;
        private readonly List<ChessRound> rounds;
        private static int ProcessSeed = 0;
        private static int ActivitySeed = 0;
        private static int ChessSpriteSeed = 0;
        private static int _recursiveActionCount;
        private const int RecursiveActionsLimit = 99999;
        private static int RecursiveActionCount
        {
            get => _recursiveActionCount;
            set
            {
                if (_recursiveActionCount > RecursiveActionsLimit)
                    throw new StackOverflowException($"{nameof(RecursiveActionCount)}>={nameof(RecursiveActionsLimit)}");
                _recursiveActionCount = value;
            }
        }

        /// <summary>
        /// 单体 一列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        protected static T[] Singular<T>(T t) => new[] {t};
        #endregion

        #region Helper
        /// <summary>
        /// 计算比率值(不包括原来的值)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private static float GetRatio(float value, int ratio) => ratio * 0.01f * value;
        #endregion

        protected ChessboardOperator(bool isChallengerFirst, ChessGrid grid)
        {
            _isChallengerOdd = isChallengerFirst;
            rounds = new List<ChessRound>();
            Grid = grid;
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

        public ChessRound StartRound()
        {
            //instance Round
            //invoke pre-action
            //Get all sorted this operators
            //invoke this operations
            //invoke finalization
            RecursiveActionCount = 0;
            currentRound = new ChessRound
            {
                InstanceId = Rounds.Count,
                PreAction = new RoundAction(),
                FinalAction = new RoundAction(),
            };

            var currentOps = StatusMap.Where(o => !o.Value.IsDeath).Select(o => o.Key).ToList();
            var roundProcesses = new List<ChessPosProcess>();
            var preActionFlag = true;
            do
            {
                var op = GetSortedOperator(currentOps); //根据排列逻辑获取执行代理
                if (op == null) break;
                if (GetStatus(op).IsDeath)
                {
                    currentOps.Remove(op);
                    continue;
                }
                CurrentProcess = ChessPosProcess.Instance(ProcessSeed, GetStatus(op).Pos, op.IsChallenger);

                if (CurrentProcess.InstanceId == 0) //第一次需要更新所有棋位上的预设计算
                {
                    foreach (var pos in Grid.Challenger.Concat(Grid.Opposite).Select(c => c.Value))
                    {
                        if (!pos.IsPostedAlive) continue;
                        var o = GetOperator(pos.Operator.InstanceId);
                        o.OnPosting(pos);
                        UpdateTerrain(pos);
                    }
                }

                if (preActionFlag)//如果第一次循环预先执行预设行动
                {
                    var preRoundAction = GetPreRoundTriggerByOperators();
                    currentRound.PreAction.ConcatAction(preRoundAction);
                    preActionFlag = false;
                    foreach (var activity in currentRound.PreAction.Activities)
                        RoundActionInvocation(activity.Key, activity.Value);
                }

                op.MainActivity();

                roundProcesses.Add(CurrentProcess);
                currentOps.Remove(op);

                //移除死亡的棋子
                foreach (var death in StatusMap.Where(o => o.Value.IsDeath && o.Value.Pos >= 0))
                {
                    var deathOp = death.Key;
                    PosOperator(deathOp, -1);
                }

                ProcessSeed++;
            } while (currentOps.Count > 0);

            currentRound.Processes = roundProcesses.ToArray();

            var finalAction = GetRoundEndTriggerByOperators();
            currentRound.FinalAction.ConcatAction(finalAction);
            foreach (var activity in currentRound.FinalAction.Activities)
                RoundActionInvocation(activity.Key, activity.Value);

            //UpdatePosesBuffs(currentRound.FinalAction, true);
            return currentRound;
        }

        #region RoundActivities

        protected abstract RoundAction GetRoundEndTriggerByOperators();

        protected abstract RoundAction GetPreRoundTriggerByOperators();

        protected abstract void RoundActionInvocation(int roundKey, IEnumerable<Activity> activities);

        // 注册当回合结束执行的全局逻辑
        private void RegRoundEnd(int id,IEnumerable<Activity> activity) => AddRoundResourceHelper(currentRound.FinalAction.Activities, id, activity);

        /// <summary>
        /// 回合结束加金币
        /// </summary>
        /// <param name="op"></param>
        /// <param name="to">如果是玩家资源维度的,-1 = 己方，-2 = 对方。正数为棋格</param>
        /// <param name="value"></param>
        public void RegGoldOnRoundEnd(ChessOperator op,int to ,int value)
        {
            RegRoundEnd(RoundAction.PlayerResources,
                Singular(Activity.Instance(
                    ActivitySeed,
                    CurrentProcess.InstanceId,
                    GetStatus(op).Pos,
                    op.IsChallenger ? 0 : 1,
                    ResourceTarget(op, to == -1),
                    Activity.PlayerResource,
                    Singular(CombatConduct.InstancePlayerResource(-1, value))
                    , 0, -1)));
        }
        /// <summary>
        /// 回合结束添加战役宝箱
        /// </summary>
        /// <param name="op"></param>
        /// <param name="to">如果是玩家资源维度的,-1 = 己方，-2 = 对方。正数为棋格</param>
        /// <param name="warChests"></param>
        public void RegWarChestOnRoundEnd(ChessOperator op,int to ,int[] warChests)
        {
            RegRoundEnd(RoundAction.PlayerResources,
                Singular(Activity.Instance(ActivitySeed,
                    CurrentProcess.InstanceId,
                    GetStatus(op).Pos,
                    op.IsChallenger ? 0 : 1,
                    ResourceTarget(op, to == -1),
                    Activity.PlayerResource,
                    warChests.Select(CombatConduct.InstancePlayerResource).ToArray(),
                    0, -1)));
        }

        //注册助手
        private static void AddRoundResourceHelper(Dictionary<int, List<Activity>> dic, int id, IEnumerable<Activity> activity)
        {
            if (!dic.ContainsKey(id))
                dic.Add(id, new List<Activity>());
            dic[id].AddRange(activity);
        }

        #endregion

        private ChessPosProcess CurrentProcess { get; set; }

        protected abstract ChessOperator GetOperator(int id);
        //执行代理的排列逻辑
        private ChessOperator GetSortedOperator(IEnumerable<ChessOperator> list) =>
            list.Where(o => !GetStatus(o).IsDeath)
                .OrderBy(o => GetStatus(o).Pos)
                .ThenBy(o => o.IsChallenger != IsChallengerOdd)
                .FirstOrDefault();


        public ActivityResult AppendActivityWithoutOffender(ChessOperator target,int intent,CombatConduct[] conducts,int skill=0,int rePos = -1) => AppendActivity(null, GetChessPos(target), intent, conducts, skill, rePos);

        /// <summary>
        /// 为当前的行动添加上一个行动指令
        /// </summary>
        /// <param name="offender"></param>
        /// <param name="target"></param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="skill">如果是普通攻击，标记0，大于0将会是技能值</param>
        /// <param name="rePos"></param>
        /// <returns></returns>
        public ActivityResult AppendActivity(ChessOperator offender, IChessPos target, int intent,
            CombatConduct[] conducts,int skill ,int rePos = -1)
        {
            if (target == null) return null;
            RecursiveActionCount++;
            ActivitySeed++;
            var activity = Activity.Instance(ActivitySeed, CurrentProcess.InstanceId,
                offender == null ? -1 : offender.InstanceId,
                offender == null ? -1:
                offender.IsChallenger ? 0 : 1,
                target.Operator.InstanceId,
                intent, conducts, skill, rePos);
            CurrentProcess.Activities.Add(activity);
            var op = GetOperator(target.Operator.InstanceId);
            if (op == null)
                throw new NullReferenceException(
                    $"Target Pos({target.Pos}) is null! from offender Pos({GetStatus(offender).Pos}) as IsChallenger[{offender?.IsChallenger}] type[{offender?.GetType().Name}]");
            activity.Result = GetOperator(op.InstanceId).Respond(activity, offender);
            activity.OffenderStatus = GetStatus(op).Clone();
            UpdateTerrain(target);
            return activity.Result;
        }

        /// <summary>
        /// 生成棋盘精灵
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="host">-1 =回合类型，正数：棋子id</param>
        /// <param name="lasting"></param>
        /// <param name="value"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public T InstanceSprite<T>(IChessPos target, int host,TerrainSprite.LastingType lasting ,int value = 1, int typeId = -99)
            where T : TerrainSprite, new()
        {
            var sprite =
                TerrainSprite.Instance<T>(ChessSpriteSeed, host, lasting, value, target.Pos, target.IsChallenger);
            if (typeId != -99)
                sprite.TypeId = typeId;
            RegSprite(sprite);
            ChessSpriteSeed++;
            return sprite;
        }

        private Activity SpriteActivity(TerrainSprite sprite,bool isAdd)
        {
            var conduct = isAdd
                ? CombatConduct.AddSprite(sprite.Value, sprite.InstanceId, sprite.TypeId)
                : CombatConduct.RemoveSprite(sprite.InstanceId);
            var activity = Activity.Instance(ActivitySeed,
                CurrentProcess.InstanceId, sprite.Host,
                sprite.IsChallenger ? 0 : 1, sprite.Pos, Activity.Sprite,
                Helper.Singular(conduct),
                1, -1);
            ActivitySeed++;
            return activity;
        }
        public void RegSprite(TerrainSprite sprite)
        {
            var activity = SpriteActivity(sprite, true);
            activity.Result = ActivityResult.Instance(ActivityResult.Types.Undefined);
            CurrentProcess.Activities.Add(activity);
            Sprites.Add(sprite);
            Grid.GetScope(sprite.IsChallenger)[sprite.Pos].Terrain.AddSprite(sprite);
        }

        public void RemoveSprite(TerrainSprite sprite)
        {
            var activity = SpriteActivity(sprite, false);
            activity.Result = ActivityResult.Instance(ActivityResult.Types.Undefined);
            CurrentProcess.Activities.Add(activity);
            Sprites.Remove(sprite);
            Grid.GetScope(sprite.IsChallenger)[sprite.Pos].Terrain.RemoveSprite(sprite);
        }

        /// <summary>
        /// 执行进攻方伤害转化
        /// </summary>
        /// <param name="op"></param>
        /// <param name="damage"></param>
        /// <returns></returns>
        public int ConvertHeroDamage(ChessOperator op,int damage)
        {
            var ratio = GetCondition(op, CardState.Cons.StrengthUp);
            return (int)(damage * (1 - 0.01f * ratio));
        }

        private void UpdateTerrain(IChessPos pos)
        {
            foreach (var sprite in pos.Terrain.Sprites.Where(s => s.Lasting == TerrainSprite.LastingType.Relation).ToArray())
            {
                if (GetOperator(sprite.Host).IsAlive) continue;
                RemoveSprite(sprite);
            }
        }

        /// <summary>
        /// 生成回合触发器
        /// </summary>
        /// <param name="op"></param>
        /// <param name="to">如果是玩家资源维度的,-1 = 己方，-2 = 对方。正数为棋格</param>
        /// <param name="intent"><see cref="Activity"/>Intent</param>
        /// <param name="conducts"></param>
        /// <returns></returns>
        public Activity InstanceRoundAction(ChessOperator op, int to, int intent, CombatConduct[] conducts) =>
            Activity.Instance(ActivitySeed, CurrentProcess.InstanceId, GetStatus(op).Pos,
                op.IsChallenger ? 0 : 1,
                ResourceTarget(op, to == -1),
                intent, conducts, 0, -1);

        public bool OnCounterTriggerPass(ChessOperator op,ChessOperator offender)
        {
            if (GetStatus(offender).IsDeath ||
                GetStatus(op).IsDeath ||
                offender.Style.CombatStyle == AttackStyle.CombatStyles.Range ||
                offender.Style.ArmedType < 0 ||
                op.Style.CombatStyle == AttackStyle.CombatStyles.Range)
                return false;
            return !GetBuffOperator(b => b.IsDisableCounter(op)).Any();
        }

        public bool OnDodgeTriggerPass(ChessOperator op, ChessOperator offender)
        {
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
            if (OnDodgeTriggerPass(op, offender))
                result.Result = (int)ActivityResult.Types.Dodge;
            else result.Result = (int)DetermineSufferResult(activity, op, offender);
            /***执行Activities***/
            op.ProceedActivity(activity);
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
        public CombatConduct OnArmorReduction(ChessOperator op, CombatConduct conduct)
        {
            float armor = Damage.GetKind(conduct) == Damage.Kinds.Physical ? op.GetPhysicArmor() : op.GetMagicArmor();
            //加护甲
            armor += GetCondition(op, CardState.Cons.DefendUp);
            //卸甲状态
            if (GetBuffOperator(CardState.Cons.Disarmed, op).Any() || 
                GetCondition(op, CardState.Cons.Disarmed) > 0)
                armor *= 0.5f;
            var finalConduct = conduct * (1 - armor * 0.01f);
            //伤害转化buff 例如：流血
            foreach (var bo in GetBuffOperator(b => b.IsDamageConvertTrigger(op)))
                finalConduct = bo.OnDamageConvertTrigger(op, conduct);
            return finalConduct;
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
            op.OnPosting(chessPos);
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
    }
}