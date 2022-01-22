using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VsWarUi : MonoBehaviour
{
    public int WarId { get; set; }
    public WarInfoUi WarInfo;
    public ChallengeInfoUi ChallengeUi;
    public RankingUi RankingBoardUiPrefab;
    public ScrollRect BoardScrollRect;
    [SerializeField] private GameObject[] WarBgs;
    [SerializeField] private Sprite[] Flags;
    private List<RankingUi> List { get; set; }
    private static Dictionary<int, string> FlagSet { get; } = new Dictionary<int, string>
    {
        { 0, "刘" },
        { 1, "曹" },
        { 2, "孙" },
        { 3, "袁" },
        { 4, "吕" }
    };

    public void Init(int warId, Sprite[] warTitles)
    {
        RankingBoardUiPrefab.gameObject.SetActive(false);
        List = new List<RankingUi>();
        WarId = warId;
        gameObject.SetActive(true);
        for (int i = 0; i < WarBgs.Length; i++)
        {
            WarBgs[i].SetActive(i == warId);
            if (i == warId) WarInfo.TextImage.sprite = warTitles[i];
        }
    }

    public void SetChallengeUi(Versus.ChallengeDto challenge)
    {
        var isChallenger = challenge != null;
        ChallengeUi.TimerUi.gameObject.SetActive(isChallenger);
        ChallengeUi.ChallengeImage.gameObject.SetActive(isChallenger);
        ChallengeUi.Rank.gameObject.SetActive(!isChallenger);
    }

    private void SetRank(string value) => ChallengeUi.Rank.text = value;

    public void SetBoard(int index, Dictionary<int,Versus.RkWarDto.Rank> rankSet, bool isChallenger,
        UnityAction<int,int> onClickAction)
    {
        foreach (var rankingUi in List) Destroy(rankingUi.gameObject);
        foreach (var obj in rankSet.OrderBy(r=>r.Key))
        {
            var rIndex = obj.Key;
            var rank = obj.Value;
            var ui = Instantiate(RankingBoardUiPrefab, BoardScrollRect.content);
            var flagId = rank.TroopId;
            if (!FlagSet.TryGetValue(flagId, out var flagTitle))
                flagTitle = string.IsNullOrWhiteSpace(rank.CharName) ? string.Empty : rank.CharName.First().ToString();
            Sprite fImg;
            if (flagId >= 0 && flagId < Flags.Length)
                fImg = Flags[flagId];
            else fImg = Flags.Last();

            if (!isChallenger)
            {
                ui.Set(rIndex + 1, rank.CharName, rank.MPower, fImg, flagTitle, rIndex == index,
                    rIndex == index ? default(UnityAction) : () => onClickAction(WarId, rank.WarIsd));
            }
            else
                ui.Set(rIndex + 1, rank.CharName, rank.MPower, fImg, flagTitle, rIndex == index, null);
            List.Add(ui);
        }
        var rankText = index >= 0 ? (index + 1).ToString() : "未上榜。";
        SetRank(rankText);
    }

    private void Show(IUiObj uiObj, bool show) => uiObj.Obj.SetActive(show);

    public interface IUiObj
    {
        GameObject Obj { get; }
    }
    [Serializable] public class ChallengeInfoUi : IUiObj
    {
        GameObject IUiObj.Obj => Obj;
        public GameObject Obj;
        public Text Rank;
        public Image ChallengeImage;
        public Text TimerUi;
        public Button Button;
    }
    [Serializable] public class WarInfoUi : IUiObj
    {
        GameObject IUiObj.Obj => Obj;
        public GameObject Obj;
        public Image TextImage;
    }
}