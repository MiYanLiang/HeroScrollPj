using System;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Chessboard : MonoBehaviour
{
    [SerializeField] private ChessPos[] PlayerScope;
    [SerializeField] private ChessPos[] EnemyScope;
    
    private ChessGrid grid;

    public int PlayerCardsOnBoard => PlayerScope.Count(p => p.Card != null && p.Card.cardType != 522);
    public int EnemyCardsOnBoard => EnemyScope.Count(p => p.Card != null && p.Card.cardType != 522);

    public void Init()
    {
        grid = new ChessGrid(PlayerScope.Cast<IChessPos>().ToArray(), EnemyScope.Cast<IChessPos>().ToArray());
        for (var i = 0; i < PlayerScope.Length; i++) PlayerScope[i].Init(i, true);
        for (var i = 0; i < EnemyScope.Length; i++) EnemyScope[i].Init(i, false);
    }

    public FightCardData OnActivityBegin(int pos, bool isPlayer)
    {
        var card = GetChessPos(pos, isPlayer).Card;
        var trans = card.cardObj.transform;
        trans.SetParent(transform,true);
        trans.SetAsLastSibling();
        return card;
    }

    public void ResetPos(IChessOperator op,FightCardData card)
    {
        //注意这里是获取状态里的位置而不是原始位置。
        PlaceCard(op.Pos, card);
    }

    public IEnumerable<ChessPos> GetData() => PlayerScope.Concat(EnemyScope);
    public ChessPos[] GetScope(bool isPlayer) => isPlayer ? PlayerScope : EnemyScope;
    public ChessPos GetChessPos(int index, bool isPlayer) => GetScope(isPlayer)[index];
    public ChessPos GetChessPos(FightCardData card) => GetScope(card.IsPlayer)[card.Pos];

    public void PlaceCard(int index, FightCardData card)
    {
        var scope = GetScope(card.isPlayerCard);
        var pos = scope.FirstOrDefault(p => p.Card == card);
        if (pos != null) RemoveCard(pos.Pos, card.IsPlayer);//移除卡牌前位置
        if (scope[index].Card != null) RemoveCard(index, card.isPlayerCard);//移除目标位置上的卡牌
        scope[index].PlaceCard(card, true);
        card.UpdatePos(index);
        if (card.isPlayerCard) card.cardObj.DragComponent?.ResetPos();
    }

    public FightCardData RemoveCard(int index, bool isPlayer)
    {
        var scope = GetScope(isPlayer);
        if (scope[index].Card == null)
            throw XDebug.Throw<Chessboard>($"位置[{index}] 没有卡牌！");
        var card = scope[index].Card;
        scope[index].RemoveCard();
        card.UpdatePos(-1);
        if (isPlayer && card.cardObj.DragComponent!=null) 
            card.cardObj.DragComponent.ResetPos();
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

    public int[] GetNeighborIndexes(int pos) => grid.GetNeighborIndexes(pos);

}
