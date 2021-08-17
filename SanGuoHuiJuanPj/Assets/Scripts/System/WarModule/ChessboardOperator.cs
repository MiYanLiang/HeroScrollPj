using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Timeline;

namespace Assets.System.WarModule
{
    public interface IChessboardOperator
    {
        ChessGrid Grid { get; }
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
        void RegGoldOnRoundEnd(ChessOperator op, int to, int value);

        /// <summary>
        /// 回合结束添加战役宝箱
        /// </summary>
        /// <param name="op"></param>
        /// <param name="to">如果是玩家资源维度的,-1 = 己方，-2 = 对方。正数为棋格</param>
        /// <param name="warChests"></param>
        void RegWarChestOnRoundEnd(ChessOperator op, int to, int[] warChests);
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
        /// <summary>
        /// 回应行动
        /// </summary>
        /// <param name="offender"></param>
        /// <param name="target"></param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="skill">如果是普通攻击，标记0，大于0将会是技能值</param>
        /// <param name="rePos"></param>
        /// <returns></returns>
        ActivityResult ActionRespondResult(ChessOperator offender, IChessPos target, int intent,
            CombatConduct[] conducts, int skill, int rePos = -1);

        /// <summary>
        /// 生成回合触发器
        /// </summary>
        /// <param name="op"></param>
        /// <param name="to">如果是玩家资源维度的,-1 = 己方，-2 = 对方。正数为棋格</param>
        /// <param name="intent"><see cref="Activity"/>Intent</param>
        /// <param name="conducts"></param>
        /// <returns></returns>
        Activity InstanceRoundAction(ChessOperator op,int to,int intent, CombatConduct[] conducts);

        PieceStatus GetStatus(IChessPos pos);
    }

    /// <summary>
    /// 棋盘处理器。主要处理<see cref="IChessOperator{TChess}"/>的交互与进程逻辑。
    /// </summary>
    /// <typeparam name="TChess"></typeparam>
    public abstract class ChessboardOperator : IChessboardOperator
    {
        public ChessGrid Grid { get; }

        private int _roundId = 0;
        public int RoundId => _roundId;
        private bool IsChallengerOdd => _isChallengerOdd;

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

        protected ChessboardOperator(bool isChallengerFirst, ChessGrid grid)
        {
            _isChallengerOdd = isChallengerFirst;
            rounds = new List<ChessRound>();
            Grid = grid;
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
                InstanceId = RoundId,
                PreAction = new RoundAction(),
                FinalAction = new RoundAction(),
            };

                var preActions = GetPreRoundTriggerByOperators();
                currentRound.PreAction.Concat(preActions);
                foreach (var activity in currentRound.PreAction.Activities)
                    RoundActionInvocation(activity.Key, activity.Value);
            var sortedOperators = GetSortedOperators();
            //UpdatePosesBuffs(currentRound.PreAction, false);
            var roundProcesses = new List<ChessPosProcess>();
            for (var i = 0; i < sortedOperators.Length; i++)
            {
                var op = sortedOperators[i];
                if (op.Status.IsDeath) continue;
                var process = PosInvocation(GetOperator(op.InstanceId));
                if (process == null) continue;
                roundProcesses.Add(process);
            }

            currentRound.Processes = roundProcesses.ToArray();

                var finalAction = GetRoundEndTriggerByOperators();
                currentRound.FinalAction.Concat(finalAction);
                foreach (var activity in currentRound.FinalAction.Activities)
                    RoundActionInvocation(activity.Key, activity.Value);

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

        public void RegGoldOnRoundEnd(ChessOperator op,int to ,int value)
        {
            RegRoundEnd(RoundAction.PlayerResources,
                Singular(Activity.Instance(
                    ActivitySeed,
                    CurrentProcess.InstanceId,
                    op.Pos,
                    op.IsChallenger ? 0 : 1,
                    ResourceTarget(op, to == -1),
                    Activity.PlayerResource,
                    Singular(CombatConduct.InstancePlayerResource(-1, value))
                    , 0, -1)));
        }

        public void RegWarChestOnRoundEnd(ChessOperator op,int to ,int[] warChests)
        {
            RegRoundEnd(RoundAction.PlayerResources,
                Singular(Activity.Instance(ActivitySeed,
                    CurrentProcess.InstanceId,
                    op.Pos,
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
        //    //var list = roundAction.Activities.Join(posList, s => s.Value.To, p => p.this.Pos,(s,p)=> new {s, p}).ToList();
        //    void ListInvoke(IEnumerable<(Activity a, KeyValuePair<int, IChessPos> p)> list)
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
        private ChessPosProcess PosInvocation(ChessOperator op)
        {
            //invoke pos operation & return pieceProcess
            //finalize pieceProcess by interactive invocation
            CurrentProcess = ChessPosProcess.Instance(ActivitySeed, op.Pos, op.IsChallenger);
            op.StartActions();
            PieceProcessSeed++;
            return CurrentProcess;
        }

        protected IChessPos GetTarget(Activity ac)
        {
            //var isOpposite = ac.Intent == Activity.Offensive ||
            //                 ac.Intent == Activity.Counter ||
            //                 ac.Intent == Activity.OffendAttach;
            //
            //var isChallenger = IsChallengerOdd && ac.ProcessId % 2 == 0 ? !isOpposite : isOpposite;
            //return Grid.GetChessPos(ac.To, isChallenger);
            return Grid.GetChessPos(GetOperator(ac.To));
        }
        protected abstract ChessOperator GetOperator(int id);
        private IChessOperator[] GetSortedOperators() =>
            Grid.Challenger.Concat(Grid.Opposite)
                .Where(o => o.Value.IsPostedAlive)
                .OrderBy(o => o.Key)
                .ThenBy(o => o.Value.IsChallenger != IsChallengerOdd)
                .Select(o => o.Value.Operator).ToArray();

        public int Randomize(int excludedMax) => random.Next(excludedMax);

        public ActivityResult ActionRespondResult(ChessOperator offender, IChessPos target, int intent,
            CombatConduct[] conducts,int skill ,int rePos = -1)
        {
            if (target == null) return null;
            RecursiveActionCount++;
            var activity = Activity.Instance(ActivitySeed, CurrentProcess.InstanceId, offender.InstanceId,
                offender.IsChallenger ? 0 : 1,
                target.Operator.InstanceId,
                intent, conducts, skill, rePos);
            ActivitySeed++;
            CurrentProcess.Activities.Add(activity);
            var op = target.Operator;
            if (op == null)
                throw new NullReferenceException(
                    $"Target Pos({target.Pos}) is null! from offender Pos({offender.Pos}) as IsChallenger[{offender.IsChallenger}] type[{offender.GetType().Name}]");
            activity.Result = GetOperator(op.InstanceId).Respond(activity, offender);
            activity.OffenderStatus = op.Status.Clone();
            return activity.Result;
        }

        public Activity InstanceRoundAction(ChessOperator op, int to, int intent, CombatConduct[] conducts) =>
            Activity.Instance(ActivitySeed, CurrentProcess.InstanceId, op.Pos,
                op.IsChallenger ? 0 : 1,
                ResourceTarget(op, to == -1),
                intent, conducts, 0, -1);

        public PieceStatus GetStatus(IChessPos pos) => pos.Operator?.Status;

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