using System;
using System.Collections;
using System.Linq;
using System.WarModule;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ArouseWindow : MonoBehaviour
{
    [SerializeField] private Text yuanBaoText;
    [SerializeField] private Color ybNormalColor;
    [SerializeField] private Color ybDisableColor;
    [SerializeField] private GameCardUi fromUi;
    [SerializeField] private GameCardUi toUi;
    [SerializeField] private GameCardUi costUi;
    [SerializeField] private TextUiListController textUiList;
    [SerializeField] private Button _arouseButton;
    [SerializeField] private Text _arouseText;
    [SerializeField] private Color _arouseTextNormal;
    [SerializeField] private Color _arouseTextDisable;
    [SerializeField] private Button _closeButton;
    [SerializeField] private GameObject _upgradeEffectObj;

    public void Init()
    {
        _closeButton.onClick.AddListener(() =>
        {
            UIManager.instance.PlayOnClickMusic();
            Display(false);
        });
        textUiList.Init();
        _upgradeEffectObj.SetActive(false);
        yuanBaoText.color = ybNormalColor;
        _arouseText.color = _arouseTextNormal;
    }

    public void Set(GameCard card,UnityAction<UnityAction<bool>> onArouseAction)
    {
        if (card.Type != (int)GameCardType.Hero)
            throw new InvalidOperationException("非武将卡牌，不允许打开觉醒窗口!");
        var hero = DataTable.Hero[card.CardId];
        var nextArouse = card.Arouse + 1;
        var isLevelEnough = false;
        var hasArouseConfig = DataTable.ArouseConfig.TryGetValue(nextArouse, out var arouseConfig);
        if (hasArouseConfig)
            isLevelEnough = arouseConfig.Stars <= card.Level;

        var consume = hero.ArouseConsumes.Where((_, i) => i >= card.Arouse).FirstOrDefault();
        if (consume == null) return;

        textUiList.ClearList();
        InitCardUi(card, fromUi);
        fromUi.CityOperation.SetDisable(!hasArouseConfig || !isLevelEnough);
        var arousesCard = GameCard.Instance(card);
        arousesCard.Arouse = nextArouse;
        if (hasArouseConfig && !isLevelEnough) arousesCard.Level = arouseConfig.Stars;
        InitCardUi(arousesCard, toUi);
        
        var costCard = GameCard.InstanceHero(consume.CardId, consume.CardLevel);
        InitCardUi(costCard, costUi);
        var ownCard = PlayerDataForGame.instance.hstData.heroSaveData.FirstOrDefault(h => h.CardId == consume.CardId);
        var hasCard = (ownCard?.Level ?? -1) >= consume.CardLevel;
        costUi.CityOperation.SetDisable(!hasCard);
        var strength = hero.GetArousedStrength(arouse: arousesCard.Arouse, level: arousesCard.Level) - hero.GetArousedStrength(arouse: card.Arouse, level: arousesCard.Level);
        var intelligent = hero.GetArousedIntelligent(arousesCard.Arouse) - hero.GetArousedIntelligent(card.Arouse);
        var dodge = hero.GetArousedDodge(arousesCard.Arouse) - hero.GetArousedDodge(card.Arouse);
        var speed = hero.GetArousedSpeed(arousesCard.Arouse) - hero.GetArousedSpeed(card.Arouse);
        var armor = hero.GetArousedArmor(arousesCard.Arouse) - hero.GetArousedArmor(card.Arouse);
        var hitPoint = hero.GetArousedHitPoint(arouse: arousesCard.Arouse, level: arousesCard.Level) - hero.GetArousedHitPoint(arouse: card.Arouse, level: arousesCard.Level);
        var magicResist = hero.GetArousedMagicRest(arousesCard.Arouse) - hero.GetArousedMagicRest(card.Arouse);
        var list = new[]
        {
            ("武力", strength),
            ("智力", intelligent),
            ("血量", hitPoint),
            ("速度", speed),
            ("闪避", dodge),
            ("护甲", armor),
            ("法免", magicResist),
        };
        foreach (var(title,value) in list)
        {
            if (value == 0) continue;
            textUiList.AddUi(ui => ui.Set(title, value));
        }

        _arouseButton.onClick.RemoveAllListeners();
        var isEnough = PlayerDataForGame.instance.pyData.YuanBao >= consume.YuanBao;
        var enable = hasCard && isEnough && hasArouseConfig && isLevelEnough;
        SetYuanBao(consume, !isEnough);
        if(enable) _arouseButton.onClick.AddListener(()=>onArouseAction.Invoke(isSuccess =>
        {
            if (isSuccess) PlayUpgradeEffect();
        }));
        _arouseButton.interactable = enable;
        _arouseText.color = enable ? _arouseTextNormal : _arouseTextDisable;
        Display(true);
    }

    private void SetYuanBao(ArouseConsume consume,bool disable)
    {
        yuanBaoText.text = $"{consume.YuanBao}";
        yuanBaoText.color = disable ? ybDisableColor : ybNormalColor;
    }


    private void InitCardUi(GameCard card, GameCardUi ui)
    {
        ui.Init(card);
        ui.Set(GameCardUi.CardModes.Desk);
        ui.CityOperation.SetState(GameCardCityUiOperation.States.None);
        ui.CityOperation.OffChipValue();
    }

    public void Display(bool display)
    {
        gameObject.SetActive(display);
        _upgradeEffectObj.SetActive(false);
    }

    public void PlayUpgradeEffect() => StartCoroutine(UpgradeEffect());

    private IEnumerator UpgradeEffect()
    {
        _upgradeEffectObj.gameObject.SetActive(false);
        _upgradeEffectObj.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
        _upgradeEffectObj.gameObject.SetActive(false);
    }
}