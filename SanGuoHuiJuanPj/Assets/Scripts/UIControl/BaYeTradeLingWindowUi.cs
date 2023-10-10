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
    [SerializeField] private ZhanLingSectUi ZhanLing;
    [SerializeField] private Button CloseButton;
    [SerializeField] private Text BuyTimes;
    private event UnityAction<int,int> OnAddZhanLing;
    private int ZhanLingPrice => Prices[index];
    private int[] Prices;
    private int index;
    private bool IsConsumedAd { get; set; }
    private int ForceId { get; set; }

    public void Init(UnityAction<int,int> onAddZhanLing)
    {
        Prices = BaYeManager.instance.BaYeLingPrices;
        OnAddZhanLing += onAddZhanLing;
        ZhanLing.Init();
        Display(false);
    }

    public void SetTradeZhanLing(int forceId, bool reset = true)
    {
        ForceId = forceId;
        if (reset)
        {
            index = 0;
            IsConsumedAd = false;
        }

        BuyTimes.text = $"{(Prices.Length - index)}";
        if (IsConsumedAd)
            ZhanLing.Set(forceId, ZhanLingPrice, OnBuyAction, () => IncreasePriceUpdate(true));
        else
            ZhanLing.Set(forceId, ZhanLingPrice, OnBuyAction, null);
        Display(true);
    }

    private void OnBuyAction()
    {
        var baye = PlayerDataForGame.instance.baYe;
        if (baye.gold < ZhanLingPrice)
        {
            PlayerDataForGame.instance.ShowStringTips("金币不够！");
            return;
        }
        IncreasePriceUpdate();
    }

    public void IncreasePriceUpdate(bool isConsumeAd = false)
    {
        if (!IsConsumedAd) IsConsumedAd = isConsumeAd;
        OnAddZhanLing?.Invoke(ForceId, ZhanLingPrice);
        index++;
        if (index >= Prices.Length)
        {
            Display(false);
            return;
        }
        SetTradeZhanLing(ForceId, false);
    }

    private void Display(bool isDisplay) => gameObject.SetActive(isDisplay);

    [Serializable]private class ZhanLingSectUi
    {
        [SerializeField] private BaYeLingSelectBtn LingSelect;
        [SerializeField] private AdConsumeController AdConsume;
        [SerializeField] private Button BuyButton;
        [SerializeField] private Text Price;

        public void Set(int forceId,int price, UnityAction onBuyAction, UnityAction onRewardLing)
        {
            Price.text = price.ToString();
            LingSelect.Set(forceId, 1);
            BuyButton.onClick.RemoveAllListeners();
            BuyButton.onClick.AddListener(onBuyAction.Invoke);

            if (onRewardLing == null) return;
            AdConsume.SetCallBackAction(success =>
            {
                if (success) onRewardLing.Invoke();
            }, _ => onRewardLing.Invoke(), true, 0);
        }

        public void Init() => AdConsume.Init();
    }
}