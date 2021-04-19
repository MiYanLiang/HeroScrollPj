﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//征战页面
public class Expedition : MonoBehaviour
{
    public Text warIntroText; //战役介绍文本obj
    public Image stageTipUi;//模式说明
    public Text stageTipForceName;
    public Button[] difficultyButtons;
    public Button yuanZhengButton;
    public WarStageBtnUi warStageBtnPrefab;
    public ScrollRect warStageScrollRect;
    private const int YuanZhengIndex = 6;//远征id


    public ForceSelectorUi warForceSelectorUi; //战役势力选择器
    private int lastAvailableStageIndex; //最远可战的战役索引
    private List<WarStageBtnUi> stages;
    private int recordedExpeditionWarId = -1;//当前选择战役的WarId
    public int RecordedExpeditionWarId => recordedExpeditionWarId;
    /// <summary>
    /// key = btn index, value = warStageId
    /// </summary>
    private Dictionary<int, GameModeTable> indexWarModeMap; //按键和难度的映射
    private GameModeTable currentMode;//当前选择的难度
    public GameModeTable CurrentMode => currentMode;
    public StaminaCost SelectedWarStaminaCost => currentMode.StaminaCost;

    public void Init()
    {
        warForceSelectorUi.Init(PlayerDataForGame.WarTypes.Expedition);
        indexWarModeMap = new Dictionary<int, GameModeTable>();
        stages = new List<WarStageBtnUi>();
        InitWarModes();
    }

    private void InitWarModes()
    {
        lastAvailableStageIndex = 0;
        var warModes = DataTable.GameMode.Values.Where(m => m.WarList != null).ToList();
        //新手-困难关卡入口初始化
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            var uiBtn = difficultyButtons[i];
            uiBtn.gameObject.SetActive(i < warModes.Count);
            if (!uiBtn.gameObject.activeSelf)
            {
                continue;
            }
            var warMode = warModes[i];
            indexWarModeMap.Add(i, warMode);
            var textUi = uiBtn.GetComponentInChildren<Text>();
            textUi.text = warMode.Title;
            var isUnlock = IsWarUnlock(warMode);
            textUi.color = isUnlock ? Color.white : Color.gray;
            if (isUnlock)
            {
                lastAvailableStageIndex = i;
                var index = i;
                currentMode = warMode;
                uiBtn.onClick.AddListener(() =>
                {
                    InitWarsListInfo(index);
                    UIManager.instance.PlayOnClickMusic();
                });
            }
            else
            {
                uiBtn.onClick.AddListener(() =>
                {
                    PlayerDataForGame.instance.ShowStringTips(warMode.Intro);
                    UIManager.instance.PlayOnClickMusic();
                });
            }
        }

        InitWarsListInfo(lastAvailableStageIndex, true);

        //远征关卡
        
