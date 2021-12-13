using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VsWarListController : MonoBehaviour
{
    [SerializeField] private ScrollRect listView;
    [SerializeField] private VsWarUi UiPrefab;
    private List<VsWarUi> _vsWarUiList = new List<VsWarUi>();
    private UnityAction<int,int> OnSelectAction { get; set; }

    public void Init(UnityAction<int,int> onSelectAction)
    {
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
        Versus.GetWars(OnRefreshWarList);
#endif

        void OnRefreshWarList(string data)
        {
            var bag = DataBag.DeserializeBag(data);
            var list = bag.Get<List<List<string>>>(0);
            foreach (var obj in list)
            {
                var warId = int.Parse(obj[0]);
                var hostName = obj[1];
                var warIsd = int.Parse(obj[2]);
                var warIdentity = int.Parse(obj[3]);
                var expired = long.Parse(obj[4]);
                GenerateUi(warId, warIsd, hostName, (Versus.SpWarIdentity)warIdentity,expired);
            }
        }
    }

    private void GenerateUi(int warId, int warIsd, string hostName, Versus.SpWarIdentity identity, long expired)
    {
        var ui = Instantiate(UiPrefab, listView.content);

        ui.ChallengerDisplay.gameObject.SetActive(identity == Versus.SpWarIdentity.Challenger);
        ui.HostDisplay.gameObject.SetActive(identity == Versus.SpWarIdentity.Host);
        ui.LoseDisplay.gameObject.SetActive(identity == Versus.SpWarIdentity.PrevHost);
        ui.InfoUi.gameObject.SetActive(identity != Versus.SpWarIdentity.Anonymous);

        ui.ExpiredTime = expired;
        ui.HostName.text = hostName;
        ui.gameObject.SetActive(true);
        ui.Id = warId;
        ui.ClickButton.onClick.RemoveAllListeners();
        ui.ClickButton.onClick.AddListener(() => OnSelectAction?.Invoke(warId, warIsd));
        /*
         * 1.btn click binding
         * 2.ui update
         */
        _vsWarUiList.Add(ui);
    }

    public void Display(bool isShow) => gameObject.SetActive(isShow);

    public void UpdateChallengeTimer()
    {
        var now = SysTime.UnixNow;
        _vsWarUiList.ForEach(u =>
        {
            var milliSecs = u.ExpiredTime - now;
            if (milliSecs >= 0)
            {
                var ts = TimeSpan.FromMilliseconds(milliSecs);
                u.TimeCount.text = $"{(int)ts.TotalMinutes}:{ts.Seconds:00}";
                return;
            }

            u.TimeCount.text = string.Empty;
        });
    }

    private class ChallengeDto : DataBag
    {
        public int WarId { get; set; }
        public int CharacterId { get; set; }
        public string HostName { get; set; }
        public long StartTime { get; set; }
        /// <summary>
        /// key = index, value = checkpoints
        /// </summary>
        public Dictionary<int, int[]> Stages { get; set; }
        /// <summary>
        /// key = index, value = units(ex -17)
        /// </summary>
        public Dictionary<int, int> StageUnit { get; set; }
        public Dictionary<int, bool> StageProgress { get; set; }
    }

}