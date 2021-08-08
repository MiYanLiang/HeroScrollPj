using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Timeline;

namespace Assets.System.WarModule
{
    public interface IChessboardOperator<TChess> where TChess : class, IChessman, new()
    {
        ChessGrid<TChess> Grid { get; }
        IChessOperator<TChess> GetOperator(IChessPos<TChess> chessPos);
        /// <summary>
        /// 从数据表调出触发率，并给出随机判断
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool RandomFromConfigTable(int id);
        /// <summary>
        /// 调出数值的百分比转化成点数
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        float ConfigPercentage(int id);
        /// <summary>
        /// 调出数值表的值
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        float ConfigValue(int id);

        /// <summary>
        /// 回合结束加金币
        /// </summary>
        /// <param name="op"></param>
        /// <param name="to">如果是玩家资源维度的,-1 = 己方，-2 = 对方。正数为棋格</param>
        /// <param name="value"></param>
        void RegGoldOnRoundEnd(IChessOperator<TChess> op, int to, int value);

        /// <summary>
        /// 回合结束添加战役宝箱
        /// </summary>
        /// <param name="op"></param>
        /// <param name="to">如果是玩家资源维度的,-1 = 己方，-2 = 对方。正数为棋格</param>
        /// <param name="warChests"></param>
        void RegWarChestOnRoundEnd(IChessOperator<TChess> op, int to, int[] warChests);
        /// <summary>
        /// 根据武将表的值反馈是否触发特殊攻击。
        /// 1 = 暴击，2 = 会心
        /// </summary>
        /// <param name="heroId">武将Id</param>
        /// <param name="type">1 = 暴击，2 = 会心</param>
        /// <returns></returns>
        bool RandomFromHeroTable(int heroId,int type);
        /// <summary>
        /// Randomize
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        bool IsRandomPass(int ratio, int range = 100);

        /// <summary>
        /// Randomize
        /// </summary>
        /// <param name="excludedMax"></param>
        /// <returns></returns>
        int Randomize(int excludedMax);

        ActivityResult ActionRespondResult(IChessOperator<TChess> offender, IChessPos<TChess> target, int intent,
            CombatConduct[] conducts);

        /// <summary>
        /// 生成回合触发器
        /// </summary>
        /// <param name="op"></param>
        /// <param name="to">如果是玩家资源维度的,-1 = 己方，-2 = 对方。正数为棋格</param>
        /// <param name="intent"><see cref="Activity"/>Intent</param>
        /// <param name="conducts"></param>
        /// <returns></returns>
        Activity InstanceRoundAction(IChessOperator<TChess> op,int to,int intent, CombatConduct[] conducts);

        PieceStatus GetStatus(IChessPos<TChess> chessman);
    }

    /// <summary>
    /// 棋盘处理器。主要处理<see cref="IChessOperator{TChess}"/>的交互与进程逻辑。
    /// </summary>
    /// <typeparam name="TChess"></typeparam>
    public abstract class ChessboardOperator<TChess> : IChessboardOperator<TChess> where TChess : class, IChessman, new() 
    {
        public ChessGrid<TChess> Grid { get; }

        private int _roundId = 0;
        public int RoundId => _roundId;
        private bool IsChallengerOdd => _isChallengerOdd;
        public bool IsOddRound => _roundId % 2 == 0;

        public IReadOnlyList<ChessRound> Rounds => rounds;
        private ChessRound currentRound;

        #region Static Fields
        private static Random random = new Random();
        private static readonly ChessPosProcess[] EmptyProcess = new ChessPosProcess[0];
        private readonly bool _isChallengerOdd;
        private readonly List<ChessRound> rounds;
        private static int PieceProcessSeed = 0;
        private static int ActivitySeed = 0;
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
        protected static T[] Singular<T>(T t) => new[] { t };
        #endregion

        protected ChessboardOperator(bool isChallengerFirst, ChessGrid<TChess> grid)
        {
            _isChallengerOdd = isChallengerFirst;
            rounds = new List<ChessRound>();
            Grid = grid;
        }

        public bool IsChallengerRound(int roundId = -1)
        {
            if (roundId < 0)
                roundId = RoundId;
            var isOdd = roundId % 2 == 0;
            return _isChallengerOdd == isOdd;
        }

