using System;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using Microsoft.Extensions.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using ILogger = Microsoft.Extensions.Logging.ILogger;
public class NewWarManager : MonoBehaviour,ILogger
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
        ChessOperator = new ChessOperatorManager<FightCardData>(Grid,this);
        RegCards(Player, true);
        RegCards(Enemy, false);
        ChessOperator.RoundConfirm();
        void RegCards(ChessCard[] list, bool isChallenger)
        {
            foreach (var chessCard in list)
            {
                var card = new FightCardData(GameCard.Instance(chessCard.Id, (int)chessCard.Type, chessCard.Level));
                card.UpdatePos(chessCard.Pos);
                card.isPlayerCard = isChallenger;
                card = ChessOperator.RegOperator(card);
                CardData.Add(card.InstanceId, card);
            }
        }
    }

    public void StartButtonShow(bool show)
    {
        StartButton.GetComponent<Animator>().SetBool(ButtonTrigger, show);
        StartButton.interactable = show;
    }

    [Serializable]
    public class ChessCard
    {
        public int Id;
        public int Pos;
        public GameCardType Type;
        public int Level = 1;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
                Debug.Log(formatter(state,exception));
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