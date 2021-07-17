using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Chessboard : MonoBehaviour
{
    [SerializeField]private ChessPos[] PlayerScope;
    [SerializeField]private ChessPos[] EnemyScope;

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
}