using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

//点将台
public class PointDesk : MonoBehaviour
{
    public Text EnlistText;//出战数

    //信息物件
    public GameCardUi SelectedCard;

    [SerializeField] GameObject TagSelectionPointer;
    //[SerializeField]Text Fullname;        //详情信息取消名字显示
    [SerializeField] Text strength;
    [SerializeField] Text hitpoint;
    [SerializeField] Text intelligent;
    [SerializeField] Text speed;
    [SerializeField] Text dodgeRatio;
    [SerializeField] Text armorResist;
    [SerializeField] Text magicResist;
    [SerializeField] Text criticalRatio;
    [SerializeField] Text rougeRatio;
    [SerializeField] ScrollRect attScrollRect;

    [SerializeField]Text Info;
    [SerializeField]Button CardMergeBtn;
    [SerializeField]Image MergeImg;
    [SerializeField]Image UpgradeImg;
    [SerializeField]Text MergeCost;
    [SerializeField]Button CardSellBtn;
    [SerializeField]Text SellingPrice;
    [SerializeField]Button EnlistBtn;
    [SerializeField]Text CardCapability;
    [SerializeField]Text CardCapabilityTxt;

    [SerializeField]Text EnlistBtnLabel;
    [SerializeField]GameObject UpLevelEffect; //升星特效 

    [SerializeField]TextImageUi PowerUi;
    [SerializeField]TextImageUi StrUi;
    [SerializeField]TextImageUi HpUi;
    [SerializeField]TextImageUi IntUi;
    [SerializeField]TextImageUi SpeedUi;
    [SerializeField]TextImageUi DodgeUi;
    [SerializeField]TextImageUi ArmorUi;
    [SerializeField]TextImageUi MagicUi;
    [SerializeField]TextImageUi CriUi;
    [SerializeField]TextImageUi RouUi;

    [SerializeField] CardInfoTagUi About;
    [SerializeField] CardInfoTagUi Military;
    [SerializeField] CardInfoTagUi Armed;
    [SerializeField] CardInfoTagUi CombatType;
    [SerializeField] CardInfoTagUi Element;
    [SerializeField] CardInfoTagUi Major;

    public CardEvent OnMergeCard = new CardEvent();
    public CardEvent OnCardSell = new CardEvent();
    public CardEvent OnEnlistCall = new CardEvent();

    private GameObject[] _infoObjs;

    private GameObject[] InfoObjs
    {
        get
        {
            if (_infoObjs == null)
            {
                _infoObjs = new GameObject[]
                {
                    SelectedCard.gameObject,
                    EnlistText.gameObject,
                    //详情信息取消名字显示
                    //Fullname.gameObject,
                    strength.gameObject,
                    hitpoint.gameObject,
                    intelligent.gameObject,
                    speed.gameObject,
                    dodgeRatio.gameObject,
                    armorResist.gameObject,
                    magicResist.gameObject,
                    criticalRatio.gameObject,
                    rougeRatio.gameObject,
                    Info.gameObject,
                    CardMergeBtn.gameObject,
                    CardSellBtn.gameObject,
                    EnlistBtn.gameObject
                };
            }

            return _infoObjs;
        }
    }
    

    public void Init()
    {
        CardMergeBtn.onClick.AddListener(()=>OnMergeCard.Invoke(SelectedCard.Card));
        CardSellBtn.onClick.AddListener(()=>OnCardSell.Invoke(SelectedCard.Card));
        EnlistBtn.onClick.AddListener(EnlistSwitch);
        if(TagSelectionPointer)
            TagSelectionPointer.SetActive(false);
        //EnlistBtn.onClick.AddListener(()=>OnEnlistCall.Invoke(SelectedCard.Card));
    }

