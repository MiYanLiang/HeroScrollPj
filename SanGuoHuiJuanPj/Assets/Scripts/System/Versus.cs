using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

//对决页面
public class Versus : MonoBehaviour
{
    [SerializeField] private WarBoardUi WarBoard;
    [SerializeField] private ChessboardVisualizeManager ChessboardManager;
    [SerializeField] private Image Infoboard;
    [SerializeField] private int MaxCards;
    public int CityLevel = 1;

    public void Init()
    {
        if (!EffectsPoolingControl.instance.IsInit)
            EffectsPoolingControl.instance.Init();
        WarBoard.Init();
        WarBoard.MaxCards = MaxCards;
        ChessboardManager.Init(WarBoard.Chessboard, WarBoard.JiBanManager);
    }

    public void PlayResult(TestServerSimpleApi.GameResult data)
    {
        const int basePos = 17;
        
        WarBoard.NewGame();
        foreach (var op in data.Chessmen)
        {
            var card = new FightCardData(GameCard.Instance(op.Card.CardId, op.Card.Type, op.Card.Level));
            card.SetPos(op.Pos);
            card.SetInstanceId(op.InstanceId);
            card.isPlayerCard = op.IsChallenger;
            ChessboardManager.InstanceChessman(card);
        }

        WarBoard.PlayResult(data.Rounds);
    }

    public void StartNewGame()
    {
        WarBoard.NewGame();
        var card = new FightCardData(GameCard.Instance(0, (int)GameCardType.Base, CityLevel));
        card.SetPos(17);
        WarBoard.SetPlayerBase(card);
        WarBoard.Chessboard.UpdateWarSpeed();
    }

    public void SetEnemyFormation(Dictionary<int, IGameCard> formation)
    {
        var b = formation[17];
        var enemyBase = new FightCardData(GameCard.Instance(b.CardId, b.Type, b.Level));
        enemyBase.SetPos(17);
        var list = new List<ChessCard>();
        foreach (var o in formation)
        {
            var pos = o.Key;
            if (pos == 17) continue;
            var card = o.Value;
            list.Add(InstanceCard(card, pos));
        }

        WarBoard.SetEnemiesIncludeUis(enemyBase, list);

        ChessCard InstanceCard(IGameCard c, int p) => ChessCard.Instance(c.CardId, c.Type, c.Level, p);
    }
}