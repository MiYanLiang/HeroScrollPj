using System.Collections;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
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
    [SerializeField]Text Info;
    [SerializeField]Text CardCapability;
    [SerializeField]Text CardCapabilityTxt;

    [SerializeField]GameObject UpLevelEffect; //升星特效 
    [SerializeField] private CardUpgradeWindow cardUpgradeWindow;
    [SerializeField] private ArouseWindow arouseWindow;
    [SerializeField] private Button arouseButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private PointDeskDeputyView pointDeskDeputyView;
    [SerializeField] private DeputySelectionView deputySelectionView;
    [SerializeField] private GameCardPropertyViewUi gameCardPropertyView;
    [SerializeField] CardInfoTagUi About;
    [SerializeField] CardInfoTagUi Military;
    [SerializeField] CardInfoTagUi Armed;
    [SerializeField] CardInfoTagUi CombatType;
    [SerializeField] CardInfoTagUi Element;
    [SerializeField] CardInfoTagUi Major;

    public CardEvent OnMergeCard = new CardEvent();
    public CardEvent OnCardSell = new CardEvent();
    public CardEvent OnEnlistCall = new CardEvent();
    public CardRecallEvent OnArouseCall = new CardRecallEvent();
    public CardEvent OnCancelDeputy = new CardEvent();
    public IndexCardEvent OnDeputySubmit = new IndexCardEvent();

    public void Init()
    {
        UpLevelEffect.gameObject.SetActive(false);
        arouseWindow.Init();
        pointDeskDeputyView.Init(OnClickDeputyAction, SelectedCardEnlistSwitch, c => OnCancelDeputy.Invoke(c));
        deputySelectionView.Init();
        cardUpgradeWindow.Init(() => OnCardSell.Invoke(SelectedCard.Card), 
            () => OnMergeCard.Invoke(SelectedCard.Card));
        if(TagSelectionPointer)
            TagSelectionPointer.SetActive(false);
    }

    private void OnClickDeputyAction(int index, GameCard deputyCard)
    {
        var debuties = GetAvailableDeputy(SelectedCard.Card);
        var selectedIndex = index;
        deputySelectionView.Set(debuties, deputy =>
        {
            var card = SelectedCard.Card;
            PlayerDataForGame.instance.hstData.heroSaveData.UnlockDeputies(deputy.CardId);
            switch (selectedIndex)
            {
                case 0 :
                {
                    card.Deputy1Id = deputy.CardId;
                    card.Deputy1Level = deputy.Level;
                    break;
                }
                case 1 :
                {
                    card.Deputy2Id = deputy.CardId;
                    card.Deputy2Level = deputy.Level;
                    break;
                }
                case 2 :
                {
                    card.Deputy3Id = deputy.CardId;
                    card.Deputy3Level = deputy.Level;
                    break;
                }
                case 3 :
                {
                    card.Deputy4Id = deputy.CardId;
                    card.Deputy4Level = deputy.Level;
                    break;
                }
            }

            OnDeputySubmit.Invoke(card, index, deputy.CardId);
        } , () => OnCancelDeputy.Invoke(deputyCard), deputyCard);
    }

    private GameCard[] GetAvailableDeputy(GameCard general)
    {
        var heroes = PlayerDataForGame.instance.hstData.heroSaveData
            .Where(c =>
            {
                if (c.Level <= 0) return false;
                if (c.Type != (int)GameCardType.Hero) return false;
                return DataTable.Hero[c.CardId].Deputy > 0;
            })
            .OrderByDescending(c => c.IsFight)
            .ThenByDescending(c => c.Arouse)
            .ThenByDescending(c => c.Level)
            .ThenByDescending(c => c.GetInfo().Rare)
            .ToArray();
        var deputies = heroes.GetDeputyIds().ToList();
        deputies.Add(general.CardId);//不包括主将
        return heroes.Where(c=>!deputies.Contains(c.CardId)).ToArray();
    }

    public void SelectCard(GameCard card)
    {
        SelectedCard.Init(card);
        SelectedCard.Set(GameCardUi.CardModes.Desk);
        var isHero = card.Type == (int)GameCardType.Hero;
        if (isHero)
        {
            var deputies = PlayerDataForGame.instance.hstData.heroSaveData.GetDeputyIds();
            if (deputies.Contains(card.CardId))
                SelectedCard.CityOperation.SetState(GameCardCityUiOperation.States.Deputy);
        }
        var consume = DataTable.Hero[card.CardId].ArouseConsumes.Where((_, i) => i >= card.Arouse).FirstOrDefault();
        var arouseAble = isHero && card.Level > 0 && consume != null;
        SetButtonInteractable(arouseButton, arouseAble);
        //详情信息取消名字显示
        //Fullname.text = info.Name;
        //Fullname.color = ColorDataStatic.GetNameColor(info.Rare);
        var isCardEnlistAble = card.IsEnlistAble();
        CardCapability.gameObject.SetActive(isCardEnlistAble);
        CardCapabilityTxt.text = isCardEnlistAble ? card.Power().ToString() : string.Empty;
        EnlistText.text =
            $"{PlayerDataForGame.instance.TotalCardsEnlisted}/{DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level].CardLimit}";

        UpdateUpgradeWindow(card);
        gameCardPropertyView.UpdateAttributes(card);
        UpdateInfoTags(card);
        TagSelectionPointer.SetActive(false);
        pointDeskDeputyView.UpdateCardUi(card);
        if (arouseWindow.gameObject.activeSelf)
            arouseWindow.Set(card, isSuccess => OnArouseCall.Invoke(card, isSuccess));
    }

    private static void SetButtonInteractable(Button btn, bool interactable)
    {
        btn.interactable = interactable;
        foreach (var img in btn.GetComponentsInChildren<Image>()) 
            img.DOFade(interactable ? 1f : 0.7f, 0);
    }

    private void UpdateInfoTags(GameCard gameCard)
    {
        var major = "副将效果";
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
        var isHero = card.CardInfo.Type == GameCardType.Hero;
        if (isHero)
        {
            var c = DataTable.Hero[card.Card.CardId];
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
            if (c.Deputy > 0)
            {
                var strength = c.DeputyStrengths.Where((_, i) => i == card.Card.Level - 1).FirstOrDefault();
                var hp = c.DeputyHitPoints.Where((_, i) => i == card.Card.Level - 1).FirstOrDefault();
                var speed = c.DeputySpeeds.Where((_, i) => i == card.Card.Level - 1).FirstOrDefault();
                var dodge = c.DeputyDodges.Where((_, i) => i == card.Card.Level - 1).FirstOrDefault();
                var intelligent = c.DeputyIntelligents.Where((_, i) => i == card.Card.Level - 1).FirstOrDefault();
                var armor = c.DeputyArmors.Where((_, i) => i == card.Card.Level - 1).FirstOrDefault();
                var magicArmor = c.DeputyMagicRests.Where((_, i) => i == card.Card.Level - 1).FirstOrDefault();
                var deputyText = "副将效果：" +
                                 GenerateText("力量", strength) +
                                 GenerateText("血量", hp) +
                                 GenerateText("速度", speed) +
                                 GenerateText("闪避", dodge) +
                                 GenerateText("智力", intelligent) +
                                 GenerateText("物防", armor) +
                                 GenerateText("法防", magicArmor);

                string GenerateText(string text, int value) => 
                    value==0 ? string.Empty : $"\n{text} + {value}";

                SetOnClick(Major, deputyText);
            }
            ResetTag(Element, ElementText(card.CardInfo.Element, false));
            SetOnClick(Element, ElementText(card.CardInfo.Element, true));
            Info.text = m.Detail;
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
            Info.text = gameCard.GetInfo().Intro;
        }

        void ResetTag(CardInfoTagUi ui, string text)
        {
            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = false;
            ui.Text.text = text;
            ui.Text.color = Color.white;
        }

        void SetOnClick(CardInfoTagUi ui,string text)
        {
            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = true;
            ui.Text.color = ColorDataStatic.name_deepRed;
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

    private void UpdateUpgradeWindow(GameCard card)
    {
        //合成还是升星
        var upgrade = card.Level == 0 ? CardUpgradeWindow.Upgrades.Fragment : CardUpgradeWindow.Upgrades.Leveling;
        if (card.Level >= DataTable.CardLevel.Keys.Max())
            upgrade = CardUpgradeWindow.Upgrades.MaxLevel;
        var cost = 0;
        if (upgrade != CardUpgradeWindow.Upgrades.MaxLevel)
            cost = DataTable.CardLevel[card.Level + 1].YuanBaoConsume;
        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(() => cardUpgradeWindow.Set(card.GetValue(), cost, upgrade));
        arouseButton.onClick.RemoveAllListeners();
        arouseButton.onClick.AddListener(
            () => arouseWindow.Set(card, isSuccess => OnArouseCall.Invoke(card, isSuccess)));
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
    private void SelectedCardEnlistSwitch()
    {
        SelectedCard.Card.IsFight++;
        if (SelectedCard.Card.IsFight > 1)
            SelectedCard.Card.IsFight = 0;
        OnEnlistCall.Invoke(SelectedCard.Card);
    }

    void OnDisable()
    {
        UpLevelEffect.gameObject.SetActive(false);
    }
    public class CardEvent : UnityEvent<GameCard> { }
    public class CardRecallEvent : UnityEvent<GameCard,UnityAction<bool>> { }
    public class IndexCardEvent : UnityEvent<GameCard,int,int> { }
}