    private void UpdateAttributes()
    {
        StrUi.Off();
        HpUi.Off();
        IntUi.Off();
        SpeedUi.Off();
        DodgeUi.Off();
        ArmorUi.Off();
        MagicUi.Off();
        CriUi.Off();
        RouUi.Off();

        var c = SelectedCard;
        var contentRect = 60 * 4;
        PowerUi.Set(c.Card.Power(), null);
        StrUi.Set(c.CardInfo.GetStrength(c.Card.Level), null);
        HpUi.Set(c.CardInfo.GetHp(c.Card.Level), null);
        if (c.CardInfo.Type != GameCardType.Trap) 
            SpeedUi.Set(c.CardInfo.Speed, null);

        if (c.CardInfo.Type == GameCardType.Hero)
        {
            IntUi.Set(c.CardInfo.Intelligent, null);
            DodgeUi.Set(c.CardInfo.DodgeRatio, null);
            ArmorUi.Set(c.CardInfo.ArmorResist, null);
            MagicUi.Set(c.CardInfo.MagicResist, null);
            CriUi.Set(c.CardInfo.CriticalRatio, null);
            RouUi.Set(c.CardInfo.RougeRatio, null);
            contentRect = 60 * 10;
        }

        var size = attScrollRect.content.sizeDelta;
        attScrollRect.content.sizeDelta = new Vector2(size.x, contentRect);
    }

    public void SelectCard(GameCard card)
    {
        SelectedCard.Init(card);
        SelectedCard.Set(GameCardUi.CardModes.Desk);
        var info = card.GetInfo();
        //详情信息取消名字显示
        //Fullname.text = info.Name;
        //Fullname.color = ColorDataStatic.GetNameColor(info.Rare);
        Info.text = info.Intro;
        var isCardEnlistAble = card.IsEnlistAble();
        CardCapability.gameObject.SetActive(isCardEnlistAble);
        CardCapabilityTxt.text = isCardEnlistAble ? card.Power().ToString() : string.Empty;
        UpdateMergeInfo(card);
        UpdateSellingPrice(card);
        UpdateEnlist();
        UpdateAttributes();
        UpdateInfoTags();
        TagSelectionPointer.gameObject.SetActive(false);
    }

    private void UpdateInfoTags()
    {
        var major = "主副将";
        ResetTag(About, "典故");
        ResetTag(Military, "兵种");
        ResetTag(Armed, "系数");
        ResetTag(CombatType, "战斗");
        ResetTag(Element, "元素");
        ResetTag(Major, major);
        var card = SelectedCard;
        var about = string.Empty;
        var military = string.Empty;
        var armed = string.Empty;
        var combatType = string.Empty;
        if (card.CardInfo.Type == GameCardType.Hero)
        {
            var c = DataTable.Hero[card.Card.id];
            var m = DataTable.Military[c.MilitaryUnitTableId];
            switch (m.ArmedType)
            {
                case 0:
                    armed = "普通系";
                    break;
                case 1:
                    armed = "护盾系";
                    break;
                case 2:
                    armed = "步兵系";
                    break;
                case 3:
                    armed = "长持系";
                    break;
                case 4:
                    armed = "短持系";
                    break;
                case 5:
                    armed = "骑兵系";
                    break;
                case 6:
                    armed = "器械系";
                    break;
                case 7:
                    armed = "弓弩系";
                    break;
                case 8:
                    armed = "战船系";
                    break;
                case 9:
                    armed = "蛮族系";
                    break;
                case 10:
                    armed = "统御系";
                    break;
                case 11:
                    armed = "干扰系";
                    break;
                case 12:
                    armed = "辅助系";
                    break;
                case 13:
                    armed = "投掷系";
                    break;
                case 14:
                    armed = "猛兽系";
                    break;
                case 15:
                    armed = "召唤系";
                    break;
            }

            military = m.Title;
            var combatText = string.Empty;
            if (m.CombatStyle == 1)
            {
                combatType = "远程";
                combatText = DataTable.GetStringText(92);
            }
            else
            {
                combatType = "近战";
                combatText = DataTable.GetStringText(91);
            }

            about = card.CardInfo.Intro;
            SetOnClick(About, about);
            ResetTag(Military, military);
            SetOnClick(Military, m.Detail);
            ResetTag(Armed, armed);
            ResetTag(CombatType, combatType);
            SetOnClick(CombatType, combatText);
            ResetTag(Element, ElementText(card.CardInfo.Element, false));
            SetOnClick(Element, ElementText(card.CardInfo.Element, true));
        }
        else
        {
            military = card.CardInfo.Name;
            armed = card.CardInfo.Type == GameCardType.Tower ? "建筑" : "陷阱";
            ResetTag(Military, military);
            SetOnClick(Military, card.CardInfo.Intro);
            ResetTag(Armed, armed);
            ResetTag(CombatType, string.Empty);
            ResetTag(Element, string.Empty);
        }

        void ResetTag(CardInfoTagUi ui, string text)
        {
            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = false;
            ui.Text.text = text;
        }

        void SetOnClick(CardInfoTagUi ui,string text)
        {
            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = true;
            ui.Button.onClick.AddListener(() =>
            {
                if (TagSelectionPointer)
                {
                    TagSelectionPointer.SetActive(false);
                    TagSelectionPointer.transform.SetParent(ui.transform);
                    TagSelectionPointer.transform.localPosition = Vector3.zero;
                    TagSelectionPointer.SetActive(true);
                }
                Info.text = text;
            });
        }

        string ElementText(int e,bool detail)
        {
            switch (e)
            {
                case CombatConduct.FixedDmg: 
                    return detail ? DataTable.GetStringText(103) : DataTable.GetStringText(93);
                case CombatConduct.PhysicalDmg:
                    return detail ? DataTable.GetStringText(104) : DataTable.GetStringText(94);
                case CombatConduct.MechanicalDmg:
                    return detail ? DataTable.GetStringText(105) : DataTable.GetStringText(95);
                case CombatConduct.BasicMagicDmg:
                    return detail ? DataTable.GetStringText(106) : DataTable.GetStringText(96);
                case CombatConduct.WindDmg: 
                    return detail ? DataTable.GetStringText(107) : DataTable.GetStringText(97);
                case CombatConduct.ThunderDmg:
                    return detail ? DataTable.GetStringText(108) : DataTable.GetStringText(98);
                case CombatConduct.WaterDmg: 
                    return detail ? DataTable.GetStringText(109) : DataTable.GetStringText(99);
                case CombatConduct.PoisonDmg:
                    return detail ? DataTable.GetStringText(110) : DataTable.GetStringText(100);
                case CombatConduct.FireDmg: 
                    return detail ? DataTable.GetStringText(111) : DataTable.GetStringText(101);
                case CombatConduct.EarthDmg:
                    return detail ? DataTable.GetStringText(112) : DataTable.GetStringText(102);
                default: return string.Empty;
            }
        }
    }

