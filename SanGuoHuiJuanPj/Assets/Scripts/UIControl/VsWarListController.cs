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
        Http.Get($"{Versus.Api}/{Versus.GetWarsV1}" , OnRefreshWarList, Versus.GetWarsV1);

        void OnRefreshWarList(string data)
        {
            var bag = DataBag.DeserializeBag(data);
            var list = bag.Get<List<List<string>>>(0);
            foreach (var obj in list)
            {
                var warId = int.Parse(obj[0]);
                var hostName = obj[1];
                GenerateUi(warId, hostName);
            }
        }
    }

    public void GenerateUi(int warId, string hostName)
    {
        var ui = Instantiate(UiPrefab,listView.content);
        ui.HostName.text = hostName;
        ui.gameObject.SetActive(true);
        ui.InfoUi.gameObject.SetActive(false);
        ui.Id = warId;
        ui.ClickButton.onClick.RemoveAllListeners();
        ui.ClickButton.onClick.AddListener(() => OnSelectAction?.Invoke(warId));
        /*
         * 1.btn click binding
         * 2.ui update
         */
        _vsWarUiList.Add(ui);
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

    public void Display(bool isShow)
    {
        gameObject.SetActive(isShow);
    }
}