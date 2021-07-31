using System.Collections;
using CorrelateLib;
using Newtonsoft.Json;
using UnityEngine;

public class ChessmanTester : MonoBehaviour
{
    public EffectsPoolingControl EffectsPooling;
    public DataTable DataTable;
    public PieceOperator Operator;
    public FightCardData CardData;
    private FightCardData TargetCard;
    public WarGameCardUi Target;
    

    private GameResources gameResources;
    void Start()
    {
        DataTable.Init();
        gameResources = new GameResources();
        gameResources.Init();
        EffectsPooling.Init();
        var jCard = JsonConvert.SerializeObject(CardData,new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        });
        //CardData.isPlayerCard = true;
        var card = GameCard.Instance(CardData.cardId, CardData.cardType, CardData.cardGrade);
        TargetCard = Json.Deserialize<FightCardData>(jCard);
        TargetCard.cardObj = Target;
        Target.Init(card);
        CardData.cardObj.Init(card);
    }

    public void InvokeCard()
    {
        var opMgr = new ChessOperatorManager();
        Operator = ChessOperatorManager.GetWarCard(CardData);
    }

    //private IEnumerator Fight(ChessmanOperation player,ChessmanOperation target)
    //{
    //    yield return Operator.MainOperation(player);
    //    yield return new WaitForSeconds(1);
    //    yield return Operator.MainOperation(target);
    //}
}