using System;
using System.Collections.Generic;
using System.Linq;
using Assets;
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
        _range = new Dictionary<RkRange, int>
        {
            { new RkRange(1, 1), 0 },
            { new RkRange(2, 3), 1 },
            { new RkRange(4, 10), 2 },
            { new RkRange(11, 30), 3 },
            { new RkRange(31, 100), 4 },
            { new RkRange(101, 9999), 5 }
        };

        Vs = vs;
        gameObject.SetActive(true);
        OnSelectAction = onSelectAction;
        UiPrefab.gameObject.SetActive(false);
        GetWarList();
    }

    public void GetWarList(UnityAction onSuccessAction = null)
    {
        if (PlayerDataForGame.instance.Character == null ||
            !PlayerDataForGame.instance.Character.IsValidCharacter()) return;//需要有角色才可以，否则会报错。
        ApiPanel.instance.InvokeRk(OnRefreshWarList, PlayerDataForGame.instance.ShowStringTips, Versus.GetWarsV1);
#if UNITY_EDITOR
        //Versus.GetRkWars(OnRefreshWarList);
#endif
        void OnRefreshWarList(DataBag bag)
        {
            foreach (var ui in _vsWarUiList) Destroy(ui.gameObject);
            _vsWarUiList.Clear();
            var timer = bag.Get<Versus.RkTimerDto>(0);
            Vs.SetState(timer);
            var wars = bag.Get<List<Versus.RkWarDto>>(1);
            var challenges = bag.Get<Dictionary<int, Versus.ChallengeDto>>(2);
            var rewardSet = bag.Get<Dictionary<int, int>>(3);
            foreach (var war in wars)
            {
                challenges.TryGetValue(war.WarId, out var challenge);
                GenerateUi(war, challenge, rewardSet.TryGetValue(war.WarId, out var rank) ? rank : -1);
            }
            onSuccessAction?.Invoke();
        }
    }

    private void GenerateUi(Versus.RkWarDto war, Versus.ChallengeDto challengeDto, int rank)
    {
        var ui = Instantiate(UiPrefab, listView.content);
        ui.Init(war.WarId, Vs.WarTitles);
        ui.SetChallengeUi(challengeDto);
        var isChallenger = challengeDto != null;
        ui.SetBoard(war.Index, war.RankingBoard, isChallenger, OnSelectAction);
        ui.ChallengeUi.Button.gameObject.SetActive(isChallenger);
        if (rank >= 0)
        {
            var chessId = GetRewardChestValue(rank);
            ui.SetChest(chessId, OnRequestRkReward);
        }

        if (isChallenger)
        {
            Vs.RegChallengeUi(challengeDto, ui.ChallengeUi.TimerUi);
            ui.ChallengeUi.Button.onClick.RemoveAllListeners();
            ui.ChallengeUi.Button.onClick.AddListener(() =>
            {
                Vs.PlayClickAudio();
                OnSelectAction.Invoke(war.WarId, challengeDto.WarIsd);
            });
        }

        _vsWarUiList.Add(ui);

        void OnRequestRkReward(UnityAction callBack)
        {
            ApiPanel.instance.InvokeBag(OnSuccessGetReward, PlayerDataForGame.instance.ShowStringTips,
                EventStrings.Rk_GetReward, string.Empty, war.WarId);

            void OnSuccessGetReward(DataBag bag)
            {
                var py = bag.Get<PlayerDataDto>(0);
                var r = bag.Get<RewardDto>(1);
                UIManager.instance.ShowRewardsThings(new DeskReward(
                    r.YuanBao, r.YuQue, 0, r.Stamina, r.AdPass,
                    r.RewardCards.Select(c => new CardReward
                    {
                        cardId = c.CardId,
                        cardChips = c.Chips,
                        cardType = c.Type
                    }).ToList()), 1.5f);
                ConsumeManager.instance.SaveChangeUpdatePlayerData(py);
                callBack?.Invoke();
            }
        }
    }
    private class RewardDto
    {
        public int YuanBao { get; set; }
        public int YuQue { get; set; }
        public int Stamina { get; set; }
        public int AdPass { get; set; }
        public Card[] RewardCards { get; set; }

        public class Card : IGameCard
        {
            public int CardId { get; set; }
            public int Level { get; set; }
            public int Chips { get; set; }
            public int Type { get; set; }
        }
    }


    #region PageActivities
    public void Display(bool isShow) => gameObject.SetActive(isShow);
    void OnEnable() => SignalRClient.instance.SubscribeAction(EventStrings.Chn_RkListUpdate, CallRefreshWarList);
    void OnDisable() => SignalRClient.instance.UnSubscribeAction(EventStrings.Chn_RkListUpdate, CallRefreshWarList);
    private void CallRefreshWarList(string arg0) => GetWarList();
    #endregion

    #region RewardRange

    private Dictionary<RkRange, int> _range = new Dictionary<RkRange, int>();

    private int GetRewardChestValue(int rank)
    {
        var result = -1;
        var element = _range.FirstOrDefault(r => r.Key.InRange(rank));
        if (element.Key != null) result = element.Value;
        return result;
    }

    private class RkRange
    {
        public int Min { get; }
        public int Max { get; }
        public bool InRange(int value) => value >= Min && value <= Max;

        public RkRange(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    #endregion

}