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
    private const string ButtonTrigger = "isShow";
    #region UnityFields

    public Button StartButton { get; private set; }
    public ChessPos[] PlayerPoses { get; set; }
    public ChessPos[] EnemyPoses { get; set; }
    public ChessCard[] Enemy;
    public List<ChessCard> Player;
    #endregion

    public ChessGrid Grid;
    public ChessOperatorManager<FightCardData> ChessOperator;
    public Dictionary<int, FightCardData> CardData { get; } = new Dictionary<int, FightCardData>();

    public void Init(Chessboard chessboard)
    {
        StartButton = chessboard.StartButton;
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
        ChessOperator = new ChessOperatorManager<FightCardData>(Grid, this);
#else
        ChessOperator = new ChessOperatorManager<FightCardData>(Grid);
#endif
        CardData.Clear();
        StartButtonShow(true);
    }

    private List<FightCardData> RegChessmanList(ChessCard[] list, bool isChallenger)
    {
        var cards = new List<FightCardData>();
        foreach (var chessCard in list) cards.Add(RegChessCard(chessCard, isChallenger));
        return cards;
    }

    private FightCardData RegChessCard(ChessCard chessCard, bool isChallenger)
    {
        var card = new FightCardData(GameCard.Instance(chessCard.Id, (int)chessCard.Type, chessCard.Level));
        card.SetPos(chessCard.Pos);
        card.isPlayerCard = isChallenger;
        //card = ChessOperator.RegOperator(card);
        RegCard(card);
        return card;
    }
    /// <summary>
    /// 棋盘3部曲 3b. 读取Player列表的卡牌信息
    /// (如果不用列表可以一个一个玩家注册)<see cref="RegCard"/>
    /// </summary>
    public void ConfirmPlayer() => RegChessmanList(Player.ToArray(), true);

    /// <summary>
    /// 棋盘3部曲 2.
    /// 读取enemy列表的卡牌信息
    /// </summary>
    public List<FightCardData> ConfirmEnemy() => RegChessmanList(Enemy, false);

    public void RegCard(FightCardData card)
    {
        ChessOperator.RegOperator(card);
        CardData.Add(card.InstanceId, card);
    }
    public void StartButtonShow(bool show)
    {
        StartButton.GetComponent<Animator>().SetBool(ButtonTrigger, show);
        StartButton.interactable = show;
    }

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
    public static ChessCard Instance(int id, int type, int level, int pos) =>
        Instance(id, (GameCardType)type, level, pos);
    public static ChessCard Instance(int id, GameCardType type, int level, int pos) =>
        new ChessCard { Id = id, Level = level, Type = type, Pos = pos };
    public int Id;
    public int Pos;
    public GameCardType Type;
    public int Level = 1;
}
