using System;
using System.Collections.Generic;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TradableGameCardUi : MonoBehaviour
{
    private enum States
    {
        Free,
        Sell,
        Ad
    }
    public GameCardUi GameCard;
    public GameCard Card { get; private set; }
    public Button BuyBtn;
    public Text FreeLabel;
    public Text PriceLabel;
    //public Image AdLabel;
    public AdConsumeController AdConsume;
    public UnityEvent OnClickAction;
    private Dictionary<States, Action<bool>> stateSet;
    private Dictionary<States, Action<bool>> StateSet
    {
        get
        {
            if (stateSet == null)
            {
                stateSet = new Dictionary<States, Action<bool>>
                {
                    {States.Free, on=> FreeLabel.gameObject.SetActive(on)},
                    {States.Sell,on=> PriceLabel.gameObject.SetActive(on)},
                    {States.Ad, on =>
                    {
                        if(on) AdConsume.ShowWithUpdate();
                        else AdConsume.Off();
                    }}
                };
            }
            return stateSet;
        }
    }

    void Start()
    {
        GameCard.CityOperation.OnclickAction.AddListener(() => OnClickAction.Invoke());
        AdConsume.Init();
    }

    public void SetAd(UnityAction<bool> action)
    {
        SetState(States.Ad);
        AdConsume.SetCallBackAction(action, _ => action.Invoke(true), ViewBag.Instance().SetValue(0), true);
    }

    public void SetPrice(int price, UnityAction buyAction)
    {
        PriceLabel.text = price.ToString();
        SetState(price == 0 ? States.Free : States.Sell);
        BuyBtn.onClick.RemoveAllListeners();
        BuyBtn.onClick.AddListener(buyAction);
    }

    private void SetState(States state)
    {
        foreach (var item in StateSet) item.Value.Invoke(item.Key == state);
    }

    public void SetSelect(bool isSelected) => GameCard.Selected(isSelected);

    public void SetGameCard(GameCard card)
    {
        Card = card;
        gameObject.SetActive(true);
        GameCard.Init(card);
        GameCard.Set(GameCardUi.CardModes.Desk);
        GameCard.CityOperation.OffChipValue();
    }

    public void Off() => gameObject.SetActive(false);
}