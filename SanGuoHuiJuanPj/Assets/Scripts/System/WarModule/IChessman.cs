using System.Diagnostics.Contracts;
using System.Dynamic;
using CorrelateLib;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋子接口规范
    /// </summary>
    public interface IChessman
    {
        int InstanceId { get; set; }
        int Pos { get; }
        bool IsPlayer { get; }
        int CardId { get; }
        GameCardType CardType { get; }
        GameCardInfo Info { get; }
        int HitPoint { get; }
        int Level { get; }
        ChessStatus Status { get; }
        CombatStyle GetStyle();
    }
    /// <summary>
    /// 棋位接口规范
    /// </summary>
    public interface IChessPos
    {
        ChessTerrain Terrain { get; }
        /// <summary>
        /// 棋子执行代理，一切活动都用此代理数据为基准
        /// </summary>
        IChessOperator Operator { get; }
        int Pos { get; }
        bool IsChallenger { get; }
        bool IsPostedAlive { get; }
        bool IsAliveHero { get; }
        void RemoveOperator();
        void Init(int pos, bool isCChallenger);
        void SetPos(IChessOperator op);
    }
}