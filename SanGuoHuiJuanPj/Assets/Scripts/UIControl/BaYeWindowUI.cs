using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaYeWindowUI : MiniWindowUI
{
    private enum DisplayViews
    {
        Reward,
        Quest,
        Selection
    }
    public Transform questionView;
    public Transform selectionView;
    public Transform selectionContent;
    public BaYeLingSelectBtn selectionButtonPrefab;
    public Text question;
    public Button[] answer;
    public AdConsumeController adConsume;
    //public Button adBtn;
    public Button closeBtn;
    private List<BaYeLingSelectBtn> lingCaches;

    public override void Init()
    {
        base.Init();
        foreach (var btn in selectionView.GetComponentsInChildren<BaYeLingSelectBtn>(true))
            btn.gameObject.SetActive(false);
        lingCaches = new List<BaYeLingSelectBtn>();
        adConsume.Init();
    }

    public override void Show(Dictionary<int, int> rewardMap, Action<MiniWindowElementUI> extraSetAction = null)
    {
        base.Show(rewardMap, extraSetAction);
        DisplayViewChange(DisplayViews.Reward);
    }

    public void ShowQuest(QuestTable quest, Action onCorrectAction, Action onFaultyAction)
    {
        question.text = quest.Question;
        var answers = new[] {(1,quest.A), (2,quest.B), (3,quest.C)};
        for (int i = 0; i < answer.Length; i++)
        {
            var btn = answer[i];
            var asw = answers[i];
            btn.GetComponentInChildren<Text>().text = asw.Item2;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(Close);
            if (quest.Answer == asw.Item1)
                btn.onClick.AddListener(onCorrectAction.Invoke);
            else btn.onClick.AddListener(onFaultyAction.Invoke);
        }

        DisplayViewChange(DisplayViews.Quest);
    }

    private void DisplayViewChange(DisplayViews display)
    {
        if(display == DisplayViews.Reward)
            adConsume.ShowWithUpdate();
        else adConsume.Off();
        //adBtn.gameObject.SetActive(display==DisplayViews.Reward);
        listView.gameObject.SetActive(display == DisplayViews.Reward);
        questionView.gameObject.SetActive(display == DisplayViews.Quest);
        selectionView.gameObject.SetActive(display == DisplayViews.Selection);
        gameObject.SetActive(true);
    }

    public void ShowSelection(Dictionary<int, int> rewardMap, UnityAction<int> recallAction)
    {
        closeBtn.gameObject.SetActive(false);
        foreach (var obj in rewardMap)
        {
            var orderId = obj.Key;
            var amt = obj.Value;
            var ling = Instantiate(selectionButtonPrefab, selectionContent);
            ling.Set(orderId, amt);
            ling.gameObject.SetActive(true);
            ling.btn.onClick.AddListener(() =>
            {
                recallAction(orderId);
                OnPlayerSelected();
            });
            lingCaches.Add(ling);
        }
        DisplayViewChange(DisplayViews.Selection);
    }

    private void OnPlayerSelected()
    {
        if (lingCaches == null || lingCaches.Count == 0) return;
        foreach (var btn in lingCaches) 
            Destroy(btn.gameObject);
        lingCaches.Clear();
        Close();
    }

    public override void Close()
    {
        closeBtn.gameObject.SetActive(true);
        selectionView.gameObject.SetActive(false);
        questionView.gameObject.SetActive(false);
        adConsume.Off();
        base.Close();
    }

    public void ShowAdButton(UnityAction<bool> onClickAction)
    {
        adConsume.SetCallBackAction(onClickAction.Invoke, _ => onClickAction.Invoke(true),
            true, 0);
        adConsume.ShowWithUpdate();
    }
}