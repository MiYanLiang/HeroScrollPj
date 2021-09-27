using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CorrelateLib;
using Newtonsoft.Json;
using UnityEngine;

public class ChessboardTester : ChessboardManager
{
    [SerializeField] private DataTable DataTable;
    [SerializeField] private EffectsPoolingControl EffectsPooling;
    [SerializeField] private PlayerDataForGame PlayerData;
    [SerializeField] private GameResources gameResources;

    void Start()
    {
        DataTable.Init();
        PlayerData.Init();
        gameResources = new GameResources();
        CardMap = new Dictionary<int, FightCardData>();
        gameResources.Init();
        EffectsPooling.Init();

        NewWar.StartButton.onClick.AddListener(InvokeCard);
        Init();
        NewWar.Init();
        NewWar.ConfirmPlayer();
        NewWar.ConfirmEnemy();
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
}