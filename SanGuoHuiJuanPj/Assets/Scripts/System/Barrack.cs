using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

//主城页面
public class Barrack : MonoBehaviour
{
    public PointDesk PointDesk;
    public int SelectedForce;
    public Image ChangeTroop;//切换军团按钮
    public Text TroopName;
    public Image TroopNameImage;
    public GameCardUi GameCardUiPrefab;
    public Transform InventoryContent;
    private List<GameCardUi> _cardPool = new List<GameCardUi>();

    public void Init(UnityAction<GameCard> onCardMerge,UnityAction<GameCard> onCardSell,UnityAction<GameCard> onCardEnlist)
    {
        var flagBtn = ChangeTroop.gameObject.AddComponent<Button>();
        flagBtn.onClick.AddListener(() =>
        {
            RefreshCardList(SelectedForce + 1);
            RefreshTroopName(SelectedForce + 1);
            AudioController0.instance.RandomPlayGuZhengAudio();//随机播放古筝声音
        });
        PointDesk.Init();
        PointDesk.OnMergeCard.AddListener(onCardMerge);
        PointDesk.OnCardSell.AddListener(onCardSell);
        PointDesk.OnEnlistCall.AddListener(onCardEnlist);//出战
    }
    private void RefreshTroopName(int troop)
    {
        string troopName="";
        switch (troop) 
        {
            case 1:troopName = "刘";break;
            case 2:troopName = "曹";break;
            case 3:troopName = "孙";break;
            case 4:troopName = "袁";break;
            case 5:troopName = "吕";break;
            case 6:troopName = "司马";break;
        }
        TroopName.text = troopName;
    }

    /// <summary> 
    /// 创建并展示单位列表 
    /// </summary> 
    public void RefreshCardList(int forceId = -1)
    {
        if (forceId == -1)
        {
            forceId = SelectedForce;
        }

        if (SelectedForce != forceId)
        {
            if (forceId < 0 ||
                forceId > PlayerDataForGame.instance.UnlockForceId)
            {
                SelectedForce = 0;
            }
            else SelectedForce = forceId;

            PointDesk.SetForce(SelectedForce);
        }

        PlayerDataForGame.instance.RefreshEnlisted(SelectedForce);
        ResetCardPool();
        if (_cardPool.Count == 0) return;
        GameCard selected;
        selected = PointDesk.SelectedCard?.Card;
        if (selected == null ||
            !_cardPool.Any(ui => ui.Card.CardId == selected.CardId && ui.Card.Type == selected.Type))
            selected = _cardPool.First(c => c.gameObject.activeSelf).Card;
        PointDesk.SelectCard(selected);
        HighlightSelected(selected);
        //AudioController0.instance.RandomPlayGuZhengAudio();//随机播放古筝声音
        //AudioController0.instance.ChangeAudioClip(13); //播放随机音效 
        //UIManager.instance.PlayOnClickMusic();

    }

    private void HighlightSelected(GameCard card) =>
        _cardPool.ForEach(c => c.Selected(c.Card.CardId == card.CardId && c.Card.Type == card.Type));
    private void ResetCardPool()
    {
        _cardPool.ForEach(c => c.Off());
        var cards = PlayerDataForGame.instance.hstData.heroSaveData
            .Concat(PlayerDataForGame.instance.hstData.towerSaveData)
            .Concat(PlayerDataForGame.instance.hstData.trapSaveData).Where(c => c.IsOwning())
            .Select(c => new {Card = c, Info = c.GetInfo()}).Where(c => c.Info.ForceId == SelectedForce)
            .Select(c => c.Card)
            .ToList();
        cards.Sort();
        cards.ForEach(InstanceGameCardUi);
    }

    private void InstanceGameCardUi(GameCard card)
    {
        var ui = GetCardFromPool();
        ui.Init(card);
        ui.Set(GameCardUi.CardModes.Desk);
        var state = card.level == 0 ? GameCardCityUiOperation.States.Disable :
            card.isFight > 0 ? GameCardCityUiOperation.States.Enlisted : GameCardCityUiOperation.States.None;
        ui.CityOperation.SetState(state);
        //列表中取消碎片数量显示
        ui.CityOperation.Chips.gameObject.SetActive(false);
        ui.CityOperation.OnclickAction.RemoveAllListeners();
        ui.CityOperation.OnclickAction.AddListener(() =>
        {
            PointDesk.SelectCard(ui.Card);
            HighlightSelected(card);
            UIManager.instance.PlayOnClickMusic();
        });
    }

    /// <summary> 
    /// 从卡牌池中获取空卡牌 
    /// </summary> 
    /// <returns></returns> 
    private GameCardUi GetCardFromPool()
    {
        var ui = _cardPool.FirstOrDefault(c => !c.gameObject.activeSelf);
        if (!ui)
        {
            ui = Instantiate(GameCardUiPrefab, InventoryContent);
            _cardPool.Add(ui);
        }
        return ui;
    }

}