        var yuanZhengMode = DataTable.GameMode[YuanZhengIndex];
        indexWarModeMap.Add(YuanZhengIndex, yuanZhengMode);
        var isYuanZhengUnlock = IsWarUnlock(yuanZhengMode);
        yuanZhengButton.gameObject.SetActive(isYuanZhengUnlock);
        if (isYuanZhengUnlock)
        {
            var textUi = yuanZhengButton.GetComponentInChildren<Text>();
            textUi.text = yuanZhengMode.Title;
            textUi.color = Color.white;
            yuanZhengButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                InitWarsListInfo(YuanZhengIndex);
                UIManager.instance.PlayOnClickMusic();
            });
        }

        bool IsWarUnlock(GameModeTable warMode)
        {
            if (warMode.Unlock == default) return true;//解锁关卡为(id = 0)初始关卡
            var theWarBeforeUnlockWar = warMode.Unlock - 1;//当前需要解锁关卡的前一个关卡id
            var lastWarTotalStages = DataTable.War[theWarBeforeUnlockWar].CheckPoints;//上一个war的总关卡
            return PlayerDataForGame.instance.warsData.warUnlockSaveData
                .Single(w => w.warId == theWarBeforeUnlockWar).unLockCount >= lastWarTotalStages;//是否上一关已经完成
        }
    }

    /// <summary>
    /// 初始化征战关卡(左边)列表
    /// </summary>
    private void InitWarsListInfo(int btnIndex,bool forceRefresh = false)
    {
        //防止跳转界面时切换关卡
        if (UIManager.instance.IsJumping) return;
        //如果点击同个难度模式不需要刷新
        if (!forceRefresh && currentMode == indexWarModeMap[btnIndex]) return;

        currentMode = indexWarModeMap[btnIndex];
        //右侧难度选择按钮刷新大小
        OnSelectDifficultyUiScale(btnIndex);
        var warMode = indexWarModeMap[btnIndex];

        //删除战役列表
        foreach (var ui in stages) Destroy(ui.gameObject);
        stages.Clear();

        var lastStageWarId = -1;
        if (warMode.Id == 6)
        {
            var warId = DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level].YuanZhengWarTableId;
            lastStageWarId = warId;
            InitWarListUi(warId);
        }
        else
        {
            var wars = DataTable.War
                .Where(w => w.Key >= warMode.WarList.Min && w.Key <= warMode.WarList.IncMax)
                .OrderBy(w => w.Key)
                .ToList();

            foreach (var item in wars)
            {
                var war = item.Value;
                InitWarListUi(war.Id);
                lastStageWarId = war.Id;
                var campaign = PlayerDataForGame.instance.warsData.warUnlockSaveData.FirstOrDefault(w => w.warId == war.Id);
                if (campaign == null || campaign.unLockCount < war.CheckPoints) break;
            }
        }


        //默认选择最后一个关卡
        OnClickChangeWarsFun(lastStageWarId);
        //如果UIManager在初始化，代表是场景转跳，不需要动画显示
        warStageScrollRect.DOVerticalNormalizedPos(0f, 0.3f);
    }

    private void InitWarListUi(int warId)
    {
        var checkPoints = DataTable.War[warId].CheckPoints;
        var warStageUi = Instantiate(warStageBtnPrefab, warStageScrollRect.content);
        var campaign = PlayerDataForGame.instance.warsData.warUnlockSaveData.FirstOrDefault(w => w.warId == warId);
        stages.Add(warStageUi);
        warStageUi.SetUi(warId, DataTable.War[warId].Title
                                + "\u2000\u2000\u2000\u2000"
                                + Mathf.Min(campaign?.unLockCount ?? 0, checkPoints)
                                + "/" + checkPoints
            , () => { OnClickChangeWarsFun(warId); UIManager.instance.PlayOnClickMusic(); });
        if (campaign == null) return;
        var isUnlock = campaign.unLockCount >= checkPoints;
        if (!campaign.isTakeReward && isUnlock)
        {
            warStageUi.SetChest(() =>
            {
                GetWarAchievementRewards(warId);
                warStageUi.boxButton.onClick.RemoveAllListeners();
                warStageUi.boxButton.gameObject.SetActive(false);
            });
        }
    }

    private void OnSelectDifficultyUiScale(int index)
    {
        var scaleUp = new Vector2(1.2f, 1.2f);
        yuanZhengButton.transform.localScale = index == YuanZhengIndex ? scaleUp : Vector2.one;
        for (int i = 0; i < difficultyButtons.Length; i++)
            difficultyButtons[i].transform.localScale = index == i ? scaleUp : Vector2.one;
    }

    //选择战役的改变
    public void OnClickChangeWarsFun(int warId)
    {
        for (int i = 0; i < stages.Count; i++)
        {
            var boundWarId = stages[i].boundWarId;
            stages[i].selectedImage.enabled = warId == boundWarId;
        }
        var isLimitedForce = DataTable.War[warId].ForceLimit.Length > 0;
        stageTipUi.gameObject.SetActive(isLimitedForce);
        stageTipForceName.text = string.Empty;
        warForceSelectorUi.Init(PlayerDataForGame.WarTypes.Expedition);

        if (isLimitedForce)
        {
            var forces = DataTable.War[warId].ForceLimit.ToArray();
            foreach (var flagUi in warForceSelectorUi.Data)
            {
                var ui = flagUi.Value;
                var enable = forces.Contains(flagUi.Key);
                ui.Interaction(enable);

                if(!enable) warForceSelectorUi.BtnData[flagUi.Key].onClick.RemoveAllListeners();
            }

            stageTipForceName.text = forces.Length > 0 ? DataTable.War[warId].ForceIntro : string.Empty;
            warForceSelectorUi.OnSelected(PlayerDataForGame.WarTypes.Expedition);
        }

        PlayerDataForGame.instance.zhanYiColdNums = 10;
        PlayerDataForGame.instance.selectedWarId = warId;
        recordedExpeditionWarId = warId;
        //战役介绍
        warIntroText.DOPause();
        warIntroText.text = string.Empty;
        warIntroText.color = new Color(warIntroText.color.r, warIntroText.color.g, warIntroText.color.b, 0);
        warIntroText.DOFade(1, 3f);
        warIntroText.DOText(("\u2000\u2000\u2000\u2000" + DataTable.War[warId].Intro), 3f).SetEase(Ease.Linear)
            .SetAutoKill(false);
    }

    /// <summary> 
    /// 领取战役首通宝箱 
    /// </summary> 
    private void GetWarAchievementRewards(int warId)
    {
        var playerUnlockProgress = PlayerDataForGame.instance.warsData.warUnlockSaveData.Single(w => w.warId == warId);
        if (playerUnlockProgress.isTakeReward) PlayerDataForGame.instance.ShowStringTips("首通宝箱已领取！");
        //var reward = DataTable.War[warId].AchievementReward;
        //if (reward != null)
        //{
        //    if (reward.YuanBao > 0) ConsumeManager.instance.AddYuanBao(reward.YuanBao);
        //    if (reward.YvQue > 0) ConsumeManager.instance.AddYuQue(reward.YvQue);
        //    if (reward.Stamina > 0) UIManager.instance.AddStamina(reward.Stamina);
        //}
        //else reward = new ConsumeResources();

        //var card = DataTable.War[warId].AchievementCardProduce;

        //var cards = new List<RewardsCardClass>();

        //if (card != null)
        //{
        //    UIManager.instance.rewardManager.RewardCard((GameCardType)card.Type, card.CardId, card.Chips);
        //    var rewardCard = new RewardsCardClass
        //    {
        //        cardType = card.Type, cardId = card.CardId, cardChips = card.Chips
        //    };
        //    cards.Add(rewardCard);
        //    PlayerDataForGame.instance.isNeedSaveData = true;
        //    LoadSaveData.instance.SaveGameData(2);
        //}
        ApiPanel.instance.Invoke(vb =>
            {
                var re = vb.GetResourceDto();
                var py = vb.GetPlayerDataDto();
                var reCard = vb.GetGameCardDto();
                if(reCard!=null)
                {
                    List<NowLevelAndHadChip> list = null;
                    switch (reCard.Type)
                    {
                        case GameCardType.Hero:
                            list = PlayerDataForGame.instance.hstData.heroSaveData;
                            break;
                        case GameCardType.Tower:
                            list = PlayerDataForGame.instance.hstData.towerSaveData;
                            break;
                        case GameCardType.Trap:
                            list = PlayerDataForGame.instance.hstData.trapSaveData;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    var card = list.GetOrInstance(reCard.CardId, reCard.Type, reCard.Level);
                    card.chips += reCard.Chips;
                }
                playerUnlockProgress.isTakeReward = true;
                ConsumeManager.instance.SaveChangeUpdatePlayerData(py, 0);
                UIManager.instance.ShowRewardsThings(re.YuanBao, re.YuQue, 0, re.Stamina,
                    reCard == null ? new List<RewardsCardClass>() :
                    new List<RewardsCardClass>
                    {
                        new RewardsCardClass {cardId = reCard.CardId, cardChips = reCard.Chips, cardType = (int) reCard.Type}
                    }, 0);
            }, PlayerDataForGame.instance.ShowStringTips,
            EventStrings.Req_Achievement,
            ViewBag.Instance().SetValue(playerUnlockProgress.warId));
    }


}
