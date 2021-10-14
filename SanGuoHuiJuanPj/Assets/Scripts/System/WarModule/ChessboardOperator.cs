using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋盘处理器。主要处理<see cref="IChessOperator"/>的交互与进程逻辑。
    /// </summary>
    public abstract class ChessboardOperator 
    {
        public const int HeroDodgeLimit = 75;
        public const int HeroArmorLimit = 100;
        public ChessGrid Grid { get; }
        private enum ProcessCondition
        {
            PlaceActions,
            RoundStart,
            Chessman,
            RoundEnd
        }

        private ProcessCondition RoundState { get; set; } = ProcessCondition.PlaceActions;

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

        #region Round and Process methods

        private struct ChessMajor
        {
            public static readonly ChessMajor UnDefined = new ChessMajor(-3, false);
            public int Pos;
            public bool IsChallenger;
            public int GetMajor() => Pos < 0 ? IsChallenger ? -1 : -2 : Pos;
            public int GetScope() => IsChallenger ? 0 : 1;

            public ChessMajor(int pos, bool isChallenger)
            {
                Pos = pos;
                IsChallenger = isChallenger;
            }
        }
        private ChessMajor CurrentMajor { get; set; }
        private readonly List<ChessRound> rounds;
        private ChessRound currentRound;

        private ChessRound GetActiveRound()
        {
            if (rounds.Count != 0 &&
                !rounds.Last().IsEnd)
                return currentRound;
            return InstanceRound();
        }

        private ChessRound InstanceRound()
        {
            currentRound = new ChessRound
            {
                InstanceId = RoundIdSeed,
                PlaceActions = new List<ChessProcess>(),
                Processes = new List<ChessProcess>(),
                PreAction = new RoundAction(),
                FinalAction = new RoundAction(),
                ChallengerJiBans = new List<int>(),
                OppositeJiBans = new List<int>()
            };
            rounds.Add(currentRound);
            RoundIdSeed++;
            return currentRound;
        }

        private ChessProcess GetCurrentProcess()
        {
            switch (RoundState)
            {
                case ProcessCondition.RoundStart:
                    return GetActiveRound().PreAction.ChessProcesses.LastOrDefault();
                case ProcessCondition.Chessman:
                    return GetActiveRound().Processes.LastOrDefault();
                case ProcessCondition.RoundEnd:
                    return GetActiveRound().FinalAction.ChessProcesses.LastOrDefault();
                case ProcessCondition.PlaceActions:
                    return GetActiveRound().PlaceActions.LastOrDefault();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

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

        #endregion
        /// <summary>
        /// 是否是新摆入的棋子
        /// </summary>
        protected List<ChessOperator> PlaceList = new List<ChessOperator>();

        protected ChessboardOperator(ChessGrid grid, ILogger log = null)
        {
            rounds = new List<ChessRound>();
            Grid = grid;
            Logger = log;
        }
        protected abstract ChessOperator GetOperator(int id);

        #region Status & Condition

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
        public ChessStatus GetFullCondition(IChessOperator op)
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

            return ChessStatus.Instance(status.Hp, status.MaxHp, status.Pos, status.Speed, dic,
                status.LastSuffers.ToList());
        }

        #endregion

        #region Round
        public ChessRound StartRound()
        {
            if (IsGameOver) return null;
            CurrentMajor = ChessMajor.UnDefined;

            RoundState = ProcessCondition.PlaceActions;
            foreach (var op in PlaceList.ToArray())
            {
                PlaceList.Remove(op);
                op.OnPlaceInvocation();
            }

            var round = GetActiveRound();
            round.PreRoundStats = StatusMap.Where(s => !s.Value.IsDeath)
                .ToDictionary(s => s.Key.InstanceId, s => GetFullCondition(s.Key));

            RoundState = ProcessCondition.RoundStart;
            RecursiveActionCount = 0;
            ActivatedJiBan.Clear();
            Log($"开始回合[{currentRound.InstanceId}]");
            var currentOps = StatusMap.Where(o => !o.Value.IsDeath).Select(o => o.Key).ToList();
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

                InstanceChessmanProcess(op);

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

                LogProcess(GetCurrentProcess());
            } while (currentOps.Count > 0);

            CurrentMajor = ChessMajor.UnDefined;
            if (IsGameOver)
            {
                var winner = string.Empty;
                if (IsChallengerWin)
                    winner += "玩家胜利!";
                if (IsOppositeWin)
                    winner += "对方胜利!";
                Log($"{winner}");
                currentRound.IsEnd = true;
                return currentRound;
            }
            RoundState = ProcessCondition.RoundEnd;
            InvokeRoundEndTriggers();
            currentRound.IsEnd = true;
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
                    PosOperator(o, pos.Pos);
                UpdateTerrain(pos);
            }
        }

        private void InstanceChessmanProcess(ChessOperator op)
        {
            CurrentMajor = new ChessMajor(GetStatus(op).Pos, op.IsChallenger);
            GetMajorProcess(ChessProcess.Types.Chessman, CurrentMajor.Pos, CurrentMajor.IsChallenger);
        }

        private void CheckIsGameOver()
        {
            IsOppositeWin = Grid.Challenger.Values
                .Where(p => p.Operator != null && p.Operator.CardType == GameCardType.Base).All(p => GetStatus(p.Operator).IsDeath);
            IsChallengerWin = Grid.Opposite.Values
                .Where(p => p.Operator != null && p.Operator.CardType == GameCardType.Base).All(p => GetStatus(p.Operator).IsDeath);
        }

        #endregion

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
                    GetActiveRound().ChallengerJiBans.Add(jb.BondId);
                else
                    GetActiveRound().OppositeJiBans.Add(jb.BondId);
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
                Singular(CombatConduct.InstancePlayerResource(resourceId, op.InstanceId, value)), 0, -1);
            AddToCurrentProcess(ChessProcess.Types.Chessboard, op.IsChallenger ? -1 : -2, activity, -1);
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


        //执行代理的排列逻辑
        private ChessOperator GetSortedOperator(IEnumerable<ChessOperator> list) =>
            list.Where(o => !GetStatus(o).IsDeath)
                .OrderBy(o => GetStatus(o).Pos)
                .ThenByDescending(o => o.IsChallenger)
                .FirstOrDefault();

        /// <summary>
        /// 棋盘指令，非棋子执行类，buff,精灵
        /// </summary>
        /// <param name="fromChallenger"></param>
        /// <param name="target"></param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="skill"></param>
        /// <param name="rePos"></param>
        public void InstanceChessboardActivity(bool fromChallenger, IChessOperator target, int intent,
            CombatConduct[] conducts, int skill = 0, int rePos = -1)
        {
            //棋盘没用字典分类活动顺序，所以直接嵌入上一个活动actId-1
            var activity = InstanceActivity(fromChallenger,null ,target.InstanceId, intent, conducts, skill, rePos);
            activity.TargetStatus = GetFullCondition(target);
            AddToCurrentProcess(ChessProcess.Types.Chessboard, fromChallenger ? -1 : -2, activity, -1);
            ProcessActivityResult(activity);
        }

        public void InstanceJiBanActivity(int bondId, bool fromChallenger, IChessOperator target, int intent,
            CombatConduct[] conducts, int rePos = -1)
        {
            CurrentMajor = new ChessMajor(bondId, fromChallenger);
            GetMajorProcess(ChessProcess.Types.JiBan, bondId, fromChallenger);
            var activity = InstanceActivity(fromChallenger,null ,target.InstanceId, intent, conducts, skill: 0, rePos);

            activity.TargetStatus = GetFullCondition(target);
            AddToCurrentProcess(ChessProcess.Types.Chessboard, fromChallenger ? -1 : -2, activity, 0);
            ProcessActivityResult(activity);
        }


        /// <summary>
        /// 武将技活动
        /// </summary>
        /// <param name="offender">施展者</param>
        /// <param name="target">目标</param>
        /// <param name="intent">活动标记，查<see cref="Activity"/>常量如：<see cref="Activity.Offensive"/></param>
        /// <param name="actId">组合技标记，-1 = 追随上套组合，0或大于0都是各别组合标记。例如大弓：攻击多人，但是组合技都标记为0，它將同时间多次攻击。而连弩的连击却是每个攻击标记0,1,2,3(不同标记)，这样它会依次执行而不是一次执行一组</param>
        /// <param name="skill">武将技标记，-1为隐式技能，0为普通技能，大于0为武将技</param>
        /// <param name="conducts">招式，每一式会有一个招式效果。如赋buff，伤害，补血等...一个活动可以有多个招式</param>
        /// <param name="rePos">移位，-1为无移位，而大或等于0将会指向目标棋格(必须是空棋格)</param>
        /// <returns></returns>
        public ActivityResult AppendOpActivity(ChessOperator offender, IChessPos target, int intent,
            CombatConduct[] conducts, int actId, int skill, int rePos = -1)
        {
            if (target == null) return null;
            if (isCounterFlagged) return ProcessActivity();

            isCounterFlagged = intent == Activity.Counter;
            var result = ProcessActivity();
            isCounterFlagged = false;
            return result;

            ActivityResult ProcessActivity()
            {
                //如果是Counter将会在这个执行结束前，递归的活动都记录再反击里
                var activity = InstanceActivity(offender.IsChallenger, offender, target.Operator.InstanceId, intent,
                    conducts, skill, rePos);
                if (target.Operator != null)
                    activity.TargetStatus = GetFullCondition(target.Operator);
                AddToCurrentProcess(ChessProcess.Types.Chessman, CurrentMajor.GetMajor(), activity, actId);
                var op = GetOperator(target.Operator.InstanceId);
                if (op == null)
                    throw new NullReferenceException(
                        $"Target Pos({target.Pos}) is null! from offender Pos({GetStatus(offender).Pos}) as IsChallenger[{offender?.IsChallenger}] type[{offender?.GetType().Name}]");
                return ProcessActivityResult(activity);
            }
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
            var processId = GetMajorProcess(ChessProcess.Types.Chessman, CurrentMajor.GetMajor(), fromChallenger).InstanceId;
            var fromId = offender == null ? fromChallenger ? -1 : -2 : offender.InstanceId;
            var activity = Activity.Instance(
                ActivitySeed,
                processId,
                fromId,
                fromChallenger ? 0 : 1,
                targetInstance,
                intent, conducts.Select(c => c.Clone()).ToArray(), skill, rePos);
            //Log($"生成{activity}");
            return activity;
        }

        /// <summary>
        /// 加入活动
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fromPos"></param>
        /// <param name="activity"></param>
        /// <param name="actId">-1 = 加入上一个活动</param>
        private void AddToCurrentProcess(ChessProcess.Types type, int fromPos, Activity activity, int actId)
        {
            var process = GetMajorProcess(type, fromPos, Scope(activity));
            if (actId == -1) //如果-1的话就加入上一个活动
                actId = process.CombatMaps.Count == 0 ? 0 : process.CombatMaps.Last().Key;
            
            if (!process.CombatMaps.ContainsKey(actId))
            {
                CurrentCombatMapper = new CombatMapper(actId);
                process.CombatMaps.Add(actId, CurrentCombatMapper);
            }
            else CurrentCombatMapper = process.CombatMaps[actId];

            MapperAdd(activity);
            return;

            bool Scope(Activity act) => act.From < 0 ? act.From == -1 : GetOperator(act.From).IsChallenger;

            void MapperAdd(Activity act)
            {
                if (act.Intent == Activity.Counter)
                    CurrentCombatMapper.CounterActs.Add(act);
                else
                    CurrentCombatMapper.Activities.Add(act);
            }
        }

        private ChessProcess GetMajorProcess(ChessProcess.Types type, int major, bool isChallenger)
        {
            var process = GetCurrentProcess();
            if (process == null || process.Major != CurrentMajor.Pos || process.Scope != CurrentMajor.GetScope())
            {
                CurrentMajor = new ChessMajor(major, isChallenger);
                process = InstanceProcess(CurrentMajor);

                return process;

                ChessProcess InstanceProcess(ChessMajor maj)
                {
                    var pros = ChessProcess.Instance(ProcessSeed, GetActiveRound().InstanceId, type, maj.Pos,
                        maj.IsChallenger);
                    ProcessSeed++;
                    switch (RoundState)
                    {
                        case ProcessCondition.RoundStart:
                        {
                            GetActiveRound().PreAction.ChessProcesses.Add(pros);
                            break;
                        }
                        case ProcessCondition.Chessman:
                        {
                            GetActiveRound().Processes.Add(pros);
                            break;
                        }
                        case ProcessCondition.RoundEnd:
                        {
                            GetActiveRound().FinalAction.ChessProcesses.Add(pros);
                            break;
                        }
                        case ProcessCondition.PlaceActions:
                        {
                            GetActiveRound().PlaceActions.Add(pros);
                            break;
                        }
                        default:
                            throw new InvalidOperationException(
                                $"非法执行组合RoundState[{RoundState}] + ProcessType[{type}]!");
                    }

                    return pros;
                }
            }

            return process;
        }

        /// <summary>
        /// 生成棋盘精灵
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="typeId"></param>
        /// <param name="lasting">回合数或宿主id</param>
        /// <param name="value">buff参数</param>
        /// <param name="actId"></param>
        /// <returns></returns>
        public T InstanceSprite<T>(IChessPos target, int typeId,int lasting,int value,int actId)
            where T : PosSprite, new()
        {
            var sprite =
                PosSprite.Instance<T>(this,ChessSpriteSeed,
                    value: value,
                    typeId: typeId,
                    pos: target.Pos, isChallenger: target.IsChallenger, lasting: lasting);
            ChessSpriteSeed++;
            RegSprite(sprite, actId);
            return sprite;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="op"></param>
        /// <param name="target"></param>
        /// <param name="typeId"></param>
        /// <param name="conducts"></param>
        /// <param name="actId"></param>
        /// <param name="skill"></param>
        /// <param name="lasting">1 = 回合数，0 = 即刻销毁, -1 = 去除</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ActivityResult DelegateSpriteActivity<T>(ChessOperator op,IChessPos target, int typeId, CombatConduct[] conducts,int actId ,int skill,int lasting = 0, int value = 0)
            where T : PosSprite, new()
        {
            var sprite = InstanceSprite<T>(target, typeId, lasting, value, actId);
            return sprite.OnActivity(op, this, conducts, actId, skill);
        }

        private Activity SpriteActivity(PosSprite sprite,bool isAdd)
        {
            GetMajorProcess(ChessProcess.Types.Chessboard, sprite.Pos, sprite.IsChallenger);
            var conduct = isAdd
                ? CombatConduct.AddSprite(sprite.Value, sprite.TypeId, sprite.InstanceId)
                : CombatConduct.RemoveSprite(sprite.InstanceId, sprite.TypeId);
            return InstanceActivity(sprite.IsChallenger, null, sprite.Pos, Activity.Sprite,
                Helper.Singular(conduct), 1, -1);
        }

        private void RegSprite(PosSprite sprite,int actId)
        {
            Log($"添加{sprite}");
            var activity = SpriteActivity(sprite, true);
            //activity.Result = ActivityResult.Instance(ActivityResult.Types.ChessPos);
            AddToCurrentProcess(ChessProcess.Types.Chessboard, sprite.IsChallenger ? -1 : -2, activity, actId);
            Sprites.Add(sprite);
            Grid.GetScope(sprite.IsChallenger)[sprite.Pos].Terrain.AddSprite(sprite);
        }

        public bool UpdateRemovable(PosSprite sprite)
        {
            if (sprite.Host == PosSprite.HostType.Relation && 
                (sprite.Lasting > 0 && GetOperator(sprite.Lasting).IsAlive)) return false;

            if (sprite.Host == PosSprite.HostType.Round && sprite.Lasting > 0) return false;

            RemoveSprite(sprite);
            var activity = SpriteActivity(sprite, false);
            //activity.Result = ActivityResult.Instance(ActivityResult.Types.ChessPos);
            AddToCurrentProcess(ChessProcess.Types.Chessboard, sprite.IsChallenger ? -1 : -2, activity, -1);
            Log($"移除{sprite}");
            return true;
        }

        private void RemoveSprite(PosSprite sprite)
        {
            Sprites.Remove(sprite);
            Grid.GetScope(sprite.IsChallenger)[sprite.Pos].Terrain.RemoveSprite(sprite);
        }

        private void UpdateTerrain(IChessPos pos)
        {
            foreach (var sprite in pos.Terrain.Sprites.ToArray()) UpdateRemovable(sprite);
        }

        #endregion

        #region Logs

        private void Log(string message) => Logger?.Log(LogLevel.Information, message);

        private void LogProcess(ChessProcess process)
        {
            string opText;
            switch (process.Type)
            {
                case ChessProcess.Types.Chessman:
                    opText = OperatorText(process, true);
                    break;
                case ChessProcess.Types.Chessboard:
                    opText = "棋盘";
                    break;
                case ChessProcess.Types.JiBan:
                    opText = $"羁绊[{process.Major}]";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Log($"**********进程({process.InstanceId})[{opText}]**********");
            foreach (var combat in process.CombatMaps.Values)
            {
                foreach (var activity in combat.Activities.Concat(combat.CounterActs))
                {
                    LogActivity(activity);
                    LogConducts(activity);
                }

                foreach (var result in combat.ResultMapper) Log($"{GetOperator(result.Key)}{result.Value}");
            }
        }

        private string OperatorText(int instanceId, bool withHp) => OpText(GetOperator(instanceId), withHp);

        private string OperatorText(ChessProcess process, bool withHp) =>
            OpText(Grid.GetChessPos(process.Major, process.Scope == 0).Operator, withHp);

        private string OpText(IChessOperator op,bool withHp)
        {
            if (op == null) return string.Empty;
            var statText = string.Empty;
            if(withHp)
            {
                var stat = GetStatus(op);
                statText = $"[{stat.Hp}/{stat.MaxHp}]Pos({stat.Pos})";
            }
            return $"{op.InstanceId}.{op}{statText}";
        }
        public CombatMapper CurrentCombatMapper { get; private set; }

        public bool IsMajorTarget(ChessOperator target)
        {
            var activity = CurrentCombatMapper.Activities.First();
            return activity.To == target.InstanceId;
        }

        private bool isCounterFlagged;
        private ActivityResult ProcessActivityResult(Activity activity)
        {
            var target = GetOperator(activity.To);
            var offender = activity.From < 0 ? null : GetOperator(activity.From);
            var result = GetOperator(target.InstanceId).Respond(activity, offender);
            result.SetStatus(GetFullCondition(target));
            var resultMapper = isCounterFlagged
                ? CurrentCombatMapper.CounterResultMapper
                : CurrentCombatMapper.ResultMapper;
            if (!resultMapper.ContainsKey(target.InstanceId))
                resultMapper.Add(target.InstanceId, result);
            else
                resultMapper[target.InstanceId] = result;

            if (activity.To >= 0 && result.IsDeath)
            {
                var death = GetOperator(activity.To);
                foreach (var op in StatusMap.Keys.Where(op => op != death && op.IsAlive))
                    op.OnSomebodyDie(death);
                Log($"@@@@【{death}败退】@@@@！");
            }

            UpdateTerrain(GetChessPos(target));
            return result;
        }

        private void LogActivity(Activity activity)
        {
            var statText = string.Empty;
            if (activity.TargetStatus != null)
            {
                var stat = activity.TargetStatus;
                statText = $"[{stat.Hp}/{stat.MaxHp}]Pos({stat.Pos})";
            }
            var targetText = string.Empty;
            if (activity.Intent != Activity.Sprite)
                targetText = $"目标：{OperatorText(activity.To, false)}{statText}";
            Log($"【活动】{activity.StanceText()}@{activity.IntentText()} {targetText}");
        }

        private void LogConducts(Activity activity)
        {
            foreach (var conduct in activity.Conducts) Log($"一招--->{conduct}");
        }
        #endregion

        #region ChessboardConductPipeline

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


        public bool OnCounterTriggerPass(ChessOperator counterOp,ChessOperator counterTarget)
        {
            if (GetStatus(counterTarget).IsDeath ||
                GetStatus(counterOp).IsDeath ||
                counterTarget.Style.Type == CombatStyle.Types.Range ||
                counterTarget.Style.ArmedType < 0 ||
                counterOp.Style.Type == CombatStyle.Types.Range || 
                counterTarget.IsNotCounterAble(counterOp))
                return false;
            return !GetBuffOperator(b => b.IsDisableCounter(counterOp)).Any();
        }

        public ActivityResult OnOffensiveActivity(Activity activity, ChessOperator op, IChessOperator off)
        {
            var offender = GetOperator(off.InstanceId);
            var result = ActivityResult.Instance(ActivityResult.Types.Suffer);
            //闪避判定
            if (offender.CardType == GameCardType.Hero &&
                activity.Intent != Activity.Inevitable &&
                OnDodgeTriggerPass(op, offender))
            {
                result.Result = (int)ActivityResult.Types.Dodge;
            }
            else
            {
                var state = ShieldFilter(op, offender, activity);
                result.Result = (int)state;
                //元素特性
                foreach (var conduct in activity.Conducts)
                {
                    if (conduct.Kind == CombatConduct.ElementDamageKind) continue;
                    foreach (var bo in GetBuffOperator(b => b.IsElementTrigger))
                        bo.OnElementConduct(op, conduct);
                }
                /***执行Activities***/
                op.ProceedActivity(activity, result.Type);
            }

            result.Status = GetStatus(op);
            return result;
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

        private ActivityResult.Types ShieldFilter(ChessOperator op, ChessOperator offender, Activity activity)
        {
            if (GetCondition(op,CardState.Cons.Shield) > 0)
            {
                if (offender.IsIgnoreShieldUnit) return ActivityResult.Types.Suffer;
                if (activity.Conducts.Where(c => c.Kind == CombatConduct.DamageKind)
                    .All(c => Damage.GetKind(c) != Damage.Kinds.Physical))
                    return ActivityResult.Types.Suffer;
                //如果被护盾免伤后，扣除护盾1层
                activity.Conducts.Add(CombatConduct.InstanceBuff(op.InstanceId, CardState.Cons.Shield, -1));
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
        public void OnCombatMiddlewareConduct(ChessOperator op, CombatConduct conduct)
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

            if (armor < 0) armor = 0;
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

        #endregion

        #region Ratio Methods

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
        public bool IsRandomPass(int ratio, int range = 100) => random.Next(1, range) <= ratio;

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

        #endregion

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
        public IChessPos GetChessPos(bool isChallenger, int pos) => Grid.GetChessPos(pos, isChallenger);

        public IChessPos GetContraTarget(ChessOperator op)
        {
            return Grid.GetContraPositionInSequence(
                GetCondition(op, CardState.Cons.Confuse) > 0 ? op.IsChallenger : !op.IsChallenger,
                GetStatus(op).Pos,
                p => p.IsPostedAlive && p.Operator != op);
        }

        public enum Targeting
        {
            /// <summary>
            /// 对位
            /// </summary>
            Contra,
            /// <summary>
            /// 任何目标
            /// </summary>
            Any,
            /// <summary>
            /// 任何英雄
            /// </summary>
            AnyHero,
            LowHp,

        }

        public IChessPos GetTargetByMode(ChessOperator op,Targeting mode)
        {
            IChessPos chessPos = null;
            switch (mode)
            {
                case Targeting.Contra:
                    chessPos = GetContraTarget(op);break;
                case Targeting.Any:
                    chessPos = GetRivals(op).RandomPick();
                    if (chessPos == null)
                        chessPos = GetContraTarget(op);
                    break;
                case Targeting.AnyHero:
                    chessPos = GetRivals(op, p => p.IsAliveHero).RandomPick();
                    if (chessPos == null)
                        chessPos = GetContraTarget(op);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            return chessPos;
        }

        public IEnumerable<IChessPos> GetAttackPath(ChessOperator op)
        {
            var isChallenger = GetCondition(op, CardState.Cons.Confuse) > 0 ? op.IsChallenger : !op.IsChallenger;
            return Grid.GetAttackPath(isChallenger, GetStatus(op).Pos).Where(p => p.Operator != op);
        }

        public IEnumerable<IChessPos> GetFriendlyNeighbors(ChessOperator op)
        {
            var isChallenger = GetCondition(op, CardState.Cons.Confuse) > 0 ? !op.IsChallenger : op.IsChallenger;
            return Grid.GetNeighbors(GetStatus(op).Pos, isChallenger);
        }

        public IEnumerable<IChessPos> GetContraNeighbors(ChessOperator op)
        {
            var isChallenger = GetCondition(op, CardState.Cons.Confuse) > 0 ? op.IsChallenger : !op.IsChallenger;
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

        public IEnumerable<PosSprite> GetSpriteInChessPos(int pos, bool isChallenger) => Grid.GetChessPos(pos, isChallenger).Terrain.Sprites;
        public IEnumerable<PosSprite> GetSpriteInChessPos(ChessOperator op) => GetChessPos(op).Terrain.Sprites;

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