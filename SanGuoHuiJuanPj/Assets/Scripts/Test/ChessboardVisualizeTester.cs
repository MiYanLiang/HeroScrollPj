using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Assets.System.WarModule;
using CorrelateLib;
using Newtonsoft.Json;
using UnityEngine;

public class ChessboardVisualizeTester : ChessboardVisualizeManager
{
    [SerializeField] private DataTable DataTable;
    [SerializeField] private EffectsPoolingControl EffectsPooling;
    [SerializeField] private PlayerDataForGame PlayerData;
    [SerializeField] private GameResources gameResources;
    [SerializeField] private Chessboard chessboard;
    [SerializeField] private JiBanAnimationManager jbAnimationManager;
    [SerializeField] private NewWarManager NewWar;

    void Start()
    {
        DataTable.Init();
        PlayerData.Init();
        gameResources = new GameResources();
        gameResources.Init();
        EffectsPooling.Init();

        Init(chessboard, jbAnimationManager);
        NewWar.Init(chessboard);
        NewWar.NewGame();
        var enemies = NewWar.ConfirmInstanceEnemies();
        var players = NewWar.ConfirmInstancePlayers();
        _cardData = enemies.Concat(players).ToDictionary(c => c.InstanceId, c => c);
        
        GenerateChessmanFromList();
        chessboard.StartButton.onClick.AddListener(RoundStart);
    }

    public void RoundStart()
    {
        WarBoardUi.StartButtonAnim(false, chessboard.StartButton);
        var round = NewWar.ChessOperator.StartRound();
        StartCoroutine(TestRoundAnim(round));
    }

    private IEnumerator TestRoundAnim(ChessRound chessRound)
    {
        yield return AnimateRound(chessRound, true);
        WarBoardUi.StartButtonAnim(true, chessboard.StartButton);
    }

    private void GenerateChessmanFromList()
    {
        foreach (var card in _cardData.Values) InstanceChessman(card);
    }

}