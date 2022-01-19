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
        RequestApiButton.onClick.AddListener(RequestTestServerList);
        StartCoroutine(StartInit());
    }

    private IEnumerator StartInit()
    {
        StartButton = chessboard.StartButton;
        StartButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(1f);
        Versus.Init(null);
        Versus.StartNewGame();
    }

    private async void RequestTestServerList()
    {
        var respond = await Http.GetAsync(ServerGetListApi);
        var json = await respond.Content.ReadAsStringAsync();
        var list = Json.DeserializeList<TestStageUi.SimpleFormation>(json);
        UpdateTestStateList(list);
    }

    private void UpdateTestStateList(List<TestStageUi.SimpleFormation> list)
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
        var result = Json.Deserialize<Versus.WarResult>(json);
        Versus.PlayResult(result);
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

}