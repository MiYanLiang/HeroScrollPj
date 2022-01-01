using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VsWarListController : MonoBehaviour
{
    [SerializeField] private ScrollRect listView;
    [SerializeField] private VsWarUi UiPrefab;
    private List<VsWarUi> _vsWarUiList = new List<VsWarUi>();
    private Versus Vs { get; set; }
    private UnityAction<int,int> OnSelectAction { get; set; }
    public void Init(Versus vs,UnityAction<int,int> onSelectAction)
    {
        Vs = vs;
        gameObject.SetActive(true);
        OnSelectAction = onSelectAction;
        UiPrefab.gameObject.SetActive(false);
        GetWarList();
    }

    public void GetWarList()
    {
        foreach (var ui in _vsWarUiList) Destroy(ui.gameObject);
        _vsWarUiList.Clear();
#if UNITY_EDITOR
        Versus.GetRkWars(OnRefreshWarList);
#endif

        void OnRefreshWarList(string data)
        {
            var bag = DataBag.DeserializeBag(data);
            if (bag == null)
            {
                Versus.ShowHints(data);
                return;
            }

            var wars = bag.Get<List<RkWarDto>>(0);
            var challenges = bag.Get<Dictionary<int, Versus.ChallengeDto>>(1);
            foreach (var war in wars)
            {
                challenges.TryGetValue(war.WarId, out var challenge);
                GenerateUi(war, war.RankingBoard.FindIndex(r => r.CharId == Versus.CharId), challenge);
            }
        }
    }

    private void GenerateUi(RkWarDto war, int index, Versus.ChallengeDto challengeDto)
    {
        var ui = Instantiate(UiPrefab, listView.content);
        ui.Init(war.WarId);
        ui.SetChallengeUi(challengeDto);
        var isChallenger = challengeDto != null;
        ui.SetBoard(index, war.RankingBoard, isChallenger, OnSelectAction);
        ui.ChallengeUi.Button.gameObject.SetActive(isChallenger);
        if (isChallenger)
        {
            Vs.RegChallengeUi(challengeDto, ui.ChallengeUi.TimerUi);
            ui.ChallengeUi.Button.onClick.RemoveAllListeners();
            ui.ChallengeUi.Button.onClick.AddListener(() => OnSelectAction.Invoke(war.WarId, challengeDto.HostId));
        }
        _vsWarUiList.Add(ui);
    }

    public void Display(bool isShow) => gameObject.SetActive(isShow);

    public class RkWarDto
    {
        public int WarId { get; set; }
        public List<Rank> RankingBoard { get; set; }

        public RkWarDto() { }
        public RkWarDto(int warId, IEnumerable<Rank> rankingBoard)
        {
            WarId = warId;
            RankingBoard = rankingBoard.ToList();
        }

        public class Rank
        {
            public int CharId { get; set; }
            public string CharName { get; set; }
            public int MPower { get; set; }

            public Rank()
            {

            }
            public Rank(int charId, string charName, int mPower)
            {
                CharId = charId;
                CharName = charName;
                MPower = mPower;
            }
        }
    }

}