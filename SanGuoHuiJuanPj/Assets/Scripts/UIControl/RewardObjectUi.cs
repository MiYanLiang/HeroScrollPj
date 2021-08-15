using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardObjectUi : MonoBehaviour
{
    private enum Items
    {
        None,
        YuanBao,
        YuQue,
        Exp,
        Stamina,
        Card,
        AdPass
    }
    [SerializeField] private Image YuanBaoImg;
    [SerializeField] private Image YuQueImg;
    [SerializeField] private Image ExpImg;
    [SerializeField] private Image StaminaImg;
    [SerializeField] private Image AdPassImg;
    [SerializeField] private GameCardUi CardUi;
    [SerializeField] private Text Value;

    private Dictionary<Items, GameObject> ItemMap
    {
        get
        {
            if (_itemsMap == null)
                _itemsMap = new Dictionary<Items, GameObject>
                {
                    {Items.YuanBao, YuanBaoImg.gameObject},
                    {Items.YuQue, YuQueImg.gameObject},
                    {Items.Exp, ExpImg.gameObject},
                    {Items.Stamina, StaminaImg.gameObject},
                    {Items.Card, CardUi.gameObject},
                    {Items.AdPass, AdPassImg.gameObject}
                };
            return _itemsMap;
        }
    }
    private Dictionary<Items, GameObject> _itemsMap;

    public void SetYuanBao(int yuanBao)
    {
        DisplayItem(Items.YuanBao);
        Value.text = yuanBao.ToString();
    }
    public void SetYuQue(int yuQue)
    {
        DisplayItem(Items.YuQue);
        Value.text = yuQue.ToString();
    }
    public void SetExp(int exp)
    {
        DisplayItem(Items.Exp);
        Value.text = exp.ToString();
    }

    public void SetStamina(int stamina)
    {
        DisplayItem(Items.Stamina);
        Value.text = stamina.ToString();
    }

    public void SetAdPass(int adPass)
    {
        DisplayItem(Items.AdPass);
        Value.text = adPass.ToString();
    }
    
    public void SetCard(GameCard card)
    {
        DisplayItem(Items.Card);
        CardUi.Init(card);
        CardUi.Set(GameCardUi.CardModes.Desk);
        CardUi.CityOperation.OffChipValue();
        CardUi.CityOperation.OffEnlisted();
        CardUi.SetLevel(0);
        Value.text = card.Chips.ToString();
    }

    private void DisplayItem(Items item)
    {
        gameObject.SetActive(true);
        foreach (var o in ItemMap) o.Value.SetActive(o.Key == item);
    }

    public void Off()
    {
        foreach (var o in ItemMap) o.Value.SetActive(false);
        gameObject.SetActive(false);
    }
}