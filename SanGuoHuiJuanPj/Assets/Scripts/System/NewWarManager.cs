using System;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using Microsoft.Extensions.Logging;
using UnityEngine;
using UnityEngine.UI;
using ILogger = Microsoft.Extensions.Logging.ILogger;
public class NewWarManager : MonoBehaviour, ILogger
{
    #region UnityFields

    public ChessPos[] PlayerPoses { get; set; }
    public ChessPos[] EnemyPoses { get; set; }
    public ChessCard[] Enemy;
    public List<ChessCard> Player;
    #endregion

    public ChessGrid Grid;
    public ChessOperatorManager<FightCardData> ChessOperator;

    public void Init(Chessboard chessboard)
    {
        PlayerPoses = chessboard.PlayerScope;
        EnemyPoses = chessboard.EnemyScope;
        Grid = new ChessGrid(PlayerPoses, EnemyPoses);
    }
    /// <summary>
    /// 棋盘3部曲 1.新游戏
    /// </summary>
    public void NewGame()
    {
        foreach (var chessPos in PlayerPoses.Concat(EnemyPoses)) chessPos.ResetPos();
#if UNITY_EDITOR
        ChessOperator = new ChessOperatorManager<FightCardData>(Grid, DataTable.Hero.Values, DataTable.Tower.Values,
            DataTable.Trap.Values, DataTable.Military.Values, DataTable.JiBan.Values, DataTable.BaseLevel.Values, this);
#else
        ChessOperator = new ChessOperatorManager<FightCardData>(Grid, DataTable.Hero.Values, DataTable.Tower.Values,
            DataTable.Trap.Values, DataTable.Military.Values, DataTable.JiBan.Values, DataTable.BaseLevel.Values);
#endif
    }

    private List<FightCardData> RegChessmanList(ChessCard[] list, bool isChallenger)
    {
        var cards = new List<FightCardData>();
        foreach (var chessCard in list) cards.Add(RegChessCard(chessCard, isChallenger));
        return cards;
    }

    public FightCardData RegChessCard(ChessCard chessCard, bool isChallenger, int customHp = 0)
    {
        var card = new FightCardData(GameCard.Instance(cardId: chessCard.Id, type: (int)chessCard.Type, level: chessCard.Level,
            arouse: chessCard.Arouse, deputy1Id: 0, deputy1Level: 0, deputy2Id: 0, deputy2Level: 0, deputy3Id: 0, deputy3Level: 0, deputy4Id: 0, deputy4Level: 0));
        card.SetPos(chessCard.Pos);
        card.isPlayerCard = isChallenger;
        if (customHp > 0) card.ResetHp(customHp);

        //card = ChessOperator.RegOperator(card);
        RegCard(card);
        return card;
    }
    /// <summary>
    /// 棋盘3部曲 3b. 读取Player列表的卡牌信息
    /// (如果不用列表可以一个一个玩家注册)<see cref="RegCard"/>
    /// </summary>
    public List<FightCardData> ConfirmInstancePlayers() => RegChessmanList(Player.ToArray(), true);

    /// <summary>
    /// 棋盘3部曲 2.
    /// 读取enemy列表的卡牌信息
    /// </summary>
    public List<FightCardData> ConfirmInstanceEnemies() => RegChessmanList(Enemy, false);

    public void RegCard(FightCardData card) => ChessOperator.RegOperator(card);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
                Debug.Log(formatter(state, exception));
                break;
            case LogLevel.Warning:
                Debug.LogWarning(formatter(state, exception));
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                Debug.LogError(formatter(state, exception));
                break;
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) => null;
}

[Serializable]
public class ChessCard
{
    public static ChessCard Instance(int id, int type, int level, int arouse, int pos) =>
        Instance(id, (GameCardType)type, level, arouse, pos);
    public static ChessCard Instance(int id, GameCardType type, int level, int arouse,int pos) =>
        new ChessCard { Id = id, Level = level, Type = type, Arouse = arouse, Pos = pos};
    public int Id;
    public int Pos;
    public GameCardType Type;
    public int Level = 1;
    public int Arouse;
}
