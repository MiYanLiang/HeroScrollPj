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

    public IReadOnlyDictionary<int, FightCardData> Data => data;

    private ChessGrid grid;
    private Dictionary<int, FightCardData> data;

    public int PlayerCardsOnBoard => PlayerScope.Count(p => p.Card != null && p.Card.cardType != 522);
    public int EnemyCardsOnBoard => EnemyScope.Count(p => p.Card != null && p.Card.cardType != 522);

    public void Init()
    {
        grid = new ChessGrid(PlayerScope.Cast<IChessPos>().ToArray(), EnemyScope.Cast<IChessPos>().ToArray());
        data = new Dictionary<int, FightCardData>();
        for (var i = 0; i < PlayerScope.Length; i++) PlayerScope[i].Init(i, true);
        for (var i = 0; i < EnemyScope.Length; i++) EnemyScope[i].Init(i, false);
    }

    public void ResetChessboard()
    {
        foreach (var chessPos in PlayerScope.Concat(EnemyScope)) chessPos.ResetPos();
        data.Clear();
    }
    /// <summary>
    /// 棋子控件置高，避免被其它UI挡到
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="isPlayer"></param>
    public void OnActivityBeginTransformSibling(int pos, bool isPlayer)
    {
        var card = GetChessPos(pos, isPlayer).Card;
        var trans = card.cardObj.transform;
        trans.SetParent(transform,true);
        trans.SetAsLastSibling();
    }

    public void ResetPos(FightCardData card)
    {
        //注意这里是获取状态里的位置而不是原始位置。
        PlaceCard(card.Pos , card);
    }

    public ChessPos[] GetScope(bool isPlayer) => isPlayer ? PlayerScope : EnemyScope;
    public ChessPos GetChessPos(int index, bool isPlayer) => GetScope(isPlayer)[index];
    public ChessPos GetChessPos(FightCardData card) => GetScope(card.IsPlayer)[card.Pos];

    public void PlaceCard(int index, FightCardData card)
    {
        if (!data.ContainsKey(card.InstanceId))
            data.Add(card.InstanceId, card);
        var scope = GetScope(card.isPlayerCard);
        var pos = scope.FirstOrDefault(p => p.Card == card);
        if (pos != null) RemoveCard(pos.Pos, card.IsPlayer);//移除卡牌前位置
        if (scope[index].Card != null) RemoveCard(index, card.isPlayerCard);//移除目标位置上的卡牌
        scope[index].PlaceCard(card, true);
        card.UpdatePos(index);
    }

    public FightCardData RemoveCard(int index, bool isPlayer)
    {
        var scope = GetScope(isPlayer);
        if (scope[index].Card == null)
            throw XDebug.Throw<Chessboard>($"位置[{index}] 没有卡牌！");
        var card = scope[index].Card;
        scope[index].RemoveCard();
        card.UpdatePos(-1);
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
