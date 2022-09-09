using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ArouseWindow : MonoBehaviour
{
    [SerializeField] private Text yuanBaoText;
    [SerializeField] private GameCardUi fromUi;
    [SerializeField] private GameCardUi toUi;
    [SerializeField] private GameCardUi costUi;
    [SerializeField] private TextUiListController textUiList;
    [SerializeField] private Button _arouseButton;
    [SerializeField] private Button _closeButton;

    public void Init()
    {
        _closeButton.onClick.AddListener(() => Display(false));
        textUiList.Init();
    }

    public void Set(GameCard card,UnityAction onArouseAction)
    {
        var table = DataTable.Hero[card.CardId];
        var consume = table.ArouseConsumes.Where((_, i) => i >= card.Arouse).FirstOrDefault();
        if (consume == null) return;

        textUiList.ClearList();
        InitCardUi(card, fromUi);
        var arousesCard = GameCard.Instance(card);
        arousesCard.Arouse++;
        InitCardUi(arousesCard, toUi);

        var costCard = GameCard.InstanceHero(consume.CardId, consume.CardLevel, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        InitCardUi(costCard, costUi);
        var ownCard = PlayerDataForGame.instance.hstData.heroSaveData.FirstOrDefault(h => h.CardId == consume.CardId);
        var hasCard = (ownCard?.Level ?? -1) >= consume.CardLevel;
        costUi.CityOperation.SetDisable(!hasCard);

        var strength = CompareValue(card, arousesCard, table.GetArousedStrength);
        var intelligent = CompareValue(card, arousesCard, table.GetArousedIntelligent);
        var dodge = CompareValue(card, arousesCard, table.GetArousedDodge);
        var speed = CompareValue(card, arousesCard, table.GetArousedSpeed);
        var armor = CompareValue(card, arousesCard, table.GetArousedArmor);
        var hitPoint = CompareValue(card, arousesCard, table.GetArousedHitPointAddOn);
        var magicResist = CompareValue(card, arousesCard, table.GetArousedMagicRest);
        var list = new[]
        {
            ("武力",strength),
            ("智力",intelligent),
            ("血量",hitPoint),
            ("速度",speed),
            ("闪避",dodge),
            ("护甲",armor),
            ("法免",magicResist),
        };
        yuanBaoText.text = $"x {consume.YuanBao}";
        foreach (var(title,value) in list)
        {
            if (value == 0) continue;
            textUiList.AddUi(ui => ui.Set(title, value));
        }

        _arouseButton.onClick.RemoveAllListeners();
        var isEnough = hasCard && PlayerDataForGame.instance.pyData.YuanBao >= consume.YuanBao;
        if(isEnough) _arouseButton.onClick.AddListener(onArouseAction);
        _arouseButton.interactable = isEnough;
        Display(true);
    }


    private void InitCardUi(GameCard card, GameCardUi ui)
    {
        ui.Init(card);
        ui.Set(GameCardUi.CardModes.Desk);
        ui.CityOperation.OffChipValue();
    }

    private int CompareValue(GameCard card, GameCard arousesCard, Func<int, int> func)
    {
        var fromValue = func(card.Arouse);
        var toValue = func(arousesCard.Arouse);
        return toValue - fromValue;
    }

    public void Display(bool display) => gameObject.SetActive(display);
}