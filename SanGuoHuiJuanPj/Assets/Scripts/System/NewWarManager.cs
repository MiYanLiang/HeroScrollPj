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

    public Button StartButton;
    public ChessPos[] PlayerPoses;
    public ChessPos[] EnemyPoses;
    public ChessCard[] Enemy;
    public ChessCard[] Player;
    #endregion

    public ChessGrid Grid;
    public ChessOperatorManager<FightCardData> ChessOperator;
    public Dictionary<int, FightCardData> CardData { get; } = new Dictionary<int, FightCardData>();

    public void Init()
    {
        Grid = new ChessGrid(PlayerPoses, EnemyPoses);
        ChessOperator = new ChessOperatorManager<FightCardData>(Grid, this);
    }
    private void RegChessman(ChessCard[] list, bool isChallenger)
    {
        foreach (var chessCard in list)
        {
            var card = new FightCardData(GameCard.Instance(chessCard.Id, (int)chessCard.Type, chessCard.Level));
            card.UpdatePos(chessCard.Pos);
            card.isPlayerCard = isChallenger;
            //card = ChessOperator.RegOperator(card);
            RegCard(card);
        }

        var playerBase = CardData.Values.FirstOrDefault(c => c.isPlayerCard && c.CardType == GameCardType.Base);
        var enemyBase = CardData.Values.FirstOrDefault(c => !c.isPlayerCard && c.CardType == GameCardType.Base);
        if (playerBase != null && !playerBase.Status.IsDeath &&
            enemyBase != null && !enemyBase.Status.IsDeath)
            ChessOperator.RoundConfirm();
    }
    /// <summary>
    /// 读取Player列表的卡牌信息
    /// </summary>
    public void ConfirmPlayer() => RegChessman(Player, true);
    /// <summary>
    /// 读取enemy列表的卡牌信息
    /// </summary>
    public void ConfirmEnemy() => RegChessman(Enemy, false);

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
