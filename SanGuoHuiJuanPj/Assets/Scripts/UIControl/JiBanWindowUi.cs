using System;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class JiBanWindowUi : MonoBehaviour
{
    [SerializeField] private Image BackgroundImg;
    [SerializeField] private Image TitleImg;
    [SerializeField] private Text Info;
    [SerializeField] private Transform content;
    [SerializeField] private GameCardUi GameCardUiPrefab;

    private bool isInit;

    public void Set(int jiBanId)
    {
        if(isInit)return;
        isInit = true;
        var jiBan = DataTable.JiBan[jiBanId];
        BackgroundImg.sprite = GameResources.Instance.JiBanBg[jiBanId];
        TitleImg.sprite = GameResources.Instance.JiBanHText[jiBanId];
        Info.text = jiBan.JiBanEffect;
        SetCards(jiBan.Cards);
    }

    private void SetCards(CardElement[] cards)
    {
        var gameCards = PlayerDataForGame.instance.GetCards(false)
            .Join(cards, g => (g.CardId, g.Type), c => (c.CardId, c.CardType), (g, _) => g).ToArray();
        foreach (var element in cards)
        {
            var card = GameCard.Instance(element.CardId, element.CardType, level: 0, arouse: 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var ui = Instantiate(GameCardUiPrefab,content);
            ui.Init(card);
            ui.Set(GameCardUi.CardModes.Desk);
            ui.CityOperation.OffChipValue();
            ui.Level.gameObject.SetActive(false);
            ui.CityOperation.SetDisable(!gameCards.Any(g => g.CardId == element.CardId && g.Type == element.CardType));
        }
    }

    public void Show() => gameObject.SetActive(true);
}
