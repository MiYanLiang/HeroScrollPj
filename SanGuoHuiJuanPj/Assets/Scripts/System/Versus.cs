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

    public void PlayResult(TestServerSimpleApi.GameResult data, TestStageUi.SimpleFormation enemyFormation)
    {
        const int basePos = 17;
        var enemies = new List<GameCard>();
        var challengers = new List<GameCard>();
        FightCardData enemyBase,playerBase;
        //var eBase = enemyFormation.Formation[basePos];
        //enemyFormation.Formation.Remove(basePos);
        //var eb = DataTable.BaseLevel[eBase.Level];
        //enemyBase = FightCardData.BaseCard(false, eb.BaseHp, eBase.Level);
        //WarBoard.NewGame();
        //WarBoard.SetEnemiesIncludeUis(enemyBase,
        //    enemyFormation.Formation.Select(c => new ChessCard
        //            { Id = c.Value.CardId, Level = c.Value.Level, Type = (GameCardType)c.Value.Type, Pos = c.Key })
        //        .ToList());
        //WarBoard.StartNewGame();
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