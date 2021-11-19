using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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


    void Start()
    {
        DataTable.Init();
        PlayerData.Init();
        gameResources = new GameResources();
        gameResources.Init();
        EffectsPooling.Init();

        Init(chessboard, jbAnimationManager);
        NewWar.NewGame();
        NewWar.ConfirmEnemy();
        NewWar.ConfirmPlayer();
        GenerateChessmanFromList();
        //var playerBase = NewWar.Player.FirstOrDefault(c => c.Type == GameCardType.Base);
        //if (playerBase == null) Debug.LogError("玩家老巢未设置。");
        //var pBase = FightCardData.PlayerBaseCard(playerBase.Level);
        //var enemyBase = NewWar.Enemy.FirstOrDefault(c => c.Type == GameCardType.Base);
        //if (enemyBase == null) Debug.LogError("敌方老巢未设置。");
        //var eBase = FightCardData.PlayerBaseCard(enemyBase.Level);
        //SetPlayerChess(pBase, NewWar.Player);
        //SetEnemyChess(eBase, NewWar.Enemy);
    }

    private void GenerateChessmanFromList()
    {
        foreach (var card in NewWar.CardData.Values) InstanceChessman(card);
    }

}