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
    private List<RankingUi> List { get; set; }

    public void Init(int warId)
    {
        RankingBoardUiPrefab.gameObject.SetActive(false);
        List = new List<RankingUi>();
        WarId = warId;
        gameObject.SetActive(true);
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
            var i = obj.Key;
            var rank = obj.Value;
            var ui = Instantiate(RankingBoardUiPrefab, BoardScrollRect.content);
            if (!isChallenger)
            {

                ui.Set(i + 1, rank.CharName, rank.MPower,
                    i == index ? default(UnityAction) : () => onClickAction(WarId, rank.WarIsd));
            }
            else
                ui.Set(i + 1, rank.CharName, rank.MPower, null);
            List.Add(ui);
        }
        var rankText = index >= 0 ? (index + 1).ToString() : "~";
        SetRank(rankText);
    }

    private void Show(IUiObj uiObj,bool show) => uiObj.Obj.SetActive(show);

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