        public ChessRound StartRound()
        {
            //instance Round
            //invoke pre-action
            //Get all sorted Chessman operators
            //invoke Chessman operations
            //invoke finalization
            RecursiveActionCount = 0;
            currentRound = new ChessRound
            {
                InstanceId = RoundId,
                PreAction = new RoundAction(),
                FinalAction = new RoundAction(),
            };

            if (IsOddRound)//双数激活双方回合
            {
                var preActions = GetPreRoundTriggerByOperators();
                currentRound.PreAction.Concat(preActions);
                foreach (var activity in currentRound.PreAction.Activities)
                    RoundActionInvocation(activity.Key, activity.Value);
            }

            var sortedOperators = GetSortedChessmanOperators();
            //UpdatePosesBuffs(currentRound.PreAction, false);
            var roundProcesses = new List<ChessPosProcess>();
            for (var i = 0; i < sortedOperators.Length; i++)
            {
                var chessPos = sortedOperators[i];
                if (chessPos.Chessman == null) continue;
                var process = PosInvocation(chessPos);
                if (process == null) continue;
                roundProcesses.Add(process);
            }

            currentRound.Processes = roundProcesses.ToArray();

            if(!IsOddRound)//单数激活双方回合的结束
            {
                var finalAction = GetRoundEndTriggerByOperators();
                currentRound.FinalAction.Concat(finalAction);
                foreach (var activity in currentRound.FinalAction.Activities)
                    RoundActionInvocation(activity.Key, activity.Value);
            }

            //UpdatePosesBuffs(currentRound.FinalAction, true);
            _roundId++;
            return currentRound;
        }

        #region RoundActivities


        protected abstract RoundAction GetRoundEndTriggerByOperators();

        protected abstract RoundAction GetPreRoundTriggerByOperators();

        protected abstract void RoundActionInvocation(int roundKey, IEnumerable<Activity> activities);

        // 注册当回合结束执行的全局逻辑
        private void RegRoundEnd(int id,IEnumerable<Activity> activity) => AddRoundResourceHelper(currentRound.FinalAction.Activities, id, activity);

        public void RegGoldOnRoundEnd(IChessOperator<TChess> op, int to, int value)
        {
            RegRoundEnd(RoundAction.PlayerResources,
                Singular(Activity.Instance(
                    ActivitySeed,
                    currentRound.InstanceId,
                    ResourceTarget(op.Chessman.IsPlayer,to, RoundId),
                    Activity.PlayerResource,
                    Singular(CombatConduct.InstancePlayerResource(-1, value)))));
        }

        public void RegWarChestOnRoundEnd(IChessOperator<TChess> op, int to, int[] warChests)
        {
            RegRoundEnd(RoundAction.PlayerResources,
                Singular(Activity.Instance(ActivitySeed,
                    RoundId,
                    ResourceTarget(op.Chessman.IsPlayer, to, RoundId),
                    Activity.PlayerResource,
                    warChests.Select(CombatConduct.InstancePlayerResource).ToArray()
                )));
        }

        //注册助手
        private static void AddRoundResourceHelper(Dictionary<int, List<Activity>> dic, int id, IEnumerable<Activity> activity)
        {
            if (!dic.ContainsKey(id))
                dic.Add(id, new List<Activity>());
            dic[id].AddRange(activity);
        }

        #endregion

        public abstract IChessOperator<TChess> GetOperator(IChessPos<TChess> chessPos);
        //棋子的全局状态更新，用于回合前与回合结束
        //private void UpdatePosesBuffs(RoundAction roundAction,bool isLastRoundActivities)
        //{
        //    var activities = roundAction.Activities.SelectMany(a => a.Value).ToArray();
        //    var friendlyActivities = activities.Where(a => a.Intent == Activity.Friendly ||
        //                                                   a.Intent == Activity.FriendlyTrigger ||
        //                                                   a.Intent == Activity.Self).ToArray();
        //    var oppositeActivities = activities.Except(friendlyActivities);
        //    //last round scope == current round
        //    var roundScope = isLastRoundActivities ? IsChallengerRound : !IsChallengerRound;
        //    var fList = friendlyActivities.Join(Grid.GetScope(roundScope), 
        //        a => a.To, p => p.Key, (a, p) => (a, p));
        //    var oList = oppositeActivities.Join(Grid.GetRivalScope(roundScope),
        //        a => a.To, p => p.Key, (a, p) => (a, p));
        //    ListInvoke(fList);//Invoke Friendly Activities
        //    ListInvoke(oList);//Invoke Opposite Activities
        //    //var list = roundAction.Activities.Join(posList, s => s.Value.To, p => p.Chessman.Pos,(s,p)=> new {s, p}).ToList();
        //    void ListInvoke(IEnumerable<(Activity a, KeyValuePair<int, IChessPos<TChess>> p)> list)
        //    {
        //        foreach (var obj in list)
        //        {
        //            var action = obj.a;
        //            if (obj.p.Value == null) continue;
        //            var chessPos = obj.p;
        //            var op = GetOperator(chessPos.Value);
        //            // Game card operator respond to the action;
        //            var result = op.UpdateConducts(action, null);
        //            var actions = result.Activities.ToList();
        //            //状态触发没有执行
        //            RecursiveAction(null, result.Activities, actions);
        //        }
        //    }
        //}

