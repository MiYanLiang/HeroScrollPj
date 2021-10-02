using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋格管理器
    /// </summary>
    public class ChessGrid
    {
        private readonly Dictionary<int, IChessPos> _challenger;
        private readonly Dictionary<int, IChessPos> opposite;
        public IReadOnlyDictionary<int, IChessPos> Challenger => _challenger;
        public IReadOnlyDictionary<int, IChessPos> Opposite => opposite;
        public int[] FrontRows { get; } = { 0, 1, 2, 3, 4 };

        private static int[][] NeighborCards { get; }= new int[20][] {
            new int[3]{ 2, 3, 5},           //0
            new int[3]{ 2, 4, 6},           //1
            new int[5]{ 0, 1, 5, 6, 7},     //2
            new int[3]{ 0, 5, 8},           //3
            new int[3]{ 1, 6, 9},           //4
            new int[6]{ 0, 2, 3, 7, 8, 10}, //5
            new int[6]{ 1, 2, 4, 7, 9, 11}, //6
            new int[6]{ 2, 5, 6, 10,11,12}, //7
            new int[4]{ 3, 5, 10,13},       //8
            new int[4]{ 4, 6, 11,14},       //9
            new int[6]{ 5, 7, 8, 12,13,15}, //10
            new int[6]{ 6, 7, 9, 12,14,16}, //11
            new int[6]{ 7, 10,11,15,16,17}, //12
            new int[4]{ 8, 10,15,18},       //13
            new int[4]{ 9, 11,16,19},       //14
            new int[5]{ 10,12,13,17,18},    //15
            new int[5]{ 11,12,14,17,19},    //16
            new int[3]{ 12,15,16},          //17
            new int[2]{ 13,15},             //18
            new int[2]{ 14,16},             //19
        };
        //位置列攻击目标选择次序
        private static int[][] AttackPath { get; }= new int[5][]
        {
            new int[11]{ 0, 2, 3, 5, 7, 8, 10,12,13,15,17},     //0列
            new int[11]{ 1, 2, 4, 6, 7, 9, 11,12,14,16,17},     //1列
            new int[12]{ 0, 1, 2, 5, 6, 7, 10,11,12,15,16,17},  //2列
            new int[8] { 0, 3, 5, 8, 10,13,15,17},              //3列
            new int[8] { 1, 4, 6, 9, 11,14,16,17},              //4列
        };

        public ChessGrid(IList<IChessPos> player, IList<IChessPos> enemy)
        {
            _challenger = new Dictionary<int, IChessPos>();
            opposite = new Dictionary<int, IChessPos>();
            for (var i = 0; i < player.Count; i++)
            {
                player[i].Init(i,true);
                _challenger.Add(i, player[i]);
            }
            for (var i = 0; i < enemy.Count; i++)
            {
                enemy[i].Init(i,false);
                opposite.Add(i, enemy[i]);
            }
        }
        public ChessGrid()
        {
            _challenger = new Dictionary<int, IChessPos>();
            opposite = new Dictionary<int, IChessPos>();
        }

        /// <summary>
        /// 设置棋位
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="obj"></param>
        public void SetPos(int pos, IChessOperator obj) => Replace(pos, obj);
        /// <summary>
        /// 替换棋位，如果该位置有棋子，返回棋子，否则返回null
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IChessOperator Replace(int pos, IChessOperator obj)
        {
            var chessPos = GetScope(obj.IsChallenger)[pos];
            var last = chessPos.Operator;
            chessPos.SetPos(obj);
            return last;
        }
        /// <summary>
        /// 移除棋子
        /// </summary>
        public void Remove(int pos, bool isChallenger)
        {
            var scope = GetScope(isChallenger);
            if (!scope.ContainsKey(pos))
                throw new KeyNotFoundException(
                    $"{nameof(ChessGrid)}.{nameof(Remove)}(): [{pos}({(isChallenger ? 0 : 1)})]Key not found!");
            scope[pos].RemoveOperator();
        }

        public IChessPos BackPos(IChessPos pos)
        {
            var scope = GetScope(pos.IsChallenger);
            var backPos = pos.Pos + 5;
            return scope.ContainsKey(backPos) ? scope[backPos] : null;
        }

        public IReadOnlyDictionary<int, IChessPos> GetScope(IChessOperator chessman) => GetScope(chessman.IsChallenger);
        public IReadOnlyDictionary<int, IChessPos> GetScope(bool isChallenger) => isChallenger ? _challenger : opposite;
        public IReadOnlyDictionary<int, IChessPos> GetRivalScope(bool isChallenger) => GetScope(!isChallenger);
        public IReadOnlyDictionary<int, IChessPos> GetRivalScope(IChessOperator chessman) => GetRivalScope(chessman.IsChallenger);

        /// <summary>
        /// 获取改棋位的周围棋位
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="surround"></param>
        /// <returns></returns>
        public int[] GetNeighborIndexes(int pos,int surround = 1)
        {
            var list = NeighborCards[pos].ToList();
            for (var i = 1; i < surround; i++)
            {
                foreach (var n in list.ToArray())
                foreach (var p in NeighborCards[n])
                {
                    if (!list.Contains(p))
                        list.Add(p);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 获取该棋位的周围棋子
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="isChallenger"></param>
        /// <param name="includeEmptyPos"></param>
        /// <param name="surround"></param>
        /// <returns></returns>
        public IEnumerable<IChessPos> GetNeighbors(int pos, bool isChallenger, bool includeEmptyPos = false,
            int surround = 1) =>
            includeEmptyPos
                ? GetNeighborIndexes(pos, surround).Select(i => GetScope(isChallenger)[i])
                : GetNeighborIndexes(pos, surround).Select(i => GetScope(isChallenger)[i]).Where(o => o.IsPostedAlive);

        public IEnumerable<IChessPos> GetNeighbors(IChessPos chessPos, bool includeEmptyPos = false,int surround = 1) => GetNeighbors(chessPos.Pos, chessPos.IsChallenger, includeEmptyPos, surround);

        public IChessPos GetChessPos(int pos, bool isChallenger)
        {
            var scope = GetScope(isChallenger);
            return !scope.ContainsKey(pos) ? null : scope[pos];
        }

        private const int RecursiveProtect = 999;

        /// <summary>
        /// 根据<see cref="conditionFunc"/>条件获取相邻(链)的位置
        /// </summary>
        /// <param name="chessPos"></param>
        /// <param name="isChallenger"></param>
        /// <param name="conditionFunc"></param>
        /// <returns></returns>
        public IEnumerable<IChessPos> GetChained(int chessPos, bool isChallenger,Func<IChessPos, bool> conditionFunc)
        {
            var pos = GetChessPos(chessPos, isChallenger);
            var chained = new List<IChessPos>();
            var neighbors = new List<IChessPos> { pos };
            var test = 0;
            while (neighbors.Count > 0)
            {
                test++;
                if (test > RecursiveProtect)
                    throw new StackOverflowException($"精灵[{nameof(ChainSprite)}]死循环!");
                var nPos = neighbors[0];
                if (chained.Contains(nPos))
                {
                    neighbors.Remove(nPos);
                    continue;
                }
                chained.Add(nPos);
                neighbors = neighbors.Concat(GetNeighbors(nPos).Where(conditionFunc)).Distinct().ToList();
                neighbors.Remove(nPos);
            }
            return chained;
        }

        public IEnumerable<IChessPos> GetFriendlyNeighbors(IChessOperator chessman,int pos) => GetNeighbors(pos, chessman.IsChallenger);

        public IChessPos GetChessmanInSequence(bool isChallenger,Func<IChessPos, bool> condition) => GetScope(isChallenger).OrderBy(c => c.Key).Select(c => c.Value).FirstOrDefault(condition);

        /// <summary>
        /// 获取以对位开始排列的目标,
        /// 默认 p => p.IsPostedAlive
        /// </summary>
        /// <param name="chessman"></param>
        /// <param name="pos"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public IChessPos GetContraPositionInSequence(IChessOperator chessman, int pos,
            Func<IChessPos, bool> condition = null) =>
            GetContraPositionInSequence(!chessman.IsChallenger, pos, condition);

        /// <summary>
        /// 获取以对位开始排列的目标,
        /// 默认 p => p.IsPostedAlive
        /// </summary>
        /// <param name="isChallenger"></param>
        /// <param name="pos"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public IChessPos GetContraPositionInSequence(bool isChallenger, int pos, Func<IChessPos, bool> condition = null)
        {
            if (condition == null) condition = p => p.IsPostedAlive;
            var series = AttackPath[pos % 5];
            return GetScope(isChallenger).Join(series, p => p.Key, s => s, (p, _) => p.Value).FirstOrDefault(condition);
        }

        //public IChessPos GetChessPos(IChessOperator chessman) => GetChessPos(chessman.Pos, chessman.IsPlayer);
        public IChessPos[] GetAttackPath(bool isChallenger, int pos) => GetScope(isChallenger)
            .Join(AttackPath[pos % 5], d => d.Key, i => i, (d, _) => d.Value).ToArray();

        public IChessPos GetChessPos(IChessOperator op,int pos) => GetChessPos(pos, op.IsChallenger);
    }
}