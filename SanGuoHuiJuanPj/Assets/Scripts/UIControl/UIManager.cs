using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System;
using System.Linq;

using Beebyte.Obfuscator;
using CorrelateLib;
using Newtonsoft.Json;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{

    public static UIManager instance;
    //玩家势力 
    public enum Pages
    {
        桃园,
        主城,
        战役,
        对决,
        霸业
    }

    public Pages currentPage;
    public Image waitWhileImpress;//敬请期待 
    [SerializeField]
    PlayerInfoUi playerInfoUi;//玩家信息控件
    [SerializeField]
    public Text yuanBaoNumText;        //元宝数量Text 
    [SerializeField]
    public Text yvQueNumText;          //玉阙数量Text 

    public RewardWindowUi RewardWindow; //奖励窗口
    //[SerializeField]
    //GameObject rewardsShowObj;  //奖品展示UI 
    [SerializeField]
    GameObject[] zhuChengInterFaces;    //主城切换页面0桃园1主城2战役3霸业4对战 
    [SerializeField]
    GameObject[] particlesForInterface;    //主城页面对应粒子效果0桃园1主城2战役 
    [SerializeField]
    GameObject warsChooseListObj;   //战役选择列表obj 
    [SerializeField]
    GameObject warsChooseBtnPreObj;   //战役选择按钮obj 
    [SerializeField]
    GameObject queRenWindows;   //操作确认窗口 

    public Expedition expedition;//战役 
    public TaoYuan taoYuan;//桃园 
    public Barrack Barrack;//主营
    public Versus Versus;//对决

    //[SerializeField]
    //Transform rewardsParent;    //奖品父级 
    //[SerializeField]
    //GameObject rewardObj;       //奖品预制件 

    private int needYuanBaoNums;    //记录所需元宝数 

    public bool IsJumping { get; private set; } //记录界面是否进行跳转 

    [SerializeField]
    Transform chonseWarDifTran; //难度选择父级 

    [SerializeField]
    GameObject[] guideObjs; // 指引objs 0:桃园宝箱 1:战役宝箱 2:合成 3:开始战役 

    //[SerializeField] 
    //RoastedChickenWindow chickenWindow;

    [SerializeField]
    Text chickenCloseText;  //烧鸡关闭时间Text 

    [SerializeField]private BaYeForceSelectorUi baYeForceSelectorUi;//战役势力选择器 
    [SerializeField]private BaYeForcePicController _baYeForcePicController;//霸业军团
    public BaYeProgressUI baYeProgressUi; //霸业经验条 
    public ChestUI[] baYeChestButtons; //霸业宝箱 
    public StoryEventUIController storyEventUiController;//霸业的故事事件控制器 
    public CityStoryEventUIController cityStoryEventUiController;//霸业的故事事件控制器 
    public BaYeCityStoryWindowUi baYeCityStoryWindowUi;//霸业城池事件窗口
    public BaYeWindowUI baYeWindowUi;//霸业地图小弹窗 
    public BaYeTradeLingWindowUi baYeTradeLingWindowUi;
    public Button baYeWarButton;//霸业开始战斗按键 
    public Image bayeBelowLevelPanel;//霸业等级不足挡板 
    public Image bayeErrorPanel;//霸业异常档板
    public PlayerCharacterUi PlayerCharacterUi;
    public ConfirmationWindowUi ConfirmationWindowUi;

    public List<BaYeCityField> CityFields { get; private set; } //霸业的地图物件 
    private List<BaYeForceField> forceFields; //可选势力物件 
    [HideInInspector] public RewardManager rewardManager;
    private GameResources GameResources => GameSystem.GameResources;
    public bool IsInit { get; private set; }

    [SerializeField]
    GameObject InfoWindowObj; //说明窗口 
    [SerializeField]
    Text InfoTitle;
    [SerializeField]
    Text InfoText;

    bool isShowInfo;//说明窗口是否开启 

    [SerializeField]
    GameObject huiJuanWinObj;   //绘卷窗口obj 

    [SerializeField]
    Button jiBanWinCloseBtn;    //羁绊界面关闭按钮 

    [SerializeField]
    public CityStoryEventUIController baYeBattleEventController;   //霸业事件点父级 
    [SerializeField]
    public GameObject chooseBaYeEventImg;  //选择霸业地点的Img 
    [SerializeField]
    Text baYeGoldNumText;   //霸业金币数量 

    [SerializeField] private EventUiController EventUi;
    [SerializeField] private BaYeStoryProgressWindow BaYeStoryProgressPanel;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        IsJumping = false;
        needYuanBaoNums = 0;
        rewardManager = gameObject.AddComponent<RewardManager>();
    }

    public void Init(bool initBaYe)
    {
        if (IsInit) return;
        AudioController1.instance.FadeEndMusic();

        TimeSystemControl.instance.InitStaminaCount(PlayerDataForGame.instance.Stamina.Value <
                                                    TimeSystemControl.instance.MaxStamina);

        //第一次进入主场景的时候初始化霸业管理器 
        if (PlayerDataForGame.instance.BaYeManager == null)
        {
            PlayerDataForGame.instance.BaYeManager = PlayerDataForGame.instance.gameObject.AddComponent<BaYeManager>();
        }

        if(initBaYe) PlayerDataForGame.instance.BaYeManager.InitBaYe();

        taoYuan.Init();
        Barrack.Init(MergeCard,
            OnClickForSellCard,
            OnCardEnlist,
            OnArouseAction,
            OnDeputySubmitAction,
            OnDeputyRecallAction);

        InitializationPlayerInfo();
        expedition.Init();
        Versus.Init(this);
        InitChickenOpenTs();
        //chickenWindow.Init();
        InitBaYeFun();

        OnStartMainScene();
        PlayerDataForGame.instance.selectedWarId = -1;
        PlayerCharacterUi.Init();
        ConfirmationWindowUi.Init();
        baYeCityStoryWindowUi.Init();
        EventUi.Init();
        BaYeStoryProgressPanel.Hide();
        IsInit = true;
    }

    private void OnDeputyRecallAction(GameCard card)
    {
        PlayOnClickMusic();
        if (card == null) return;
        var generalCard = PlayerDataForGame.instance.hstData.heroSaveData
            .FirstOrDefault(c =>
                c.Deputy1Id == card.CardId ||
                c.Deputy2Id == card.CardId ||
                c.Deputy3Id == card.CardId ||
                c.Deputy4Id == card.CardId);
        if (generalCard == null) throw new NotImplementedException("找不到任何卡牌设此卡牌为副将!");
        var slot = 0;
        if (generalCard.Deputy1Id == card.CardId)
        {
            slot = 1;
        }

        if (generalCard.Deputy2Id == card.CardId)
        {
            slot = 2;
        }   

        if (generalCard.Deputy3Id == card.CardId)
        {
            slot = 3;
        }

        if (generalCard.Deputy4Id == card.CardId)
        {
            slot = 4;
        }

        ApiPanel.instance.Call(bag =>
            {
                var dto = bag.Get<GameCardDto>(0);
                var genCard = PlayerDataForGame.instance.hstData.heroSaveData
                    .First(c => c.CardId == dto.CardId && c.Type == (int)dto.Type);
                genCard.CloneFrom(dto);
                Barrack.RefreshCardList();
            }, PlayerDataForGame.instance.ShowStringTips
            , EventStrings.Call_DeputyRemove, generalCard.CardId, generalCard.Type, slot);
    }

    private void OnDeputySubmitAction(GameCard generalCard,int index,int cardId)
    {
        PlayOnClickMusic();
        var slot = index + 1;
        ApiPanel.instance.Call(bag =>
            {
                var dto = bag.Get<GameCardDto>(0);
                var troop = bag.Get<TroopDto>(1);
                var genCard = PlayerDataForGame.instance.hstData.heroSaveData
                    .First(c => c.CardId == dto.CardId && c.Type == (int)dto.Type);
                PlayerDataForGame.instance.UpdateTroopEnlist(troop);
                genCard.CloneFrom(dto);
                generalCard.Clone(genCard);
                Barrack.RefreshCardList();
            }, PlayerDataForGame.instance.ShowStringTips
            , EventStrings.Call_DeputyAdd, generalCard.CardId, generalCard.Type, cardId, generalCard.Type, slot);
    }

    private void OnArouseAction(GameCard card, UnityAction<bool> recallAction)
    {
        PlayOnClickMusic();
        ApiPanel.instance.Call(bag =>
            {
                var dto = bag.Get<GameCardDto>(0);
                var consume = bag.Get<GameCardDto>(1);
                var troop = bag.Get<TroopDto>(2);
                var genCard =
                    PlayerDataForGame.instance.hstData.heroSaveData.First(c =>
                        c.CardId == dto.CardId && c.Type == (int)dto.Type);
                var consumeCard = PlayerDataForGame.instance.hstData.heroSaveData.First(c =>
                    c.CardId == consume.CardId && c.Type == (int)consume.Type);
                genCard.CloneFrom(dto);
                consumeCard.CloneFrom(consume);
                card.Clone(genCard);
                PlayerDataForGame.instance.UpdateTroopEnlist(troop);
                Barrack.RefreshCardList();
                AudioController0.instance.ForcePlayAudio(16);
                recallAction?.Invoke(true);
            }, PlayerDataForGame.instance.ShowStringTips
            , EventStrings.Call_Arouse, card.CardId, card.Type);
    }

    public void UpdateMainSceneUi()
    {
        playerInfoUi.UpdateUi();
    }

    /// <summary> 
    /// Main场景(切换)初始化 
    /// </summary> 
    private void OnStartMainScene()
    {
        switch (PlayerDataForGame.instance.WarType)
        {
            case PlayerDataForGame.WarTypes.None:
            case PlayerDataForGame.WarTypes.Expedition:
                MainPageSwitching(1);
                break;
            case PlayerDataForGame.WarTypes.Baye:
                MainPageSwitching(4);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //PlayerDataForGame.instance.WarType = PlayerDataForGame.WarTypes.None;
        var tips = PlayerDataForGame.instance.mainSceneTips;
        if (!string.IsNullOrWhiteSpace(tips))
        {
            PlayerDataForGame.instance.ShowStringTips(tips);
            PlayerDataForGame.instance.mainSceneTips = string.Empty;
        }
    }
    public void OnConfirmation(UnityAction confirmAction,ConfirmationWindowUi.Resources resource,int cost = 0)
    {
        ConfirmationWindowUi.Show(confirmAction, resource, cost);
    }
    //显示霸业玩法说明 
    public void ShowInfoBaYe()
    {
        string title = DataTable.GetStringText(68);
        string text = DataTable.GetStringText(69);
        ShowInfo(title, text);
    }
    /// <summary>
    /// 显示对决玩法说明
    /// </summary>
    public void ShowInfoDuiJue() 
    {
        string title = DataTable.GetStringText(129);
        string text = DataTable.GetStringText(130);
        ShowInfo(title, text);
    }

    /// <summary> 
    /// 打开说明窗口 
    /// </summary> 
    public void ShowInfo(string infoTitle, string infoText)
    {
        if (!isShowInfo)
        {
            InfoWindowObj.SetActive(true);
            //标题 
            InfoTitle.text = infoTitle;
            //文本 
            InfoText.text = infoText;
        }
        isShowInfo = true;
    }
    /// <summary> 
    /// 关闭说明窗口 
    /// </summary> 
    public void HideInfo()
    {
        if (isShowInfo)
        {
            InfoWindowObj.SetActive(false);
        }
        isShowInfo = false;
    }
    //开始霸业战斗 
    public void StartBaYeFight()
    {
        PlayerDataForGame.instance.WarType = PlayerDataForGame.WarTypes.Baye;
        var selectedForce = PlayerDataForGame.instance.CurrentWarForceId;
        //if (!PlayerDataForGame.instance.ConsumeZhanLing()) return;//消费战令 
        switch (BaYeManager.instance.CurrentEventType)
        {
            case BaYeManager.EventTypes.Story:
                {
                    var map = PlayerDataForGame.instance.baYe.storyMap;
                    if (!map.TryGetValue(BaYeManager.instance.CurrentEventPoint, out var storyEvent))
                    {
                        PlayerDataForGame.instance.ShowStringTips("请选择有效的战斗点。");
                        return;
                    }
                    if (selectedForce < 0) break;
                    BaYeManager.instance.CacheCurrentStoryEvent();
                    PlayerDataForGame.instance.baYe.storyMap.Remove(BaYeManager.instance.CurrentEventPoint);
                    GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
                    StartBattle(storyEvent.WarId);
                    return;
                }
            case BaYeManager.EventTypes.City:
                {
                    var city = BaYeManager.instance.Map.SingleOrDefault(e =>
                        e.CityId == PlayerDataForGame.instance.selectedCity);
                    if (city == null || selectedForce < 0) break;
                    var savedEvent =
                        PlayerDataForGame.instance.baYe.data.SingleOrDefault(e => e.CityId == city.CityId);
                    var passes = 0;
                    if (savedEvent != null)
                        passes = savedEvent.PassedStages.Count(pass => pass);
                    if (passes == city.WarIds.Count)
                    {
                        PlayerDataForGame.instance.ShowStringTips("该地区已经平定了噢。");
                        return;
                    }
                    PlayerDataForGame.instance.SaveBaYeWarEvent(PlayerDataForGame.instance.selectedCity);
                    StartBattle(city.WarIds[passes]);
                    return;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }

        print("请选择");
        //提示选择势力后进行战斗 
        PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(65));

        void StartBattle(int warId)
        {
            if (IsJumping) return;
            IsJumping = true;
            AudioController0.instance.ForcePlayAudio(12);
            AudioController0.instance.ChangeAudioClip(12);
            AudioController0.instance.PlayAudioSource(0);
            PlayerDataForGame.instance.selectedWarId = warId;
            print($"开始战斗 WarId[{warId}]");
            StartCoroutine(LateGoToFightScene());
        }
    }

    //初始化霸业界面内容 
    private void InitBaYeFun()
    {
        try
        {
            var baye = BaYeManager.instance;
            baYeForceSelectorUi.Init(PlayerDataForGame.WarTypes.Baye);
            baYeWarButton.onClick.RemoveAllListeners();
            baYeWarButton.onClick.AddListener(StartBaYeFight);
            storyEventUiController.ResetUi();
            cityStoryEventUiController.ResetUi();
            baYeWindowUi.Init();
            baYeTradeLingWindowUi.Init(baye.OnTradeLing);
            PlayerDataForGame.instance.selectedBaYeEventId = -1;
            PlayerDataForGame.instance.selectedCity = -1;
            //霸业经验条和宝箱初始化 
            ResetBaYeProgressAndGold();
            //城市点初始化 
            var cityLvlMap = new Dictionary<int, int>(); //maxCity, level
            DataTable.PlayerLevelConfig.Values.Select(lvl =>
            {
                var maxCity = lvl.BaYeCityPoints.Max();
                return new {level = lvl.Level, maxCity};
            }).ToList().ForEach(lvl =>
            {
                if (!cityLvlMap.ContainsKey(lvl.maxCity))
                {
                    cityLvlMap.Add(lvl.maxCity, lvl.level);
                    return;
                }

                cityLvlMap[lvl.maxCity] = cityLvlMap[lvl.maxCity] > lvl.level ? cityLvlMap[lvl.maxCity] : lvl.level;
            });

            if (CityFields != null && CityFields.Count > 0)
                CityFields.ForEach(Destroy);
            CityFields = new List<BaYeCityField>();
            var baYe = PlayerDataForGame.instance.baYe;
            for (int cityId = 0; cityId < baYeBattleEventController.eventList.Length; cityId++)
            {
                //得到战役id 
                var eventUi = GetBaYeCityEventUi(cityId);
                var cityField = eventUi.gameObject.AddComponent<BaYeCityField>();
                cityField.id = cityId;
                cityField.button = eventUi.button;
                CityFields.Add(cityField);
                //cityField.IsLevelAvailable = cityList.Length > cityId;
                //if (cityField.IsLevelAvailable)
                //{
                //    InitCityEventUi(cityId);
                //}
                //else
                //{
                //    eventUi.text.text = $"{cityLvlMap[cityId]}级开启";
                //    eventUi.InactiveCityColor();
                //}
                var baYeRecord = baYe.data.SingleOrDefault(f => f.CityId == cityId);
                if (baYeRecord != null) cityField.boundWars = baYeRecord.WarIds;
            }
            UpdateCitiesProgress();
        }
        catch (Exception e)
        {
            bayeErrorPanel.gameObject.SetActive(true);
        }
    }

    private BaYeCityEvent GetBaYeCityEvent(int cityId) => BaYeManager.instance.Map.Single(e => e.CityId == cityId);
    private BaYeCityEventUI GetBaYeCityEventUi(int cityId) => baYeBattleEventController.eventList[cityId];
    public void InitCityEventUi(int cityId, UnityAction onClickAction)
    {
        var eventUi = GetBaYeCityEventUi(cityId);
        var baYeEvent = GetBaYeCityEvent(cityId);
        var city = BaYeManager.instance.Map.Single(c => c.CityId == cityId);
        var flagId = DataTable.BaYeCityEvent[baYeEvent.EventId].FlagId; //旗帜id 
        var flagName = DataTable.BaYeCityEvent[baYeEvent.EventId].FlagText; //旗帜文字 
        eventUi.forceFlag.Set(flagId, true, flagName);

        eventUi.Init(city.WarIds.Count);
        eventUi.button.onClick.RemoveAllListeners();
        eventUi.button.onClick.AddListener(onClickAction);
        eventUi.text.text = DataTable.BaYeCity[cityId].Name; //城市名 
    }

    public void DisableCityEventUi(int cityId)
    {
        var eventUi = GetBaYeCityEventUi(cityId);
        eventUi.Disable();
        eventUi.forceFlag.Hide();
        eventUi.InactiveCityColor();
    }

    private void UpdateCitiesProgress()
    {
        var baYe = PlayerDataForGame.instance.baYe;
        for (int i = 0; i < baYeBattleEventController.eventList.Length; i++)
        {
            var cityPoint = i;
            var ui = baYeBattleEventController.eventList[i];
            var baYeRecord = baYe.data.SingleOrDefault(f => f.CityId == cityPoint);
            if (baYeRecord == null) continue;
            var passCount = baYeRecord.PassedStages.Count(isPass => isPass);
            ui.SetValue(passCount);
            if (baYeRecord.PassedStages.Length == passCount) //如果玩家已经过关 
                ui.forceFlag.Hide();
        }
    }

    public void BaYeProgressUpdate()
    {
        UpdateCitiesProgress();
        ResetBaYeProgressAndGold();
        baYeForceSelectorUi.UpdateZhanLing();
        _baYeForcePicController.UpdateUi();
    }
    public void BaYeForceSelectorInit() => baYeForceSelectorUi.Init(PlayerDataForGame.WarTypes.Baye);
    private void ResetBaYeProgressAndGold()
    {
        var baYe = PlayerDataForGame.instance.baYe;
        var baYeReward = DataTable.BaYeTask.Values
            .Select(task => new { id = task.Id, exp = task.Exp, warChestId = task.WarChestTableId })
            .ToList();
        baYeGoldNumText.text = $"{baYe.gold}/{BaYeManager.instance.BaYeMaxGold}";
        baYeProgressUi.Set(baYe.CurrentExp, baYeReward[^1].exp);
        for (int i = 0; i < baYeReward.Count; i++)
        {
            baYeChestButtons[i].Text.text = baYeReward[i].exp.ToString();
            //如果玩家霸业的经验值大于宝箱经验值并宝箱未被开过 
            if (baYe.CurrentExp < baYeReward[i].exp)
            {
                baYeChestButtons[i].Disabled();
                continue;
            }

            //如果宝箱未被开过 
            if (!baYe.openedChest[i])
                baYeChestButtons[i].Ready();
            else baYeChestButtons[i].Open();
        }
    }

    public void DelayInvokeReturnStaminaUi()
    {
        StopAllCoroutines();
        StartCoroutine(ReturnStaminaFromWar());
    }

    //获取战役返还的体力 
    private IEnumerator ReturnStaminaFromWar()
    {
        yield return new WaitUntil(() => PlayerDataForGame.instance.IsCompleteLoading);
        yield return new WaitForSeconds(1f);
        var staminaCost = PlayerDataForGame.instance.StaminaReturnFromLastWar();
        if (PlayerDataForGame.instance.lastSenceIndex == 2 && staminaCost != 0)
            playerInfoUi.UpdateZhanLing(staminaCost);
        PlayerDataForGame.instance.lastSenceIndex = 1;
    }

    public void GetBaYeProgressReward(int index)
    {
        var baYe = PlayerDataForGame.instance.baYe;
        if (baYe.CurrentExp < DataTable.BaYeTask[index].Exp)
        {
            PlayerDataForGame.instance.ShowStringTips("当前经验不足以领取！");
            return;
        }

        var btnIndex = index - 1;
        var isOpen = baYe.openedChest[btnIndex];
        if (isOpen)
        {
            PlayerDataForGame.instance.ShowStringTips("该奖励已经领取了噢！");
            baYeChestButtons[btnIndex].Open();
            return;
        }

        var warChestId = DataTable.BaYeTask[index].WarChestTableId;
        ApiPanel.instance.InvokeVb(bag => WarChestRecallAction(bag),
            PlayerDataForGame.instance.ShowStringTips,
            EventStrings.Req_WarChest,
            ViewBag.Instance().SetValues(warChestId, 3));
        baYeChestButtons[btnIndex].Open();
        PlayerDataForGame.instance.baYe.openedChest[btnIndex] = true;
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
        PlayerDataForGame.instance.isNeedSaveData = true;
        AudioController0.instance.ForcePlayAudio(0);
    }

    public PlayerDataDto WarChestRecallAction(ViewBag bag)
    {
        var warChest = bag.GetWarChest();
        var player = bag.GetPlayerDataDto();
        var rewards = new List<CardReward>();
        foreach (var chestCard in warChest.Cards)
        {
            var type = (GameCardType) chestCard.Key;
            chestCard.Value.ForEach(c =>
            {
                var card = new CardReward
                {
                    cardId = c[0],
                    cardChips = c[1],
                    cardType = chestCard.Key
                };
                RewardManager.instance.RewardCard(type, card.cardId, card.cardChips); //获取卡牌
                rewards.Add(card);
            });
        }

        ShowRewardsThings(new DeskReward(warChest.YuanBao, warChest.YvQue, warChest.Exp, 0, 0, rewards), 1.5f); //显示奖励窗口
        ConsumeManager.instance.SaveChangeUpdatePlayerData(player, 0);
        return player;
    }

    //刷新体力相关的内容显示 
    public void UpdateShowTiLiInfo(string countText) => playerInfoUi.UpdateZhanLingCountdown(countText);

    /// <summary> 
    /// 开始对战 
    /// </summary> 
    [SkipRename]public void OnClickStartExpedition()
    {
        if (PlayerDataForGame.instance.selectedWarId < 0 || expedition.RecordedExpeditionWarId < 0)
        {
            PlayerDataForGame.instance.ShowStringTips("请选择战役！");
            PlayOnClickMusic();
            return;
        }

        if (expedition.warForceSelectorUi.Data.Values.All(ui => !ui.Selected))
        {
            PlayerDataForGame.instance.ShowStringTips("请选择军团！");
            PlayOnClickMusic();
            return;
        }
        PlayerDataForGame.instance.WarType = PlayerDataForGame.WarTypes.Expedition;
        if (!IsJumping)
        {
            var staminaMap = expedition.SelectedWarStaminaCost;
            int staminaCost = staminaMap.Cost;
            if (PlayerDataForGame.instance.Stamina.Value >= staminaCost)
            {
                if (PlayerDataForGame.instance.TryAddStamina(-staminaCost))
                {

                    ShowOrHideGuideObj(3, false);
                    IsJumping = true;
                    AudioController0.instance.ChangeAudioClip(12);
                    AudioController0.instance.PlayAudioSource(0);
                    playerInfoUi.UpdateZhanLing(-staminaCost);
                    PlayerDataForGame.instance.SetStaminaBeforeWar(staminaMap);
                    StartCoroutine(LateGoToFightScene());
                }
            }
            else
            {
                PlayOnClickMusic();
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(27));
                //Debug.Log("体力不足，无法战斗"); 
            }
        }
        else
        {
            PlayOnClickMusic();
        }
    }

    IEnumerator LateGoToFightScene()
    {
        if (PlayerDataForGame.instance.isJumping) yield break;
        PlayerDataForGame.instance.SendTroopToWarApi();
        yield return new WaitForSeconds(1f);
        PlayerDataForGame.instance.JumpSceneFun(GameSystem.GameScene.WarScene, false, () => PlayerDataForGame.instance.WarReward != null);
    }

    /// <summary> 
    /// 合成卡牌 
    /// </summary> 
    private void MergeCard(GameCard card)
    {
        var nextLevel = DataTable.CardLevel[card.Level + 1];
        var isChipsEnough = card.Chips >= nextLevel.ChipsConsume;
        var isYanBaoEnough = PlayerDataForGame.instance.pyData.YuanBao >= nextLevel.YuanBaoConsume;
        PlayOnClickMusic();

        if (!isChipsEnough || !isYanBaoEnough || !ConsumeManager.instance.DeductYuanBao(nextLevel.YuanBaoConsume))
        {
            PlayerDataForGame.instance.ShowStringTips(!isYanBaoEnough
                ? DataTable.GetStringText(36)
                : DataTable.GetStringText(37));
            return;
        }

        //ApiPanel.instance.CallTest(PlayerDataMock.Username, bag =>
        //    {
        //        var player = bag.Get<PlayerDataDto>(0);
        //        var dto = bag.Get<GameCardDto>(1);
        //        CardMergedUpdate(dto, player);
        //    }, msg => CardMergeErrorMessage(card),
        //    "Call_CardMerge", card.CardId, card.Type);
        ApiPanel.instance.InvokeVb(vb =>
            {
                var player = vb.GetPlayerDataDto();
                var dto = vb.GetGameCardDto();
                CardMergedUpdate(dto, player);
            }, msg => CardMergeErrorMessage(card),
            EventStrings.Req_CardMerge,
            ViewBag.Instance().SetValues(new object[] { card.CardId, card.Type }));

        void CardMergedUpdate(GameCardDto dto, PlayerDataDto player)
        {
            GameCard ca;
            var hst = PlayerDataForGame.instance.hstData;
            switch (dto.Type)
            {
                case GameCardType.Hero:
                {
                    ca = hst.heroSaveData.First(c => c.CardId == dto.CardId);
                    hst.heroSaveData.ForEach(c =>
                    {
                        if (c.Deputy1Id == dto.CardId) c.Deputy1Level = dto.Level;
                        if (c.Deputy2Id == dto.CardId) c.Deputy2Level = dto.Level;
                        if (c.Deputy3Id == dto.CardId) c.Deputy3Level = dto.Level;
                        if (c.Deputy4Id == dto.CardId) c.Deputy4Level = dto.Level;
                    });
                }
                    break;
                case GameCardType.Tower:
                    ca = hst.towerSaveData.First(c => c.CardId == dto.CardId);
                    break;
                case GameCardType.Trap:
                    ca = hst.trapSaveData.First(c => c.CardId == dto.CardId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ca.Chips = dto.Chips;
            ca.Level = dto.Level;
            ConsumeManager.instance.SaveChangeUpdatePlayerData(player, 7);
            Barrack.RefreshCardList();
            Barrack.PointDesk.PlayUpgradeEffect();
            AudioController0.instance.ForcePlayAudio(16);
            //UpdateLevelCardUi();
            ShowOrHideGuideObj(2, false);
        }

        static void CardMergeErrorMessage(GameCard card)
        {
            var message = "失败。";
            if (card.Level == 0)
                message = "合成" + message;
            else message = "升级" + message;
            PlayerDataForGame.instance.ShowStringTips(message);
        }
    }

    private void OnCardEnlist(GameCard card)
    {
        PlayOnClickMusic();
        ApiPanel.instance.InvokeVb(vb =>
            {
                var troop = vb.GetTroopDto();
                PlayerDataForGame.instance.UpdateTroopEnlist(troop);
                Barrack.RefreshCardList();
            }, PlayerDataForGame.instance.ShowStringTips,
            EventStrings.Req_TroopEnlist,
            ViewBag.Instance()
                .SetValues(Barrack.SelectedForce, card.IsFight > 0)
                .GameCardDto(card.ToDto()));
    }


    //出售卡牌 
    private void OnClickForSellCard(GameCard gameCard)
    {
        AudioController0.instance.ChangeAudioClip(18);
        AudioController0.instance.PlayAudioSource(0);
        queRenWindows.transform.GetChild(0).GetChild(1).GetComponent<Button>().onClick.RemoveAllListeners();
        queRenWindows.transform.GetChild(0).GetChild(1).GetComponent<Button>().onClick.AddListener(() =>
        {
            ApiPanel.instance.InvokeVb(vb =>
                {
                    var player = vb.GetPlayerDataDto();
                    var gameCards = vb.GetPlayerGameCardDtos();
                    var troops = vb.GetPlayerTroopDtos();
                    ConsumeManager.instance.SaveUpdatePlayerTroops(player, troops, gameCards);
                    AudioController0.instance.ChangeAudioClip(17);
                    AudioController0.instance.PlayAudioSource(0);
                    RefreshList();
                }, msg =>
                {
                    PlayerDataForGame.instance.ShowStringTips(msg);
                    RefreshList();
                },
                EventStrings.Req_CardSell,
                ViewBag.Instance().SetValues(new object[] {gameCard.CardId, gameCard.Type}));
        });
        queRenWindows.SetActive(true);

        //刷新主城列表 
        void RefreshList()
        {
            gameCard.Chips = 0;
            gameCard.Level = 0;
            gameCard.IsFight = 0;
            PlayerDataForGame.instance.EnlistCard(gameCard, false);
            Barrack.RefreshCardList();
            queRenWindows.SetActive(false);
        }
    }


    // <summary> 
    /// 匹配稀有度边框 
    /// </summary> 
    public void FrameChoose(int rarity, Image img)
    {
        img.enabled = false;//暂时不提供边框 
        return;
        img.enabled = true;
        switch (rarity)
        {
            case 4:
                img.sprite = GameResources.FrameImg[3];
                break;
            case 5:
                img.sprite = GameResources.FrameImg[2];
                break;
            case 6:
                img.sprite = GameResources.FrameImg[1];
                break;
            default:
                img.enabled = false;
                break;
        }
    }

    /// <summary> 
    /// 对HST的数据进行排序 
    /// </summary> 
    private void SortHSTData(List<GameCard> dataList)
    {
        dataList.Sort((c1, c2) =>
        {
            if (c2.IsFight.CompareTo(c1.IsFight) != 0)
            {
                return c2.IsFight.CompareTo(c1.IsFight);
            }

            if (c2.Level.CompareTo(c1.Level) != 0)
            {
                return c2.Level.CompareTo(c1.Level);
            }

            return GetRare(c2).CompareTo(GetRare(c1));
        });

        int GetRare(GameCard c)
        {
            switch ((GameCardType)c.Type)
            {
                case GameCardType.Hero:
                    return DataTable.Hero[c.CardId].Rarity;
                case GameCardType.Tower:
                    return DataTable.Tower[c.CardId].Rarity;
                case GameCardType.Trap:
                    return DataTable.Trap[c.CardId].Rarity;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary> 
    /// 初始化玩家信息显示 
    /// </summary> 
    public void InitializationPlayerInfo()
    {
        playerInfoUi.UpdateUi();
        Barrack.RefreshCardList();
        //StartCoroutine(LateToChangeViewShow(0));
    }

    /// <summary> 
    /// 展示奖励 
    /// </summary>
    /// <param name="reward"></param>
    /// <param name="waitTime">展示等待时间</param> 
    public void ShowRewardsThings(DeskReward reward, float waitTime)
    {
        //刷新主城列表 
        RewardWindow.ShowReward(reward, waitTime, () =>
        {
            Barrack.RefreshCardList();
            taoYuan.CloseAllChests();
        });
    }

    /// <summary> 
    /// 主城界面切换 
    /// </summary> 
    /// <param name="index"></param> 
    public void MainPageSwitching(int index)
    {
        if (IsJumping) return;
        currentPage = (Pages)index;
        PlayOnClickMusic();

        for (int i = 0; i < zhuChengInterFaces.Length; i++)
        {
            zhuChengInterFaces[i].SetActive(false);
            particlesForInterface[i].SetActive(false);
        }
        zhuChengInterFaces[index].SetActive(true);
        particlesForInterface[index].SetActive(true);
        bayeErrorPanel.gameObject.SetActive(false);
        bayeBelowLevelPanel.gameObject.SetActive(false);
        Barrack.gameObject.SetActive(index == 1);
        Versus.Display(index == 3);
        //暂时未开启的页面 
        //waitWhileImpress.gameObject.SetActive(index == 3);//对决
        switch (index)
        {
            case 0://桃园 
                ShowOrHideGuideObj(0, true);
                taoYuan.CloseAllChests();
                if (PlayerDataForGame.instance.gbocData.fightBoxs.Count > 0) ShowOrHideGuideObj(1, true);
                break;
            case 2://战役 
                ShowOrHideGuideObj(3, true);
                expedition.OnClickChangeWarsFun(expedition.RecordedExpeditionWarId);
                warsChooseListObj.transform.parent.parent.GetComponent<ScrollRect>().DOVerticalNormalizedPos(0f, 0.3f);
                break;
            case 4: //霸业
            {
                var isPlayerEnoughLevel = PlayerDataForGame.instance.pyData.Level > 6;
                bayeBelowLevelPanel.gameObject.SetActive(!isPlayerEnoughLevel);
                if (!isPlayerEnoughLevel) break;
                if (!SystemTimer.IsToday(PlayerDataForGame.instance.baYe.lastBaYeActivityTime))
                {
                    BaYeManager.instance.InitBaYe();
                    InitBaYeFun();
                }

                if (BaYeManager.instance.isShowTips)
                {
                    PlayerDataForGame.instance.ShowStringTips(BaYeManager.instance.tipsText);
                    BaYeManager.instance.tipsText = string.Empty;
                    BaYeManager.instance.isShowTips = false;
                }
            }
                break;
            case 1://主城 
                break;
            case 3://对决 
                break;
            default:
                XDebug.LogError<UIManager>($"未知页面索引[{index}]。");
                throw new ArgumentOutOfRangeException();
        }
    }

    //显示或隐藏指引 
    public void ShowOrHideGuideObj(int index, bool isShow)
    {
        if (isShow)
        {
            if (PlayerDataForGame.instance.GuideObjsShowed[index] == 0)
            {
                guideObjs[index].SetActive(true);
            }
        }
        else
        {
            if (PlayerDataForGame.instance.GuideObjsShowed[index] == 0)
            {
                guideObjs[index].SetActive(false);
                PlayerDataForGame.instance.GuideObjsShowed[index] = 1;
                switch (index)
                {
                    case 0:
                        PlayerPrefs.SetInt(StringForGuide.guideJinBaoXiang, 1);
                        break;
                    case 1:
                        PlayerPrefs.SetInt(StringForGuide.guideZYBaoXiang, 1);
                        break;
                    case 2:
                        PlayerPrefs.SetInt(StringForGuide.guideHeCheng, 1);
                        break;
                    case 3:
                        PlayerPrefs.SetInt(StringForGuide.guideStartZY, 1);
                        break;
                    default:
                        break;
                }
            }
        }
    }


    /// <summary> 
    /// 获取玩家经验 
    /// </summary> 
    /// <param name="expNums"></param> 
    public void AddPlayerExp(int expNums)
    {
        if (PlayerDataForGame.instance.pyData.Level >= DataTable.PlayerLevelConfig.Count)
        {
            PlayerDataForGame.instance.pyData.Exp += expNums;
            playerInfoUi.UpdateUi();
            PlayerDataForGame.instance.isNeedSaveData = true;
            LoadSaveData.instance.SaveGameData(1);
            return;
        }

        PlayerDataForGame.instance.pyData.Exp += expNums;
        while (DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level + 1].Exp <= PlayerDataForGame.instance.pyData.Exp)
        {
            PlayerDataForGame.instance.pyData.Exp -= DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level + 1].Exp;
            PlayerDataForGame.instance.pyData.Level++;
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(39));
            if (PlayerDataForGame.instance.pyData.Level == DataTable.PlayerLevelConfig.Keys.Max())
            {
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(40));
                break;
            }
        }
        playerInfoUi.UpdateUi();
        Barrack.RefreshCardList();
        //LoadSaveData.instance.SaveByJson(PlayerDataForGame.instance.pyData); 
        PlayerDataForGame.instance.isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData(1);
    }

    //播放点击音效 
    public void PlayOnClickMusic()
    {
        AudioController0.instance.ChangeAudioClip(13);
        AudioController0.instance.PlayAudioSource(0);
    }

    [SerializeField]
    Text musicBtnText;  //音乐开关文本 

    //打开设置界面 
    public void OpenSettingWinInit()
    {
        PlayOnClickMusic();
        musicBtnText.text = DataTable.GetStringText(GamePref.PrefMusicPlay ? 42 : 41);
    }

    //开关音乐 
    public void OpenOrCloseMusic()
    {
        var musicSwitch = !GamePref.PrefMusicPlay;
        GamePref.SetPrefMusic(musicSwitch);
        AudioController0.instance.MusicSwitch(musicSwitch);
        AudioController1.instance.MusicSwitch(musicSwitch);
        //打开 
        
        if (GamePref.PrefMusicPlay)
        {
            musicBtnText.text = DataTable.GetStringText(42);
            PlayOnClickMusic();
            return;
        }
        musicBtnText.text = DataTable.GetStringText(41);
    }

    //刷新锦囊入口的显示 
    public void UpdateShowJinNangBtn(bool isReady) => taoYuan.jinNangBtn.gameObject.SetActive(isReady);

    [SerializeField]
    Button rtCloseBtn;  //兑换界面关闭按钮 
    [SerializeField]
    InputField rtInputField;  //兑换界面输入控件 
    [SerializeField]
    Button rtconfirmBtn;  //兑换界面确认兑换按钮 

    //兑换礼包方法 
    public void RedemptionCodeFun()
    {
        var code = rtInputField.text;

        if (string.IsNullOrWhiteSpace(code))
        {
            ShowMessage(45);
            return;
        }

        var isRedeemed = PlayerDataForGame.instance.gbocData.redemptionCodeGotList.Contains(code);

        if (isRedeemed)
        {
            ShowMessage(48);
            return;
        }

        ApiPanel.instance.InvokeVb(vb =>
            {
                var rC = vb.GetRCode();
                var py = vb.GetPlayerDataDto();
                var cards = vb.GetPlayerGameCardDtos();
                var troops = vb.GetPlayerTroopDtos();
                OnSuccessRedeemed(rC, py, troops, cards);
            }, PlayerDataForGame.instance.ShowStringTips,
            EventStrings.Req_RCode, ViewBag.Instance().SetValue(code));

        void ShowMessage(int textId)
        {
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(textId));
            rtInputField.text = string.Empty;
            PlayOnClickMusic();
        }
    }

    private void OnSuccessRedeemed(RCodeTable rCode, PlayerDataDto playerData,TroopDto[] troops ,GameCardDto[] cards)
    {
        PlayerDataForGame.instance.UpdateGameCards(troops, cards);
        var rewards = rCode.Cards.Select(c => new CardReward
            {cardId = c.CardId, cardChips = c.Chips, cardType = c.Type}).ToList();
        PlayerDataForGame.instance.gbocData.redemptionCodeGotList.Add(rCode.Code);
        ConsumeManager.instance.SaveChangeUpdatePlayerData(playerData, 0);
        rtInputField.text = "";
        PlayerDataForGame.instance.ShowStringTips(rCode.Info);
        rtCloseBtn.onClick.Invoke();
        AudioController0.instance.ChangeAudioClip(0);
        AudioController0.instance.PlayAudioSource(0);
        ShowRewardsThings(new DeskReward(rCode.YuanBao, rCode.YuQue, 0, rCode.TiLi, rCode.AdPass, rewards), 0);
    }

    ///////////////////////////鸡坛相关///////////////////////////////// 

    //成功获得体力后的方法 
    public void GetCkChangeTimeAndWindow()
    {
        //当前时间点TimeOfDay 
        TimeSpan dspNow = TimeSystemControl.instance.SystemTimer.Now.LocalDateTime.TimeOfDay;
        //TimeSpan dspNow = DateTime.Now.TimeOfDay; 

        //在12点-14点之间 
        if (chickenOpenTs[0][0] < dspNow && dspNow < chickenOpenTs[0][1])
        {
            openCKTime0 = 2;
            PlayerPrefs.SetInt(TimeSystemControl.openCKTime0_str, openCKTime0);
        }
        //在17点-19点之间 
        if (chickenOpenTs[1][0] < dspNow && dspNow < chickenOpenTs[1][1])
        {
            openCKTime1 = 2;
            PlayerPrefs.SetInt(TimeSystemControl.openCKTime1_str, openCKTime1);
        }
        //在21点-23点之间 
        if (chickenOpenTs[2][0] < dspNow && dspNow < chickenOpenTs[2][1])
        {
            openCKTime2 = 2;
            PlayerPrefs.SetInt(TimeSystemControl.openCKTime2_str, openCKTime2);
        }

        //chickenWindow.Off();
    }

    //鸡坛开启时间点 
    string[][] chickenOpenTimeStr = new string[3][] {
        new string[2]{ "12:00", "14:00"},
        new string[2]{ "16:00", "18:00"},   //关闭 
        new string[2]{ "19:00", "21:00"}
    };

    TimeSpan[][] chickenOpenTs = new TimeSpan[3][];

    //初始化鸡坛开启时间 
    private void InitChickenOpenTs()
    {
        for (int i = 0; i < chickenOpenTs.Length; i++)
        {
            TimeSpan[] ts = new TimeSpan[2];
            ts[0] = DateTime.Parse(chickenOpenTimeStr[i][0]).TimeOfDay;
            ts[1] = DateTime.Parse(chickenOpenTimeStr[i][1]).TimeOfDay;
            chickenOpenTs[i] = ts;
        }

        //这是当天首次进游戏 
        if (TimeSystemControl.instance.isFInGame)
        {
            openCKTime0 = 0;
            PlayerPrefs.SetInt(TimeSystemControl.openCKTime0_str, openCKTime0);
            openCKTime1 = 0;
            PlayerPrefs.SetInt(TimeSystemControl.openCKTime1_str, openCKTime1);
            openCKTime2 = 0;
            PlayerPrefs.SetInt(TimeSystemControl.openCKTime2_str, openCKTime2);
        }
        else
        {
            openCKTime0 = PlayerPrefs.GetInt(TimeSystemControl.openCKTime0_str);
            openCKTime1 = PlayerPrefs.GetInt(TimeSystemControl.openCKTime1_str);
            openCKTime2 = PlayerPrefs.GetInt(TimeSystemControl.openCKTime2_str);
        }
    }

    int openCKTime0 = 0;    //0未到时1可开启2已领取 
    int openCKTime1 = 0;
    int openCKTime2 = 0;

    bool isWaitingToExit = false;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) OnExitActionTriggered();
    }

    private void OnExitActionTriggered()
    {
        if (isWaitingToExit)
        {
            PlayOnClickMusic();
            Exit();
        }
        else
        {
            isWaitingToExit = true;
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(52));
            Invoke(nameof(StopWaitingForQuit), 2f);
        }
    }

    public void ShowBaYeStoryProgressPanel(float secs, UnityAction callbackAction) =>
        BaYeStoryProgressPanel.Show(secs, callbackAction);

    //重置退出游戏判断参数 
    private void StopWaitingForQuit() => isWaitingToExit = false;

    public void AccountInfo() => GameSystem.LoginUi.OnAction(LoginUiController.ActionWindows.Info);

    public void ReloadMainScene() => PlayerDataForGame.instance.JumpSceneFun(GameSystem.GameScene.MainScene, false);

    public void Exit()
    {
#if UNITY_ANDROID
            Application.Quit();
#endif
    }

    [Serializable]private class BaYeStoryProgressWindow
    {
        public Slider slider;
        public GameObject Window;

        public void Show(float secs,UnityAction callbackAction)
        {
            slider.value = 0;
            Window.SetActive(true);
            slider.DOValue(1, secs).OnComplete(() =>
            {
                Hide();
                callbackAction?.Invoke();
            });
        }

        public void Hide() => Window.SetActive(false);
    }
}