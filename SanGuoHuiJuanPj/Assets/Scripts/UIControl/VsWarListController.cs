﻿using System;
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

            var wars = bag.Get<List<Versus.RkWarDto>>(0);
            var challenges = bag.Get<Dictionary<int, Versus.ChallengeDto>>(1);
            foreach (var war in wars)
            {
                challenges.TryGetValue(war.WarId, out var challenge);
                GenerateUi(war, challenge);
            }
        }
    }

    private void GenerateUi(Versus.RkWarDto war, Versus.ChallengeDto challengeDto)
    {
        var ui = Instantiate(UiPrefab, listView.content);
        ui.Init(war.WarId);
        ui.SetChallengeUi(challengeDto);
        var isChallenger = challengeDto != null;
        ui.SetBoard(war.Index, war.RankingBoard, isChallenger, OnSelectAction);
        ui.ChallengeUi.Button.gameObject.SetActive(isChallenger);
        if (isChallenger)
        {
            Vs.RegChallengeUi(challengeDto, ui.ChallengeUi.TimerUi);
            ui.ChallengeUi.Button.onClick.RemoveAllListeners();
            ui.ChallengeUi.Button.onClick.AddListener(() =>
                OnSelectAction.Invoke(war.WarId, challengeDto.WarIsd));
        }
        _vsWarUiList.Add(ui);
    }

    public void Display(bool isShow) => gameObject.SetActive(isShow);

}