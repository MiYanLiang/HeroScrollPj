using System;
using System.Collections;
using CorrelateLib;
using Newtonsoft.Json;
using UnityEngine;

public class ChessmanTester : MonoBehaviour
{
    public EffectsPoolingControl EffectsPooling;
    public DataTable DataTable;
    public TestCard Self;
    public TestCard Target;
    private FightCardData CardData;
    private FightCardData TargetData;
    public WarGameCardUi SelfUi;
    public WarGameCardUi TargetUi;
    public NewWarManager NewWar;
    

    private GameResources gameResources;
    void Start()
    {
        DataTable.Init();
        gameResources = new GameResources();
        gameResources.Init();
        EffectsPooling.Init();
        CardData = InitCard(Self, SelfUi, true, NewWar);
        TargetData = InitCard(Target, TargetUi, false, NewWar);
    }

    private static FightCardData InitCard(TestCard self,WarGameCardUi ui,bool isPlayer,NewWarManager mgr)
    {
        var card = GameCard.Instance(self.Id, self.Type, self.Level);
        var data = new FightCardData(card);
        data.isPlayerCard = isPlayer;
        data.cardObj = ui;
        ui.Init(card);
        var op = mgr.ChessOperator.RegOperator(data);
        var selfPos = ui.GetComponentInParent<ChessPos>();
        op.SetPos(selfPos.Pos);
        return data;
    }

    public void InvokeCard()
    {
        var round = NewWar.ChessOperator.StartRound();
        //var opMgr = new ChessOperatorManager();
        //Operator = ChessOperatorManager.GetWarCard(CardData);
    }

    //private IEnumerator Fight(ChessmanOperation player,ChessmanOperation target)
    //{
    //    yield return Operator.MainOperation(player);
    //    yield return new WaitForSeconds(1);
    //    yield return Operator.MainOperation(target);
    //}
    [Serializable]
    public class TestCard
    {
        public int Id;
        public int Type;
        public int Level;
    }
}