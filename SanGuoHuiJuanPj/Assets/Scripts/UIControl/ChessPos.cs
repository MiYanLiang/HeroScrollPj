using System.Collections.Generic;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEngine;

public class ChessPos : MonoBehaviour,IChessPos
{
    [SerializeField]private int posIndex = -1;
    
    public ChessTerrain Terrain { get; private set; }
    
    public IChessOperator Operator { get; private set; }

    public int Pos => posIndex;
    public bool IsChallenger { get; private set; }
    public bool IsPostedAlive => Operator != null && Operator.IsAlive;
    public bool IsAliveHero => IsPostedAlive && Operator.CardType == GameCardType.Hero;
    public FightCardData Card { get; private set; }

    public void Init(int pos,bool isChallenger)
    {
        posIndex = pos;
        IsChallenger = isChallenger;
        Terrain = new ChessTerrain();
    }

    void IChessPos.SetPos(IChessOperator op)
    {
        Operator = op;
    }
    void IChessPos.RemoveOperator() => Operator = null;
    public void RemoveCard() => Card = null;
    public void PlaceCard(FightCardData chessman, bool resetPos)
    {
        chessman.UpdatePos(posIndex);
        chessman.cardObj.transform.SetParent(transform);
        Card = chessman;
        if (resetPos)
        {
            Card.cardObj.transform.localPosition = Vector3.zero;
            Card.cardObj.transform.localScale = Vector3.one;
        }
    }
}