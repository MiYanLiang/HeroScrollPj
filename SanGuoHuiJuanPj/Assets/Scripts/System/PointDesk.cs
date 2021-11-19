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
        //EnlistBtn.onClick.AddListener(()=>OnEnlistCall.Invoke(SelectedCard.Card));
    }

    private void UpdateAttributes()
    {
        var c = SelectedCard;
        PowerUi.Set(c.Card.Power(), null);
        StrUi.Set(c.CardInfo.Strength, null);
        HpUi.Set(c.CardInfo.HitPoint, null);
        IntUi.Set(c.CardInfo.Intelligent, null);
        SpeedUi.Set(c.CardInfo.Speed, null);
        DodgeUi.Set(c.CardInfo.DodgeRatio, null);
        ArmorUi.Set(c.CardInfo.ArmorResist, null);
        MagicUi.Set(c.CardInfo.MagicResist, null);
        CriUi.Set(c.CardInfo.CriticalRatio, null);
        RouUi.Set(c.CardInfo.RougeRatio, null);
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
    }

    private void UpdateInfoTags()
    {
        var major = "主副将";
        ResetTag(About, "故事");
        ResetTag(Military, "兵种");
        ResetTag(Armed, "系数");
        ResetTag(About, "战斗");
        ResetTag(Element, "元素");
        ResetTag(Major, major);
        var card = SelectedCard;
        var about = string.Empty;
        var military = string.Empty;
        var armed = string.Empty;
        var combatType = string.Empty;
        var element = string.Empty;
        if (card.CardInfo.Type == GameCardType.Hero)
        {
            var c = DataTable.Hero[card.Card.id];
            var m = DataTable.Military[c.MilitaryUnitTableId];
            switch (m.ArmedType)
            {
                case 0  : armed ="普通系";break;
                case 1  : armed ="护盾系";break;
                case 2  : armed ="步兵系";break;
                case 3  : armed ="长持系";break;
                case 4  : armed ="短持系";break;
                case 5  : armed ="骑兵系";break;
                case 6  : armed ="器械系";break;
                case 7  : armed ="弓弩系";break;
                case 8  : armed ="战船系";break;
                case 9  : armed ="蛮族系";break;
                case 10 : armed ="统御系";break;
                case 11 : armed ="干扰系";break;
                case 12 : armed ="辅助系";break;
                case 13 : armed ="投掷系";break;
                case 14 : armed ="猛兽系";break;
                case 15: armed = "召唤系";break;
            }
            military = m.Title;
            combatType = m.CombatStyle == 1 ? "远程" : "近战";
            about = card.CardInfo.About;
            SetOnClick(About, about);
            SetOnClick(Military, military);
            SetOnClick(Armed, armed);
            SetOnClick(CombatType , combatType);
        }
        else
        {
            military = card.CardInfo.Name;
            armed = combatType = card.CardInfo.Type == GameCardType.Tower ? "塔" : "陷阱";
            about = card.CardInfo.Intro;
            SetOnClick(About, about);
            SetOnClick(Military, military);
            ResetTag(Armed, armed);
            ResetTag(CombatType, combatType);
        }
        ResetTag(Element, ElementText(card.CardInfo.Element));



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
            ui.Button.onClick.AddListener(() => Info.text = text);
        }

        string ElementText(int e)
        {
            switch (e)
            {
                case CombatConduct.PhysicalDmg: return "物理";
                case CombatConduct.MechanicalDmg: return "器械";
                case CombatConduct.FixedDmg: return "固伤";
                case CombatConduct.BasicMagicDmg: return "法术";
                case CombatConduct.WindDmg: return "风伤";
                case CombatConduct.ThunderDmg: return "雷伤";
                case CombatConduct.WaterDmg: return "水伤";
                case CombatConduct.PoisonDmg: return "毒伤";
                case CombatConduct.FireDmg: return "火伤";
                case CombatConduct.EarthDmg: return "土伤";
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
        AudioController0.instance.ChangeAudioClip(15);
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