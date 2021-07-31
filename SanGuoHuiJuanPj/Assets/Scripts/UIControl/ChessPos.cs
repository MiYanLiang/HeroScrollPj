using UnityEngine;

public class ChessPos : MonoBehaviour,IChessPos<FightCardData>
{
    [SerializeField]private int posIndex = -1;
    private Chessboard board;
    public FightCardData Chessman => Card;
    public FightCardData Card { get; private set; }
    public int PosIndex
    {
        get
        {
            if (posIndex < 0) throw XDebug.Throw<ChessPos>("棋盘未初始化!");
            return posIndex;
        }
    }


    public void Init(Chessboard chessboard, int i)
    {
        board = chessboard;
        posIndex = i;
    }

    public void SetCard(FightCardData card)
    {
        Card = card;
        card.cardObj.transform.SetParent(transform);
        card.cardObj.transform.localPosition = Vector3.zero;
        card.cardObj.transform.localScale = Vector3.one;
    }

    public void RemoveCard() => Card = null;

    public void SetPos(FightCardData chessman) => SetCard(chessman);
    
    public void RemovePos() => posIndex = -1;
}