        private ChessPosProcess CurrentProcess { get; set; }
        private ChessPosProcess PosInvocation(IChessPos<TChess> chessPos)
        {
            //invoke pos operation & return pieceProcess
            //finalize pieceProcess by interactive invocation
            var op = GetOperator(chessPos);
            CurrentProcess = ChessPosProcess.Instance(ActivitySeed, op.Status.Clone());
            op.StartActions();
            PieceProcessSeed++;
            return CurrentProcess;
        }

        protected IChessPos<TChess> GetTarget(Activity ac)
        {
            var isOpposite = ac.Intent == Activity.Offensive ||
                             ac.Intent == Activity.Counter ||
                             ac.Intent == Activity.OffendTrigger;

            var isChallenger = IsChallengerRound(ac.RoundId) ? !isOpposite : isOpposite;
            return Grid.GetChessPos(ac.To, isChallenger);
        }

        private IChessPos<TChess>[] GetSortedChessmanOperators() =>
            Grid.GetScope(IsChallengerRound()).OrderBy(o => o.Key).Select(o => o.Value).ToArray();

        public int Randomize(int excludedMax) => random.Next(excludedMax);

        public ActivityResult ActionRespondResult(IChessOperator<TChess> offender, IChessPos<TChess> target, int intent,
            CombatConduct[] conducts)
        {
            RecursiveActionCount++;
            var activity = Activity.Instance(ActivitySeed, RoundId, target.Pos, intent, conducts);
            ActivitySeed++;
            CurrentProcess.Actions.Add(activity);
            var op = GetOperator(target);
            activity.Result = op.Respond(activity, offender);
            return activity.Result;
        }

        public Activity InstanceRoundAction(IChessOperator<TChess> op, int to, int intent, CombatConduct[] conducts) =>
            Activity.Instance(ActivitySeed, RoundId, ResourceTarget(op.Chessman.IsPlayer, to, RoundId), intent, conducts);

        public PieceStatus GetStatus(IChessPos<TChess> chessman) => GetOperator(chessman)?.Status;

        /// <summary>
        /// 找出该回合中指定的对象
        /// </summary>
        /// <param name="isChallenger"></param>
        /// <param name="to"></param>
        /// <param name="roundId"></param>
        /// <returns></returns>
        private int ResourceTarget(bool isChallenger, int to, int roundId)
        {
            var target = 0;
            if(!isChallenger)
            {
                to -= 1;
                if (to < -2)
                    to = -1;
            }
            switch (to)
            {
                case -1 when IsChallengerRound(roundId):
                    target = -1;
                    break;
                case -1:
                case -2 when IsChallengerRound(roundId):
                    target = -2;
                    break;
                case -2:
                    target = -1;
                    break;
            }
            return target;
        }

        public bool RandomFromConfigTable(int id) => IsRandomPass(DataTable.GetGameValue(id));

        public bool RandomFromHeroTable(int heroId,int type)
        {
            switch (type)
            {
                case 1: return IsRandomPass(DataTable.Hero[heroId].CriticalRatio);
                case 2: return IsRandomPass(DataTable.Hero[heroId].RouseRatio);
            }
            return false;
        }

        public float ConfigPercentage(int id) => ConfigValue(id) * 0.01f;

        public float ConfigValue(int id) => DataTable.GetGameValue(id);

        // Random Range
        public bool IsRandomPass(int ratio, int range = 100) => random.Next(0, range) <= ratio;
    }
}