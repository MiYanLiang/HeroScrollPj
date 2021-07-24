using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Chessboard : MonoBehaviour
{
    [SerializeField]private ChessPos[] PlayerScope;
    [SerializeField]private ChessPos[] EnemyScope;
    //卡牌附近单位遍历次序
    [SerializeField]private int[][] NeighborCards = new int[20][] {
        new int[3]{ 2, 3, 5},           //0
        new int[3]{ 2, 4, 6},           //1
        new int[5]{ 0, 1, 5, 6, 7},     //2
        new int[3]{ 0, 5, 8},           //3
        new int[3]{ 1, 6, 9},           //4
        new int[6]{ 0, 2, 3, 7, 8, 10}, //5
        new int[6]{ 1, 2, 4, 7, 9, 11}, //6
        new int[6]{ 2, 5, 6, 10,11,12}, //7
        new int[4]{ 3, 5, 10,13},       //8
        new int[4]{ 4, 6, 11,14},       //9
        new int[6]{ 5, 7, 8, 12,13,15}, //10
        new int[6]{ 6, 7, 9, 12,14,16}, //11
        new int[6]{ 7, 10,11,15,16,17}, //12
        new int[4]{ 8, 10,15,18},       //13
        new int[4]{ 9, 11,16,19},       //14
        new int[5]{ 10,12,13,17,18},    //15
        new int[5]{ 11,12,14,17,19},    //16
        new int[3]{ 12,15,16},          //17
        new int[2]{ 13,15},             //18
        new int[2]{ 14,16},             //19
    };


    public int PlayerCardsOnBoard => PlayerScope.Count(p => p.Card != null && p.Card.cardType != 522);
    public int EnemyCardsOnBoard => EnemyScope.Count(p => p.Card != null && p.Card.cardType != 522);

    public void Init()
    {
        for (var i = 0; i < PlayerScope.Length; i++) PlayerScope[i].Init(this, i);
        for (var i = 0; i < EnemyScope.Length; i++) EnemyScope[i].Init(this, i);
    }

    public ChessPos[] GetScope(bool isPlayer)=> isPlayer ? PlayerScope : EnemyScope;
    public ChessPos GetCard(int index, bool isPlayer) => GetScope(isPlayer)[index];

    public void PlaceCard(int index, FightCardData card)
    {
        var scope = GetScope(card.isPlayerCard);
        if (scope[index].Card != null) RemoveCard(index, card.isPlayerCard);

        scope[index].SetCard(card);

        card.UpdatePos(index);
        if (card.isPlayerCard) card.cardObj.DragComponent?.ResetPos();
    }

    public FightCardData RemoveCard(int index, bool isPlayer)
    {
        var scope = GetScope(isPlayer);
        if (scope[index].Card == null)
            throw XDebug.Throw<Chessboard>($"位置[{index}] 没有卡牌！");
        var card = scope[index].Card;
        card.UpdatePos(-1);
        scope[index].RemoveCard();
        if(isPlayer) card.cardObj.DragComponent?.ResetPos();
        return card;
    }

    public bool IsPlayerScopeAvailable(int index) =>
        PlayerScope[index].Card == null || !PlayerScope[index].Card.cardObj.DragComponent.IsLocked;

    public void ClearEnemyCards()
    {
        for (var i = 0; i < EnemyScope.Length; i++)
        {
            if (EnemyScope[i].Card != null)
            {
                var card = RemoveCard(i, false);
                Destroy(card.cardObj.gameObject);
            }
        }
    }

    public void DestroyCard(FightCardData card)
    {
        var scope = GetScope(card.isPlayerCard);
        if (card.PosIndex >= 0)
        {
            var pos = scope[card.PosIndex];
            pos.RemoveCard();
        }

        if (card.cardObj != null && card.cardObj.gameObject)
            Destroy(card.cardObj.gameObject);
    }

    public int[] GetNeighborIndexes(int pos) => NeighborCards[pos];
}