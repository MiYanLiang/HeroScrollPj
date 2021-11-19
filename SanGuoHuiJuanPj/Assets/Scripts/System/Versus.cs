using System.Collections.Generic;
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

    public void PlayResult(TestServerSimpleApi.GameResult data, TestStageUi.SimpleFormation enemyFormation)
    {
        var enemies = new List<GameCard>();
        var challengers = new List<GameCard>();
        FightCardData enemyBase,playerBase;
        //WarBoard.StartNewGame();todo 
        WarBoard.PlayResult(data.Rounds);

        foreach (var (pos, id, isChallenger) in data.Chessmen)
        {
            if (pos == 17)
            {
                if (!isChallenger)
                {
                    enemyBase = FightCardData.BaseCard(false,
                        DataTable.BaseLevel[enemyFormation.Formation[17].Level].BaseHp,
                        enemyFormation.Formation[17].Level);
                    
                }
                else
                    playerBase = FightCardData.PlayerBaseCard(CityLevel);
                continue;
            }
        }
    }

    public void StartNewGame()
    {
        WarBoard.NewGame();
        var card = new FightCardData(GameCard.Instance(0, (int)GameCardType.Base, CityLevel));
        card.SetPos(17);
        WarBoard.SetPlayerBase(card);
        WarBoard.GeneratePlayerScopeChessman();
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

        WarBoard.SetEnemies(enemyBase, list);

        ChessCard InstanceCard(IGameCard c, int p) => ChessCard.Instance(c.CardId, c.Type, c.Level, p);
    }
}