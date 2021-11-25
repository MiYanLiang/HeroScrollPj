using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class TestServerSimpleApi : MonoBehaviour
{
    [SerializeField] private string ServerGetListApi;
    [SerializeField] private string ServerChallengeApi;
    [SerializeField] private Button RequestApiButton;
    [SerializeField] private ScrollRect ScrollView;
    [SerializeField] private TestStageUi Prefab;
    private List<TestStageUi> Stages = new List<TestStageUi>();
    public Versus Versus;
    public WarBoardUi WarBoard;
    [SerializeField]private Chessboard chessboard;
    private Button StartButton { get; set; }
    private TestStageUi.SimpleFormation selectedFormation { get; set; }
    public void InitTest()
    {
        RequestApiButton.onClick.AddListener(RequestServerList);
        StartCoroutine(StartInit());
    }

    private IEnumerator StartInit()
    {
        StartButton = chessboard.StartButton;
        StartButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(1f);
        Versus.Init();
        Versus.StartNewGame();
        InitCardToRack();
    }

    //初始化卡牌列表
    private void InitCardToRack()
    {
        var forceId = PlayerDataForGame.instance.CurrentWarForceId;
#if UNITY_EDITOR
        if (forceId == -2) //-2为测试用不重置卡牌，直接沿用卡牌上的阵容
        {
            PlayerDataForGame.instance.fightHeroId.Select(id => new GameCard().Instance(GameCardType.Hero, id, 1))
                .Concat(PlayerDataForGame.instance.fightTowerId.Select(id =>
                    new GameCard().Instance(GameCardType.Tower, id, 1)))
                .Concat(PlayerDataForGame.instance.fightTrapId.Select(id =>
                    new GameCard().Instance(GameCardType.Trap, id, 1)))
                .ToList().ForEach(WarBoard.CreateCardToRack);
            return;
        }
#endif
        PlayerDataForGame.instance.fightHeroId.Clear();
        PlayerDataForGame.instance.fightTowerId.Clear();
        PlayerDataForGame.instance.fightTrapId.Clear();

        var hstData = PlayerDataForGame.instance.hstData;
        //临时记录武将存档信息
        hstData.heroSaveData.Enlist(forceId).ToList()
            .ForEach(WarBoard.CreateCardToRack);
        hstData.towerSaveData.Enlist(forceId).ToList()
            .ForEach(WarBoard.CreateCardToRack);
        hstData.trapSaveData.Enlist(forceId).ToList()
            .ForEach(WarBoard.CreateCardToRack);
    }

    private async void RequestServerList()
    {
        var respond = await Http.GetAsync(ServerGetListApi);
        var json = await respond.Content.ReadAsStringAsync();
        var list = Json.DeserializeList<TestStageUi.SimpleFormation>(json);
        UpdateList(list);
    }

    private void UpdateList(List<TestStageUi.SimpleFormation> list)
    {
        if(Stages.Any())
            foreach (var stage in Stages)
                Destroy(stage.gameObject);
        Stages.Clear();
        foreach (var formation in list)
        {
            var ui = Instantiate(Prefab, ScrollView.content);
            ui.Set(formation);
            Stages.Add(ui);
            ui.Button.onClick.RemoveAllListeners();
            ui.Button.onClick.AddListener(()=>OnSetSelectedFormation(formation));
        }
    }

    private void OnSetSelectedFormation(TestStageUi.SimpleFormation formation)
    {
        StartButton.gameObject.SetActive(false);
        selectedFormation = formation;
        StartButton.onClick.RemoveAllListeners();
        StartButton.onClick.AddListener(OnStartBattle);
        StartButton.gameObject.SetActive(true);
    }

    private bool isBusy = false;

    private void OnStartBattle()
    {
        if (isBusy) return;
        isBusy = true;
        Versus.SetEnemyFormation(selectedFormation.Formation.ToDictionary(c => c.Key, c => c.Value as IGameCard));
        StartButton.GetComponent<Animator>().SetBool(WarBoardUi.ButtonTrigger, false);
        StartButton.onClick.RemoveAllListeners();
        var f = selectedFormation;
        var challengerFormation = WarBoard.PlayerScope.ToDictionary(c => c.Pos, c => new ChallengeSet.Card(c.Card));
        challengerFormation.Add(17, new ChallengeSet.Card(GameCard.Instance(0, (int)GameCardType.Base, Versus.CityLevel)));
        var set = new ChallengeSet
        {
            CharacterId = 123,
            TargetStage = f.Id,
            Formation = challengerFormation
        };
        CallChallengeApi(set);
    }

    private async void CallChallengeApi(ChallengeSet challengeSet)
    {
        var response = await Http.PostAsync(ServerChallengeApi, Json.Serialize(challengeSet));
        isBusy = false;
        if (!response.IsSuccessStatusCode)
            return;
        var json = await response.Content.ReadAsStringAsync();
        var data = Json.Deserialize<GameResult>(json);
        Versus.PlayResult(data);
    }

    private class ChallengeSet
    {
        public Dictionary<int, Card> Formation { get; set; }
        public int CharacterId { get; set; }
        public int TargetStage { get; set; }

        public class Card : IGameCard
        {
            public int CardId { get; set; }
            public int Level { get; set; }
            public int Chips { get; set; }
            public int Type { get; set; }

            public Card()
            {

            }
            public Card(IGameCard c)
            {
                CardId = c.CardId;
                Level = c.Level;
                Chips = c.Chips;
                Type = c.Type;
            }
        }
    }
    public class GameResult
    {
        public List<Operator> Chessmen { get; set; }
        public List<ChessRound> Rounds { get; set; }
        public bool IsChallengerWin { get; set; }

        public class Operator:IOperatorInfo
        {
            public int InstanceId { get; set; }
            public int Pos { get; set; }
            public bool IsChallenger { get; set; }
            public Card Card { get; set; }
            IGameCard IOperatorInfo.Card => Card;
        }
        public class Card :IGameCard
        {
            public int CardId { get; set; }
            public int Level { get; set; }
            public int Chips { get; set; }
            public int Type { get; set; }
        }
    }

}