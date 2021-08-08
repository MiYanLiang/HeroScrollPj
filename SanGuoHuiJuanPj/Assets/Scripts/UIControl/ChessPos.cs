using Assets.System.WarModule;
using UnityEngine;

public class ChessPos : MonoBehaviour,IChessPos<FightCardData>
{
    [SerializeField]private int posIndex = -1;
    
    public FightCardData Chessman => Card;
    public ChessTerrain Terrain { get; private set; }

    public int Pos => posIndex;
    public FightCardData Card { get; private set; }
    
    public int PosIndex
    {
        get
        {
            if (posIndex < 0) throw XDebug.Throw<ChessPos>("棋盘未初始化!");
            return posIndex;
        }
    }


    public void Init(int pos)
    {
        posIndex = pos;
        Terrain = new ChessTerrain();
    }

    public void SetCard(FightCardData card)
    {
        Card = card;
        card.cardObj.transform.SetParent(transform);
        card.cardObj.transform.localPosition = Vector3.zero;
        card.cardObj.transform.localScale = Vector3.one;
    }

    public void RemoveCard() => Card = null;

    public void SetPos(FightCardData chessman)
    {
        SetCard(chessman);
        chessman.UpdatePos(posIndex);
    }

    public void RemovePos() => posIndex = -1;
}