    private void UpdateSellingPrice(GameCard card)
    {
        var value = card.GetValue();
        SellingPrice.text = value.ToString();
    }

    private void UpdateMergeInfo(GameCard card)
    {
        var isFragment = card.Level == 0;//合成还是升星
        MergeImg.gameObject.SetActive(isFragment);
        UpgradeImg.gameObject.SetActive(!isFragment);
        var isMax = card.Level >= DataTable.CardLevel.Keys.Max();
        CardMergeBtn.gameObject.SetActive(!isMax);
        if (isMax) return;
        var cost = DataTable.CardLevel[card.Level + 1].YuanBaoConsume;
        MergeCost.text = cost.ToString();
    }

    public void PlayUpgradeEffect() => StartCoroutine(CardUpgradeEffect());

    //隐藏升星特效 
    IEnumerator CardUpgradeEffect()
    {
        UpLevelEffect.SetActive(false);
        UpLevelEffect.SetActive(true);
        yield return new WaitForSeconds(1.7f);
        UpLevelEffect.SetActive(false);
    }

    /// <summary> 
    /// 出战或回城设置方法 
    /// </summary> 
    public void EnlistSwitch()
    {
        var lastCondition = SelectedCard.Card.isFight > 0;
        var isSuccess = PlayerDataForGame.instance.EnlistCard(SelectedCard.Card, !lastCondition);
        var isEnlisted = SelectedCard.Card.isFight > 0;
        if (!isSuccess)
        {
            UIManager.instance.PlayOnClickMusic();
            return;
        }
        SelectedCard.CityOperation.SetState(isEnlisted
            ? GameCardCityUiOperation.States.Enlisted
            : GameCardCityUiOperation.States.None);
        AudioController0.instance.ChangeAudioClip(19);
        AudioController0.instance.PlayAudioSource(0);
        UpdateEnlist();
        OnEnlistCall.Invoke(SelectedCard.Card);
    }

    private void UpdateEnlist()
    {
        EnlistBtnLabel.text = DataTable.GetStringText(SelectedCard.Card.isFight > 0 ? 30 : 31);
        EnlistBtn.gameObject.SetActive(SelectedCard.Card.Level > 0);
        EnlistText.text =
            $"{PlayerDataForGame.instance.TotalCardsEnlisted}/{DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level].CardLimit}";
    }

    public class CardEvent : UnityEvent<GameCard> { }
}