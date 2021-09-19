﻿using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using Microsoft.Extensions.Logging;
using UnityEditor;

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
        private enum ProcessCondition
        {
            PreStart,
            RoundStart,
            Chessman,
            RoundEnd
        }

        private ProcessCondition RoundState { get; set; } = ProcessCondition.PreStart;
        private ChessRound currentRound;
        private Activity currentActivity;
        protected abstract Dictionary<ChessOperator,ChessStatus> StatusMap { get; }
        public IEnumerable<PosSprite> ChessSprites => Sprites;
        protected abstract List<PosSprite> Sprites { get; }

        public int ChallengerGold { get; protected set; }
        public int OpponentGold { get; protected set; }
        public List<int> ChallengerChests { get; set; } = new List<int>();
        public List<int> OpponentChests { get; set; } = new List<int>();
        protected abstract BondOperator[] JiBan { get; }

        public bool IsInit => RoundIdSeed <= 0;
        private int RoundIdSeed;
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

        private readonly ChessProcess[] EmptyProcess = Array.Empty<ChessProcess>();

        private readonly List<ChessRound> rounds;
        private List<ChessProcess> currentProcesses;
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
        public ChessStatus GetFullCondition(ChessOperator op)
        {
            var status = GetStatus(op);
            var pos = GetChessPos(op).Terrain;
            var dic = new Dictionary<int, int>();
            foreach (var con in CardState.ConsArray)
            {
                var key = (int)con;
                var value = 0;
                if (status.Buffs.ContainsKey(key))
                    value += status.Buffs[key];
                value += pos.GetServed(con, op);
                if (value > 0) dic.Add(key, value);
            }

            return ChessStatus.Instance(status.Hp, status.MaxHp, status.Pos, status.Speed, dic);
        }

        protected void Log(string message) => Logger?.Log(LogLevel.Information, message);

        public void RoundConfirm()
        {
            currentRound = new ChessRound
            {
                InstanceId = RoundIdSeed,
                PreRound = new List<ChessProcess>(),
                PreAction = new RoundAction(),
                FinalAction = new RoundAction(),
                ChallengerJiBans = new List<int>(),
                OppositeJiBans = new List<int>()
            };
            RoundIdSeed++;
            RoundState = ProcessCondition.PreStart;
            foreach (var chessOperator in StatusMap.Where(o => !o.Value.IsDeath).Select(o => o.Key))
                chessOperator.PreStart();
            currentRound.PreRoundStats = StatusMap.Where(s => !s.Value.IsDeath)
                .ToDictionary(s => s.Key.InstanceId, s => GetFullCondition(s.Key));
        }
        public ChessRound StartRound()
        {
            if (IsGameOver) return null;
            //instance Round
            //invoke pre-action
            //Get all sorted this operators
            //invoke this operations
            //invoke finalization
            RoundState = ProcessCondition.RoundStart;
            RecursiveActionCount = 0;
            ActivatedJiBan.Clear();
            Log($"开始回合[{currentRound.InstanceId}]");
            var currentOps = StatusMap.Where(o => !o.Value.IsDeath).Select(o => o.Key).ToList();
            currentProcesses = new List<ChessProcess>();
            RefreshChessPosses();
            InvokePreRoundTriggers();
            RoundState = ProcessCondition.Chessman;
            do
            {
                var op = GetSortedOperator(currentOps); //根据排列逻辑获取执行代理
                if (op == null) break;
                if (GetStatus(op).IsDeath)
                {
                    currentOps.Remove(op);
                    continue;
                }

                InstanceChessmanProcess(GetStatus(op).Pos, op.IsChallenger);

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

                LogProcess(CurrentProcess);
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
            RoundState = ProcessCondition.RoundEnd;
            InvokeRoundEndTriggers();

            return currentRound;
        }

        private void LogProcess(ChessProcess process)
        {
            string opText;
            switch (CurrentProcess.Type)
            {
                case ChessProcess.Types.Chessman:
                    opText = OperatorText(CurrentProcess);
                    break;
                case ChessProcess.Types.Chessboard:
                    opText = "棋盘";
                    break;
                case ChessProcess.Types.JiBan:
                    opText = $"羁绊[{CurrentProcess.Major}]";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Log($"进程({CurrentProcess.InstanceId})[{opText}]");
            foreach (var activities in process.CombatMaps.Values.Select(m => m.Activities.Concat(m.CounterActs)))
            {
                foreach (var activity in activities)
                {
                    LogActivity(activity);
                    LogConducts(activity);
                    Log(activity.Result.ToString());
                }
            }
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

        private void InstanceChessmanProcess(int pos, bool isChallenger) =>
            InstanceProcess(ChessProcess.Types.Chessman, pos, isChallenger);
        private void InstanceProcess(ChessProcess.Types type, int major, bool isChallenger)
        {
            CurrentProcess = ChessProcess.Instance(ProcessSeed, currentRound.InstanceId, type, major, isChallenger);
            switch (RoundState)
            {
                case ProcessCondition.RoundStart:
                    currentRound.PreAction.ChessProcesses.Add(CurrentProcess);
                    break;
                case ProcessCondition.Chessman:
                    currentProcesses.Add(CurrentProcess);
                    break;
                case ProcessCondition.RoundEnd:
                    currentRound.FinalAction.ChessProcesses.Add(CurrentProcess);
                    break;
                case ProcessCondition.PreStart:
                    currentRound.PreRound.Add(CurrentProcess);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ProcessSeed++;
        }

        private void CheckIsGameOver()
        {
            IsOppositeWin = Grid.Challenger.Values
                .Where(p => p.Operator != null && p.Operator.CardType == GameCardType.Base).All(p => GetStatus(p.Operator).IsDeath);
            IsChallengerWin = Grid.Opposite.Values
                .Where(p => p.Operator != null && p.Operator.CardType == GameCardType.Base).All(p => GetStatus(p.Operator).IsDeath);
        }

        #region RoundActivities

        protected abstract void InvokeRoundEndTriggers();

        protected abstract void InvokePreRoundTriggers();

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
            var activity = InstanceActivity(op.IsChallenger, op, toChallenger ? -1 : -2, Activity.PlayerResource,
                Singular(CombatConduct.InstancePlayerResource(resourceId, value)), 0, -1);
            AddToCurrentProcess(activity, 0);
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

        private ChessProcess CurrentProcess { get; set; }

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
        /// <param name="major">-1=棋盘活动</param>
        /// <param name="fromChallenger"></param>
        /// <param name="target"></param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="skill"></param>
        /// <param name="rePos"></param>
        public void InstanceChessboardActivity(int major,bool fromChallenger, IChessOperator target, int intent,
            CombatConduct[] conducts, int skill = 0, int rePos = -1)
        {
            //棋盘没用字典分类活动顺序，所以不用声明actId
            if (CurrentProcess == null || CurrentProcess.Type != ChessProcess.Types.Chessman)
                InstanceProcess(ChessProcess.Types.Chessboard, major, fromChallenger);
            var activity = InstanceActivity(fromChallenger, target.InstanceId, intent, conducts, skill, rePos);
            AddToCurrentProcess(activity, 0);
            ProcessActivityResult(activity);
        }

        private static int GetScope(bool isChallenger) => isChallenger ? 0 : 1;
        public void InstanceJiBanActivity(int bondId, bool fromChallenger, IChessOperator target, int intent,
            CombatConduct[] conducts, int rePos = -1)
        {

            if (CurrentProcess == null ||
                CurrentProcess.Type != ChessProcess.Types.JiBan || 
                CurrentProcess.Major != bondId ||
                CurrentProcess.Scope != GetScope(fromChallenger))
                InstanceProcess(ChessProcess.Types.JiBan, bondId, fromChallenger);
            var activity = InstanceActivity(fromChallenger, target.InstanceId, intent, conducts, 0, rePos);
            AddToCurrentProcess(activity, 0);
            ProcessActivityResult(activity);
        }

        private string OperatorText(int instanceId) => OpText(GetOperator(instanceId));
        private string OperatorText(ChessProcess process) => OpText(Grid.GetChessPos(process.Major, process.Scope == 0).Operator);

        private string OpText(IChessOperator op)
        {
            if (op == null) return string.Empty;
            var stat = GetStatus(op);
            return $"{op.InstanceId}.{op}[{stat.Hp}/{stat.MaxHp}]Pos({stat.Pos})";
        }
        
        private void ProcessActivityResult(Activity activity)
        {
            var target = GetOperator(activity.To);
            var offender = activity.From < 0 ? null : GetOperator(activity.From);
            activity.Result = GetOperator(target.InstanceId).Respond(activity, offender);
            activity.OffenderStatus = GetStatus(target).Clone();
            if (activity.To >= 0 && activity.Result.IsDeath)
            {
                var op = GetOperator(activity.To);
                Log($"{op}败退！");
            }

            UpdateTerrain(GetChessPos(target));
        }

        private void LogActivity(Activity activity) =>
            Log($"活动-->{activity.StanceText()}@{activity.IntentText()} 目标：{OperatorText(activity.To)}");

        private void LogConducts(Activity activity)
        {
            foreach (var conduct in activity.Conducts) Log($"武技--->{conduct}");
        }

        private bool IsFlagCounter = false;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="offender"></param>
        /// <param name="target"></param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="actId">-1 = 加入上一个活动</param>
        /// <param name="skill"></param>
        /// <param name="rePos"></param>
        /// <returns></returns>
        public ActivityResult AppendOpActivity(ChessOperator offender, IChessPos target, int intent,
            CombatConduct[] conducts,int actId,int skill ,int rePos = -1)
        {
            if (target == null) return null;
            //如果是Counter将会在这个执行结束前，递归的活动都记录再反击里
            if (!IsFlagCounter) IsFlagCounter = intent == Activity.Counter;
            var activity = InstanceActivity(offender.IsChallenger, offender, target.Operator.InstanceId, intent,
                conducts, skill, rePos);
            currentActivity = activity;
            AddToCurrentProcess(activity, actId);
            var op = GetOperator(target.Operator.InstanceId);
            if (op == null)
                throw new NullReferenceException(
                    $"Target Pos({target.Pos}) is null! from offender Pos({GetStatus(offender).Pos}) as IsChallenger[{offender?.IsChallenger}] type[{offender?.GetType().Name}]");
            ProcessActivityResult(currentActivity);
            IsFlagCounter = false;
            return currentActivity.Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromChallenger"></param>
        /// <param name="targetInstance"> op = InstanceId, <see cref="Activity.Sprite"/> = Pos</param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="skill"></param>
        /// <param name="rePos"></param>
        /// <returns></returns>
        private Activity InstanceActivity(bool fromChallenger, int targetInstance, int intent, CombatConduct[] conducts,
            int skill, int rePos)
        {
            return InstanceActivity(fromChallenger, null, targetInstance, intent, conducts, skill, rePos);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromChallenger"></param>
        /// <param name="offender">null = -1 challenger, -2 opponent</param>
        /// <param name="targetInstance"> op = InstanceId, sprite = Pos</param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="skill"></param>
        /// <param name="rePos"></param>
        /// <returns></returns>
        private Activity InstanceActivity(bool fromChallenger, ChessOperator offender, int targetInstance, int intent,
            CombatConduct[] conducts, int skill, int rePos)
        {
            RecursiveActionCount++;
            ActivitySeed++;
            var processId = CurrentProcess.InstanceId;
            var fromId = offender == null ? fromChallenger ? -1 : -2 : offender.InstanceId;
            var activity = Activity.Instance(
                ActivitySeed,
                processId,
                fromId,
                fromChallenger ? 0 : 1,
                targetInstance,
                intent, conducts, skill, rePos);
            //Log($"生成{activity}");
            return activity;
        }

        /// <summary>
        /// 加入活动
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="actId">-1 = 加入上一个活动</param>
        private void AddToCurrentProcess(Activity activity, int actId)
        {
            if (actId == -1) //如果-1的话就加入上一个活动
                actId = CurrentProcess.CombatMaps.Count == 0 ? 0 : CurrentProcess.CombatMaps.Last().Key;
            if (!CurrentProcess.CombatMaps.ContainsKey(actId))
                CurrentProcess.CombatMaps.Add(actId, new CombatMapper(actId));
            if(IsFlagCounter)
                CurrentProcess.CombatMaps[actId].CounterActs.Add(activity);
            else CurrentProcess.CombatMaps[actId].Activities.Add(activity);
        }

        //private List<Activity> InnerActivities { get; set; }

        /// <summary>
        /// 生成棋盘精灵
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="typeId"></param>
        /// <param name="lasting">回合数或宿主id</param>
        /// <param name="value">buff参数</param>
        /// <returns></returns>
        public T InstanceSprite<T>(IChessPos target, int typeId,int lasting,int value)
            where T : PosSprite, new()
        {
            var sprite =
                PosSprite.Instance<T>(this,ChessSpriteSeed,
                    value: value,
                    typeId: typeId,
                    pos: target.Pos, isChallenger: target.IsChallenger, lasting: lasting);
            RegSprite(sprite);
            ChessSpriteSeed++;
            return sprite;
        }

        private Activity SpriteActivity(PosSprite sprite,bool isAdd)
        {
            if (CurrentProcess == null)
                InstanceProcess(ChessProcess.Types.Chessboard, sprite.Pos, sprite.IsChallenger);
            var conduct = isAdd
                ? CombatConduct.AddSprite(sprite.Value, sprite.InstanceId, sprite.TypeId)
                : CombatConduct.RemoveSprite(sprite.InstanceId, sprite.TypeId);
            return InstanceActivity(sprite.IsChallenger, sprite.Pos, Activity.Sprite,
                Helper.Singular(conduct), 1, -1);
        }

        private void RegSprite(PosSprite sprite)
        {
            Log($"添加{sprite}");
            var activity = SpriteActivity(sprite, true);
            activity.Result = ActivityResult.Instance(ActivityResult.Types.ChessPos);

            if (currentRound.InstanceId!= CurrentProcess.RoundId)
                InstanceProcess(ChessProcess.Types.Chessboard, -1, sprite.IsChallenger);
            AddToCurrentProcess(activity, 0);
            Sprites.Add(sprite);
            Grid.GetScope(sprite.IsChallenger)[sprite.Pos].Terrain.AddSprite(sprite);
        }

        public void DepleteSprite(PosSprite sprite)
        {
            Log($"移除{sprite}");
            if (sprite.Value <= 0) RemoveSprite(sprite);
            var activity = SpriteActivity(sprite, false);
            activity.Result = ActivityResult.Instance(ActivityResult.Types.ChessPos);
            if (currentRound.InstanceId != CurrentProcess.RoundId)
                InstanceProcess(ChessProcess.Types.Chessboard, -1, sprite.IsChallenger);
            AddToCurrentProcess(activity, 0);
        }

        private void RemoveSprite(PosSprite sprite)
        {
            Sprites.Remove(sprite);
            Grid.GetScope(sprite.IsChallenger)[sprite.Pos].Terrain.RemoveSprite(sprite);
        }

        public int GetHeroBuffDamage(ChessOperator op)
        {
            var ratio = GetCondition(op, CardState.Cons.StrengthUp);
            return (int)(op.Strength + op.Strength * 0.01f * ratio);
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
            foreach (var sprite in pos.Terrain.Sprites.Where(s => s.Host == PosSprite.HostType.Relation).ToArray())
            {
                var op = GetOperator(sprite.Lasting);
                if (op == null)
                    throw new InvalidOperationException(
                        $"{sprite} 找不到宿主[{sprite.Lasting}]. 注意如果是依赖型精灵，请在sprite.Lasting填上宿主id");
                if (op.IsAlive) continue;
                DepleteSprite(sprite);
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

        private ActivityResult.Types DetermineSufferResult(ChessOperator op, ChessOperator offender)
        {
            if (GetCondition(op,CardState.Cons.Shield) > 0)
            {
                if (offender.IsIgnoreShieldUnit) return ActivityResult.Types.Suffer;
                //如果被护盾免伤后，扣除护盾1层
                AppendOpActivity(op, GetChessPos(op), Activity.Self,
                    Singular(CombatConduct.InstanceBuff(CardState.Cons.Shield, -1)), -1, 1);
                return ActivityResult.Types.Shield;
            }
            if (GetCondition(op,CardState.Cons.EaseShield) > 0) // 缓冲盾
                return ActivityResult.Types.EaseShield;
            return ActivityResult.Types.Suffer;
        }

        public int OnBuffingConvert(ChessOperator op, CombatConduct conduct)
        {
            if (op.CardType != GameCardType.Hero) return 0;
            var value = (int)conduct.Total;
            foreach (var bo in GetBuffOperator((CardState.Cons) conduct.Element, op))
                value = bo.OnBuffConvert(op, value);
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
                result.Result = (int)DetermineSufferResult(op, offender);
                /***执行Activities***/
                op.ProceedActivity(activity);
            }

            result.Status = GetFullCondition(op);
            return result;
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
            float armor;
            var addOn = 0;
            if (Damage.GetKind(conduct) == Damage.Kinds.Physical)
            {
                armor = op.GetPhysicArmor();
                addOn += GetCondition(op, CardState.Cons.ArmorUp);
                foreach (var bo in GetBuffOperator(b => b.IsArmorAddOnTrigger))
                    addOn += bo.OnArmorAddOn(armor, op, conduct);
            }
            else armor = op.GetMagicArmor();
            //加护甲
            var resisted = Math.Min(1 - (armor + addOn) * 0.01f, 1);
            conduct.Multiply(resisted);
            //伤害或护甲转化buff 例如：流血
            foreach (var bo in GetBuffOperator(b => b.IsSufferConductTrigger))
                bo.OnSufferConduct(op, conduct);
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

        public int Randomize(int min, int excludedMax) => random.Next(min, excludedMax);

        public bool IsRouseDamagePass(ChessOperator op)
        {
            var ratio = 0;
            if (op.CardType == GameCardType.Hero)
                ratio += DataTable.Hero[op.CardId].RouseRatio;
            foreach (var bo in GetBuffOperator(b=>b.IsRouseRatioTrigger)) 
                ratio += bo.OnRouseRatioAddOn(op);
            return IsRandomPass(ratio);
        }

        public bool IsCriticalDamagePass(ChessOperator op)
        {
            var ratio = 0;
            if (op.CardType == GameCardType.Hero)
                ratio += DataTable.Hero[op.CardId].CriticalRatio;
            foreach (var bo in GetBuffOperator(b => b.IsCriticalRatioTrigger))
                ratio += bo.OnCriticalRatioAddOn(op);
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
            var chessPos = GetChessPos(op);
            if (!IsInit) return;
            op.OnPostingTrigger(chessPos);
            UpdateTerrain(chessPos);
        }

        public IChessPos GetChessPos(IChessOperator op) => Grid.GetChessPos(op, GetStatus(op).Pos);

        public IChessPos GetContraTarget(ChessOperator op)
        {
            return Grid.GetContraPositionInSequence(
                GetCondition(op, CardState.Cons.Confuse) > 0 ? op.IsChallenger : !op.IsChallenger,
                GetStatus(op).Pos,
                p => p.IsPostedAlive && p.Operator != op);
        }

        public IEnumerable<IChessPos> GetAttackPath(ChessOperator op)
        {
            var isChallenger = GetCondition(op, CardState.Cons.Confuse) > 0 ? !op.IsChallenger : op.IsChallenger;
            return Grid.GetAttackPath(isChallenger, GetStatus(op).Pos).Where(p => p.Operator != op);
        }

        public IEnumerable<IChessPos> GetFriendlyNeighbors(ChessOperator op)
        {
            var isChallenger = GetCondition(op, CardState.Cons.Confuse) > 0 ? !op.IsChallenger : op.IsChallenger;
            return Grid.GetNeighbors(GetStatus(op).Pos, isChallenger);
        }

        public IEnumerable<IChessPos> GetContraNeighbors(ChessOperator op)
        {
            var isChallenger = GetCondition(op, CardState.Cons.Confuse) > 0 ? !op.IsChallenger : op.IsChallenger;
            return Grid.GetNeighbors(GetStatus(op).Pos, isChallenger);
        }

        public IEnumerable<IChessPos> GetRivals(ChessOperator op, Func<IChessPos, bool> condition = null)
        {
            var scope = GetCondition(op, CardState.Cons.Confuse) > 0
                ? Grid.GetRivalScope(!op.IsChallenger)
                : Grid.GetRivalScope(op);
            return scope.Values.Where(condition == null ? p => p.IsPostedAlive : condition);
        }

        public IEnumerable<IChessPos> GetNeighbors(IChessPos pos, bool includeUnPost, int surround= 1) => Grid.GetNeighbors(pos, includeUnPost, surround);
        public IEnumerable<IChessPos> GetFriendly(ChessOperator op, Func<IChessPos, bool> condition = null)
        {
            var scope = GetCondition(op,CardState.Cons.Confuse)>0 ? Grid.GetScope(!op.IsChallenger) : Grid.GetScope(op);
            return scope.Values.Where(condition == null ? p => p.IsPostedAlive : condition);
        }
        public IEnumerable<IChessPos> GetChainedPos(IChessOperator op, Func<IChessPos, bool> chainedFilter) =>
            Grid.GetChained(GetStatus(op).Pos, op.IsChallenger, chainedFilter);
        public IChessPos BackPos(IChessPos pos) => Grid.BackPos(pos);
        #endregion

        public IEnumerable<PosSprite> GetSpriteInChessPos(int pos, bool isChallenger) => Grid.GetChessPos(pos, isChallenger).Terrain.Sprites;
        public IEnumerable<PosSprite> GetSpriteInChessPos(ChessOperator op) => GetChessPos(op).Terrain.Sprites;

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