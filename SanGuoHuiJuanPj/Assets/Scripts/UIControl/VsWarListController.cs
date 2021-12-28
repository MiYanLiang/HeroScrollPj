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
    private UnityAction<int> OnSelectAction { get; set; }
    public void Init(UnityAction<int> onSelectAction)
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

            var list = bag.Get<Dictionary<int, Dictionary<int, object>>>(0);
            foreach (var obj in list)
            {
                var warId = obj.Key;
                var rank = obj.Value;
                GenerateUi(warId, rank);
            }
        }
    }

    private void GenerateUi(int warId, Dictionary<int, object> rank)
    {
        var ui = Instantiate(UiPrefab, listView.content);

        //ui.ChallengerDisplay.gameObject.SetActive(identity == Versus.SpWarIdentity.Challenger);
        //ui.HostDisplay.gameObject.SetActive(identity == Versus.SpWarIdentity.Host);
        //ui.LoseDisplay.gameObject.SetActive(identity == Versus.SpWarIdentity.PrevHost);
        //ui.InfoUi.gameObject.SetActive(identity != Versus.SpWarIdentity.Anonymous);
        //ui.PrevChallenger.gameObject.SetActive(identity == Versus.SpWarIdentity.PrevChallenger);
        //if (rank.Count > 0)
        //{
        //    ui.HostName.text = rank[0].ToString();
        //    ui.MilitaryPower.text = rank[1].ToString();
        //}
        //
        //ui.gameObject.SetActive(true);
        //ui.Id = warId;
        //ui.ClickButton.onClick.RemoveAllListeners();
        //ui.ClickButton.onClick.AddListener(() => OnSelectAction?.Invoke(warId));
        /*
         * 1.btn click binding
         * 2.ui update
         */
        
        _vsWarUiList.Add(ui);
    }

    public void Display(bool isShow) => gameObject.SetActive(isShow);

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