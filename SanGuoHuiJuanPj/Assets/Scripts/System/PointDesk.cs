using System;
using System.Collections;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

//点将台
public class PointDesk : MonoBehaviour
{
    public ForceFlagUI FlagUi;
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

    public void SelectCard(GameCard card)
    {
        SelectedCard.Init(card);
        SelectedCard.Set(GameCardUi.CardModes.Desk);
        var info = card.GetInfo();
        //详情信息取消名字显示
        //Fullname.text = info.Name;
        //Fullname.color = ColorDataStatic.GetNameColor(info.Rare);
        strength.text = info.Type == GameCardType.Hero
            ? card.level > 0
                ?
                string.Format(DataTable.GetStringText(32), info.GetDamage(card.Level))
                : string.Empty
            : string.Empty;
        hitpoint.text = info.Type == GameCardType.Hero
            ? card.level > 0 ? string.Format(DataTable.GetStringText(33), info.GetHp(card.Level)) : string.Empty
            : string.Empty;
        //intelligent.text = info.Type == GameCardType.Hero
        Info.text = info.Intro;
        var isCardEnlistAble = card.IsEnlistAble();
        CardCapability.gameObject.SetActive(isCardEnlistAble);
        CardCapabilityTxt.text = isCardEnlistAble ? card.Power().ToString() : string.Empty;
        UpdateMergeInfo(card);
        UpdateSellingPrice(card);
        UpdateEnlist();
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

    ////升级卡牌后更新显示 
    //private void UpdateLevelCardUi()
    //{
    //    //Debug.Log("selectCardData.Level: " + selectCardData.Level); 
    //    Transform listCard = lastSelectImg.transform.parent;
    //    if (SelectedCard.Card.level < DataTable.CardLevel.Keys.Max())
    //    {
    //        var consume = DataTable.CardLevel[SelectedCard.Card.level + 1].ChipsConsume;
    //        listCard.GetChild(2).GetComponent<Text>().text = SelectedCard.Card.chips + "/" + consume;
    //        listCard.GetChild(2).GetComponent<Text>().color =
    //            selectCardData.chips >= consume ? ColorDataStatic.deep_green : Color.white;
    //    }
    //    else
    //    {
    //        listCard.GetChild(2).GetComponent<Text>().text = "";
    //    }
    //    listCard.GetChild(4).GetComponent<Image>().enabled = true;
    //    //设置星级展示 
    //    listCard.GetChild(4).GetComponent<Image>().sprite = GameResources.GradeImg[selectCardData.level];
    //    listCard.GetChild(8).gameObject.SetActive(false);
    //    listCard.GetComponent<Button>().onClick.Invoke();
    //}
    public void SetForce(int forceId) => FlagUi.Set((ForceFlags) forceId);

    public class CardEvent : UnityEvent<GameCard> { }
}