using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaYeTradeLingWindowUi : MonoBehaviour
{
    [SerializeField] private ZhanLingSectUi[] Zhanlings;
    [SerializeField] private Button CloseButton;
    private event UnityAction<int,int> OnAddZhanLing;
    private int ZhanLingPrice => basePrice * multiply;
    private int basePrice;
    private int multiply;
    private bool IsConsumedAd { get; set; }
    private int[] ForceIds { get; set; }

    public void Init(int basePrice,UnityAction<int,int> onAddZhanLing)
    {
        this.basePrice = basePrice;
        OnAddZhanLing += onAddZhanLing;
        foreach (var ui in Zhanlings) ui.Init();
        Display(false);
    }

    public void SetTradeZhanLing(int[] forceIds, bool reset = true)
    {
        ForceIds = forceIds;
        if (reset)
        {
            multiply = 1;
            IsConsumedAd = false;
        }

        for (var i = 0; i < Zhanlings.Length; i++)
        {
            var ui = Zhanlings[i];
            if (IsConsumedAd)
                ui.Set(forceIds[i], ZhanLingPrice, OnBuyAction, OnConsumeAd);
            else
                ui.Set(forceIds[i], ZhanLingPrice, OnBuyAction, null);
        }
        Display(true);
    }

    private void OnBuyAction(int forceId)
    {
        IncreasePrice(forceId);
    }

    private void OnConsumeAd(int forceId)
    {
        IsConsumedAd = true;
        IncreasePrice(forceId);
    }

    private void IncreasePrice(int forceId)
    {
        multiply++;
        OnAddZhanLing?.Invoke(forceId, ZhanLingPrice);
        SetTradeZhanLing(ForceIds, false);
    }

    private void Display(bool isDisplay) => gameObject.SetActive(isDisplay);

    [Serializable]private class ZhanLingSectUi
    {
        [SerializeField] private BaYeLingSelectBtn LingSelect;
        [SerializeField] private AdConsumeController AdConsume;
        [SerializeField] private Button BuyButton;
        [SerializeField] private Text Price;

        public void Set(int forceId,int price, UnityAction<int> onBuyAction, UnityAction<int> onRewardLing)
        {
            Price.text = price.ToString();
            LingSelect.Set(forceId, 1);
            BuyButton.onClick.RemoveAllListeners();
            BuyButton.onClick.AddListener(() => onBuyAction.Invoke(forceId));

            if (onRewardLing == null) return;
            AdConsume.Init();
            AdConsume.SetCallBackAction(success =>
            {
                if (success) onRewardLing.Invoke(forceId);
            }, _ => onRewardLing.Invoke(forceId), ViewBag.Instance().SetValue(0), true);
        }

        public void Init() => AdConsume.Init();
    }
}