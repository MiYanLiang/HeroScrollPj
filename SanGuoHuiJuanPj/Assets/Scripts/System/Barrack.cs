using System;
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
    public Button ChangeTroop;//切换军团按钮
    public Text TroopName;
    public GameCardUi GameCardUiPrefab;
    public Transform InventoryContent;
    private List<GameCardUi> _cardPool = new List<GameCardUi>();

    public void Init(UnityAction<GameCard> onCardMerge,
        UnityAction<GameCard> onCardSell,
        UnityAction<GameCard> onCardEnlist,
        UnityAction<GameCard> onArouseCall,
        UnityAction<GameCard,int,int> onDeputySubmit,
        UnityAction<GameCard> onCancelDeputy)
    {
        ChangeTroop.onClick.AddListener(() =>
        {
            RefreshCardList(SelectedForce + 1);
            AudioController0.instance.RandomPlayGuZhengAudio(); //随机播放古筝声音
        });
        PointDesk.Init();
        PointDesk.OnMergeCard.AddListener(onCardMerge);
        PointDesk.OnCardSell.AddListener(onCardSell);
        PointDesk.OnEnlistCall.AddListener(onCardEnlist); //出战
        PointDesk.OnArouseCall.AddListener(onArouseCall);
        PointDesk.OnDeputySubmit.AddListener(onDeputySubmit);
        PointDesk.OnCancelDeputy.AddListener(onCancelDeputy);
    }

    private void RefreshTroopName(int troop) => TroopName.text = $"{DataTable.Force[troop].Short}";

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
        RefreshTroopName(SelectedForce);
        //AudioController0.instance.RandomPlayGuZhengAudio();//随机播放古筝声音
        //AudioController0.instance.ChangeAudioClip(13); //播放随机音效 
        //UIManager.instance.PlayOnClickMusic();

    }

    private void HighlightSelected(GameCard card) =>
        _cardPool.ForEach(c => c.Selected(c.Card.CardId == card.CardId && c.Card.Type == card.Type));
    private void ResetCardPool()
    {
        const int heroType = (int)GameCardType.Hero;
        _cardPool.ForEach(c => c.Off());
        var heroList = PlayerDataForGame.instance.hstData.heroSaveData;
        var cards = heroList.Concat(PlayerDataForGame.instance.hstData.towerSaveData)
            .Concat(PlayerDataForGame.instance.hstData.trapSaveData).Where(c => c.IsOwning())
            .Select(c => new {Card = c, Info = c.GetInfo()}).Where(c => c.Info.ForceId == SelectedForce)
            .Select(c => c.Card)
            .ToList();
        cards.Sort();
        var deputies = cards.GetDeputyIds();
        cards.ForEach(c => InstanceGameCardUi(c, c.Type == heroType && deputies.Contains(c.CardId)));
    }

    private void InstanceGameCardUi(GameCard card, bool isDeputy)
    {
        var ui = GetCardFromPool();
        ui.Init(card);
        ui.Set(GameCardUi.CardModes.Desk);
        var state = 
            card.Level == 0 ? GameCardCityUiOperation.States.Disable :
            isDeputy ? GameCardCityUiOperation.States.Deputy :
            card.IsFight > 0 ? GameCardCityUiOperation.States.Enlisted : GameCardCityUiOperation.States.None;
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
