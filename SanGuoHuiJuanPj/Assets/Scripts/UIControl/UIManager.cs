﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System;
using System.Linq;
using System.Threading;
using Beebyte.Obfuscator;
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
    GameObject zhuChengHeroContentObj;  //主城卡牌集合框 
    [SerializeField]
    GameObject heroCardCityPre; //主城武将卡牌prefab 
    [SerializeField]
    GameObject playerInfoObj;   //玩家信息obj 
    [SerializeField]
    public Text yuanBaoNumText;        //元宝数量Text 
    [SerializeField]
    public Text yvQueNumText;          //玉阙数量Text 
    [SerializeField]
    Text tiLiNumText;           //体力数量Text 
    [SerializeField]
    Text cardsListTitle;         //主城卡牌列表标题 
    [SerializeField]
    Text cardsNumsTitle;         //主城卡牌列表中的单位数量 
    [SerializeField]
    Image changeCardsListBtn;         //主城卡牌列表切换按钮势力图片 
    [SerializeField]
    Image changeCardsListNameImg;     //主城卡牌列表切换按钮势力文字图片 
    [SerializeField]
    GameObject showCardObj;     //上部展示的卡牌 
    [SerializeField]
    Transform infoTran;         //上部展示的信息栏 
    [SerializeField]
    GameObject heChengBtn;      //合成按钮obj 
    [SerializeField]
    GameObject heImgObj;        //合成文字图片 
    [SerializeField]
    GameObject rewardsShowObj;  //奖品展示UI 
    [SerializeField]
    GameObject[] zhuChengInterFaces;    //主城切换页面0桃园1主城2战役3霸业4对战 
    [SerializeField]
    GameObject[] particlesForInterface;    //主城页面对应粒子效果0桃园1主城2战役 
    [SerializeField]
    GameObject warsChooseListObj;   //战役选择列表obj 
    [SerializeField]
    GameObject warsChooseBtnPreObj;   //战役选择按钮obj 

    [SerializeField]
    GameObject holdOrFightBtn;      //出战或回城切换按钮obj 
    [SerializeField]
    GameObject sellCardBtn;      //出售按钮obj 
    [SerializeField]
    Text tiLiRecordTimer;   //体力恢复倒计时 
    [SerializeField]
    GameObject queRenWindows;   //操作确认窗口 
    //[SerializeField] 
    public GameObject[] boxBtnObjs;    //宝箱obj 
    public Expedition expedition;//战役 
    public TaoYuan taoYuan;//桃园 
    public Text JinNangQuota;    //锦囊配额文本 

    [SerializeField]
    Transform rewardsParent;    //奖品父级 
    [SerializeField]
    GameObject rewardObj;       //奖品预制件 

    private int needYuanBaoNums;    //记录所需元宝数 

    Image lastSelectImg;    //对上一个选择的卡牌selectImg的标记 
    NowLevelAndHadChip selectCardData;  //记录选择的卡牌存档数据 

    public bool IsJumping { get; private set; } //记录界面是否进行跳转 

    private int minInitCardCount = 20;  //卡牌池基础数量 
    private List<GameObject> heroCardPoolList = new List<GameObject>();     //卡牌池 

    [SerializeField]
    Transform chonseWarDifTran; //难度选择父级 

    [SerializeField]
    GameObject upStarEffectObj; //升星特效 

    [SerializeField]
    GameObject[] guideObjs; // 指引objs 0:桃园宝箱 1:战役宝箱 2:合成 3:开始战役 

    [SerializeField]
    GameObject chickenEntObj;   //体力入口 
    [SerializeField]
    GameObject chickenShopWindowObj;    //烧鸡商店窗口 
    [SerializeField]
    Button[] chickenShopBtns;   //体力商店购买按钮 
    [SerializeField]
    Text chickenCloseText;  //烧鸡关闭时间Text 

    [SerializeField]
    GameObject cutTiLiTextObj;  //扣除体力动画Obj 

    //public ForceSelectorUi warForceSelectorUi;//战役势力选择器 
    //private int lastAvailableStageIndex;//最远可战的战役索引 
    public BaYeForceSelectorUi baYeForceSelectorUi;//战役势力选择器 
    public BaYeProgressUI baYeProgressUi; //霸业经验条 
    public ChestUI[] baYeChestButtons; //霸业宝箱 
    public StoryEventUIController storyEventUiController;//霸业的故事事件控制器 
    public BaYeWindowUI baYeWindowUi;//霸业地图小弹窗 
    public Button baYeWarButton;//霸业开始战斗按键 
    public Image bayeBelowLevelPanel;//霸业等级不足挡板 

    private List<BaYeCityField> cityFields; //霸业的地图物件 
    private List<BaYeForceField> forceFields; //可选势力物件 
    [HideInInspector] public RewardManager rewardManager;
    private GameResources GameResources => PlayerDataForGame.instance.gameResources;

    [SerializeField]
    GameObject InfoWindowObj; //说明窗口 
    [SerializeField]
    Text InfoTitle;
    [SerializeField]
    Text InfoText;

    bool isShowInfo;//说明窗口是否开启 


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        IsJumping = false;
        needYuanBaoNums = 0;
        indexChooseListForceId = 0;
        selectCardData = new NowLevelAndHadChip();
        rewardManager = gameObject.AddComponent<RewardManager>();
        ItemsRedemptionFunc();
    }

    // Start is called before the first frame update 
    void Start()
    {
        //版本修正 
        BugHotFix.OnFixYvQueV1_90();
        BugHotFix.OnFixLianYuV2_02();
        TimeSystemControl.instance.InitStaminaCount(PlayerDataForGame.instance.pyData.Stamina <
                                                    TimeSystemControl.instance.MaxStamina);

        //第一次进入主场景的时候初始化霸业管理器 
        if (PlayerDataForGame.instance.baYeManager == null)
        {
            PlayerDataForGame.instance.baYeManager = PlayerDataForGame.instance.gameObject.AddComponent<BaYeManager>();
            PlayerDataForGame.instance.baYeManager.Init();
        }

        InitHeroCardPooling();

        InitializationPlayerInfo();
        expedition.Init();

        InitChickenOpenTs();
        InitChickenBtnFun();
        InitJiBanForMainFun();
        InitBaYeFun();
        PlayerDataForGame.instance.ClearGarbageStationObj();

        OnStartMainScene();
        PlayerDataForGame.instance.selectedWarId = -1;
    }

    //时间管理 
    private void OnEnable()
    {
        AudioController1.instance.ChangeBackMusic();
        TimeSystemControl.instance.isOpenMainScene = true;

        Invoke("GetBackTiLiForFight", 2f);
    }
    private void OnDisable()
    {
        TimeSystemControl.instance.isOpenMainScene = false;
    }

    [SerializeField]
    GameObject huiJuanWinObj;   //绘卷窗口obj 

    [SerializeField]
    GameObject jiBanBtnsConObj;  //羁绊按钮集合窗口obj 

    [SerializeField]
    GameObject jiBanInfoConObj; //羁绊详情窗口obj 

    [SerializeField]
    Transform jibanBtnBoxTran;  //羁绊按钮集合 

    [SerializeField]
    Transform jibanHeroBoxTran; //羁绊详情武将集合 

    [SerializeField]
    Button jiBanWinCloseBtn;    //羁绊界面关闭按钮 

    [SerializeField]
    public BaYeEventUIController baYeBattleEventController;   //霸业事件点父级 
    [SerializeField]
    public GameObject chooseBaYeEventImg;  //选择霸业地点的Img 
    [SerializeField]
    Text baYeGoldNumText;   //霸业金币数量 

    /// <summary> 
    /// 游戏物品获取次数计算函数 
    /// </summary> 
    public void ItemsRedemptionFunc()
    {
        if (!SystemTimer.IsToday(PlayerDataForGame.instance.pyData.LastJinNangRedeemTime))
            PlayerDataForGame.instance.SetRedeemCount(PlayerDataForGame.RedeemTypes.JinNang, 0);
        if (!SystemTimer.IsToday(PlayerDataForGame.instance.pyData.LastJiuTanRedeemTime))
            PlayerDataForGame.instance.SetRedeemCount(PlayerDataForGame.RedeemTypes.JiuTan, 0);
        //根据系统时间计算本地的天数是否是同一天 
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

        PlayerDataForGame.instance.WarType = PlayerDataForGame.WarTypes.None;
        var tips = PlayerDataForGame.instance.mainSceneTips;
        if (!string.IsNullOrWhiteSpace(tips))
        {
            PlayerDataForGame.instance.ShowStringTips(tips);
            PlayerDataForGame.instance.mainSceneTips = string.Empty;
        }
    }
    //显示霸业玩法说明 
    public void ShowInfoBaYe()
    {
        string title = DataTable.GetStringText(68);
        string text = DataTable.GetStringText(69);
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
        switch (BaYeManager.instance.CurrentEventType)
        {
            case BaYeManager.EventTypes.Story:
                {
                    var map = PlayerDataForGame.instance.warsData.baYe.storyMap;
                    if (!map.TryGetValue(BaYeManager.instance.CurrentEventPoint, out var storyEvent))
                    {
                        PlayerDataForGame.instance.ShowStringTips("请选择有效的战斗点。");
                        return;
                    }
                    if (selectedForce < 0) break;
                    if (!PlayerDataForGame.instance.ConsumeZhanLing()) return;//消费战令 
                    BaYeManager.instance.CacheCurrentStoryEvent();
                    PlayerDataForGame.instance.warsData.baYe.storyMap.Remove(BaYeManager.instance.CurrentEventPoint);
                    PlayerDataForGame.instance.isNeedSaveData = true;
                    LoadSaveData.instance.SaveGameData(3);
                    StartBattle(storyEvent.WarId);
                    return;
                }
            case BaYeManager.EventTypes.City:
                {
                    var city = BaYeManager.instance.Map.SingleOrDefault(e =>
                        e.CityId == PlayerDataForGame.instance.selectedCity);
                    if (city == null || selectedForce < 0) break;
                    var savedEvent =
                        PlayerDataForGame.instance.warsData.baYe.data.SingleOrDefault(e => e.CityId == city.CityId);
                    var passes = 0;
                    if (savedEvent != null)
                        passes = savedEvent.PassedStages.Count(pass => pass);
                    if (passes == city.WarIds.Count)
                    {
                        PlayerDataForGame.instance.ShowStringTips("该地区已经平定了噢。");
                        return;
                    }
                    if (!PlayerDataForGame.instance.ConsumeZhanLing()) return;//消费战令 
                    PlayerDataForGame.instance.SaveBaYeWarEvent();
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
        baYeForceSelectorUi.Init(PlayerDataForGame.WarTypes.Baye);
        baYeWarButton.onClick.RemoveAllListeners();
        baYeWarButton.onClick.AddListener(StartBaYeFight);
        storyEventUiController.ResetUi();
        baYeWindowUi.Init();
        var baYe = PlayerDataForGame.instance.warsData.baYe;
        PlayerDataForGame.instance.selectedBaYeEventId = -1;
        PlayerDataForGame.instance.selectedCity = -1;
        //霸业经验条和宝箱初始化 
        ResetBaYeProgressAndGold();
        //城市点初始化 
        var cityLvlMap = new Dictionary<int,int>();//maxCity, level
        DataTable.PlayerLevel.Select(map =>
        {
            var maxCity = map.Value[7].TableStringToInts().Max();
            return new {level = map.Key, maxCity};
        }).ToList().ForEach(map =>
        {
            if (!cityLvlMap.ContainsKey(map.maxCity))
            {
                cityLvlMap.Add(map.maxCity, map.level);
                return;
            }

            cityLvlMap[map.maxCity] = cityLvlMap[map.maxCity] > map.level ? cityLvlMap[map.maxCity] : map.level;
        });
        var cityList = DataTable.PlayerLevel[PlayerDataForGame.instance.pyData.Level][7].TableStringToInts().ToArray();
        if (cityFields != null && cityFields.Count > 0)
            cityFields.ForEach(Destroy);
        cityFields = new List<BaYeCityField>();
        for (int i = 0; i < baYeBattleEventController.eventList.Length; i++)
        {
            int cityPoint = i;
            //得到战役id 
            var ui = baYeBattleEventController.eventList[i];
            var cityField = ui.gameObject.AddComponent<BaYeCityField>();
            cityField.id = cityPoint;
            cityField.button = ui.button;
            cityFields.Add(cityField);
            var baYeEvent = BaYeManager.instance.Map.Single(e => e.CityId == cityPoint);
            var baYeRecord = baYe.data.SingleOrDefault(f => f.CityId == cityPoint);
            var flag = (ForceFlags)int.Parse(DataTable.BaYeShiJian[baYeEvent.EventId][4]);//旗帜id 
            var flagName = DataTable.BaYeShiJian[baYeEvent.EventId][5];//旗帜文字 
            ui.button.interactable = cityList.Length > i;
            ui.forceFlag.Set(flag, true, flagName);
            if (cityList.Length > i)
            {
                var city = BaYeManager.instance.Map.Single(c => c.CityId == i);
                ui.Init(city.WarIds.Count);
                ui.button.onClick.RemoveAllListeners();
                ui.button.onClick
                    .AddListener(
                        () => BaYeManager.instance.OnBaYeWarEventPointSelected(BaYeManager.EventTypes.City, baYeEvent.CityId));
                ui.text.text = DataTable.BaYeDiTu[cityPoint][3]; //城市名 
            }
            else
            {
                ui.text.text = $"{cityLvlMap[i]}级开启";
                ui.InactiveCityColor();
            }

            if (baYeRecord == null) continue;
            cityField.boundWars = baYeRecord.WarIds;
            var passCount = baYeRecord.PassedStages.Count(isPass => isPass);
            ui.SetValue(passCount);
            if (baYeRecord.PassedStages.Length == passCount)//如果玩家已经过关 
                ui.forceFlag.Hide();
        }
    }

    public void ResetBaYeProgressAndGold()
    {
        var baYe = PlayerDataForGame.instance.warsData.baYe;
        var baYeReward = DataTable.BaYeRenWu
            .Select(map => new { id = map.Key, exp = int.Parse(map.Value[1]), rewardId = int.Parse(map.Value[2]) })
            .ToList();
        baYeGoldNumText.text = $"{baYe.gold}/{BaYeManager.instance.BaYeMaxGold}";
        baYeProgressUi.Set(baYe.CurrentExp, baYeReward[baYeReward.Count - 1].exp);
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
            else baYeChestButtons[i].Opened();
        }
    }

    //main场景羁绊内容的初始化 
    private void InitJiBanForMainFun()
    {
        foreach (var map in DataTable.JiBan)
        {
            var enableValue = int.Parse(map.Value[2]);
            if (enableValue == 0) continue;
            Transform tran = jibanBtnBoxTran.GetChild(map.Key);
            if (tran != null)
            {
                tran.GetChild(0).GetChild(0).GetComponent<Image>().sprite =
                    Resources.Load("Image/JiBan/name_v/" + map.Key, typeof(Sprite)) as Sprite;
                tran.GetChild(0).GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
                    ShowJiBanInfoOnClick(map.Key));
                tran.gameObject.SetActive(true);
            }
        }
        jiBanWinCloseBtn.onClick.AddListener(CloseHuiJuanWinObjFun);
    }

    //点击单个羁绊按钮展示详细信息 
    private void ShowJiBanInfoOnClick(int indexId)
    {
        for (int i = 0; i < jibanHeroBoxTran.childCount; i++)
        {
            jibanHeroBoxTran.transform.GetChild(i).gameObject.SetActive(false);
        }

        string[] arrs = DataTable.JiBan[indexId][3].Split(';');
        for (int i = 0; i < arrs.Length; i++)
        {
            if (arrs[i] != "")
            {
                string[] arr = arrs[i].Split(',');
                if (arr[0] == "0")
                {
                    int heroId = int.Parse(arr[1]);
                    Transform tran = jibanHeroBoxTran.GetChild(i);
                    GameObject obj = tran.GetChild(0).gameObject;
                    //名字 
                    ShowNameTextRules(obj.transform.GetChild(2).GetComponent<Text>(), DataTable.Hero[heroId][1]);
                    //名字颜色根据稀有度 
                    obj.transform.GetChild(2).GetComponent<Text>().color = NameColorChoose(DataTable.Hero[heroId][3]);
                    //卡牌 
                    obj.transform.GetChild(1).GetComponent<Image>().sprite =
                        GameResources.HeroImg[heroId];
                    //兵种名 
                    obj.transform.GetChild(4).GetComponentInChildren<Text>().text = DataTable.ClassData[int.Parse(DataTable.Hero[heroId][5])][3];
                    //兵种框 
                    obj.transform.GetChild(4).GetComponent<Image>().sprite = GameResources.ClassImg[0];
                    tran.gameObject.SetActive(true);
                }
            }
        }
        jiBanInfoConObj.transform.GetChild(0).GetComponent<Image>().sprite = Resources.Load("Image/JiBan/art/" + indexId, typeof(Sprite)) as Sprite;
        jiBanInfoConObj.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = DataTable.JiBanData[indexId][4];
        jiBanInfoConObj.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite = Resources.Load("Image/JiBan/name_h/" + indexId, typeof(Sprite)) as Sprite;


        jiBanBtnsConObj.SetActive(false);
        jiBanInfoConObj.SetActive(true);
        jiBanWinCloseBtn.onClick.RemoveAllListeners();
        jiBanWinCloseBtn.onClick.AddListener(delegate ()
        {
            jiBanInfoConObj.SetActive(false);
            jiBanBtnsConObj.SetActive(true);
            jiBanWinCloseBtn.onClick.RemoveAllListeners();
            jiBanWinCloseBtn.onClick.AddListener(CloseHuiJuanWinObjFun);
        });
    }

    /// <summary> 
    /// 打开绘卷界面 
    /// </summary> 
    public void OpenHuiJuanWinObjFun()
    {
        jiBanBtnsConObj.SetActive(true);
        huiJuanWinObj.SetActive(true);
    }

    /// <summary> 
    /// 关闭绘卷界面 
    /// </summary> 
    private void CloseHuiJuanWinObjFun()
    {
        huiJuanWinObj.SetActive(false);
        jiBanBtnsConObj.SetActive(false);
        jiBanInfoConObj.SetActive(false);
    }

    //获取战役返还的体力 
    private void GetBackTiLiForFight()
    {
        if (PlayerDataForGame.instance.lastSenceIndex == 2 && PlayerDataForGame.instance.getBackTiLiNums > 0)
        {
            cutTiLiTextObj.SetActive(false);
            cutTiLiTextObj.GetComponent<Text>().color = ColorDataStatic.deep_green;
            cutTiLiTextObj.GetComponent<Text>().text = "+" + PlayerDataForGame.instance.getBackTiLiNums;
            cutTiLiTextObj.SetActive(true);
            AddTiLiNums(PlayerDataForGame.instance.getBackTiLiNums);
            PlayerDataForGame.instance.ShowStringTips(string.Format(DataTable.GetStringText(25), PlayerDataForGame.instance.getBackTiLiNums));
        }
        PlayerDataForGame.instance.lastSenceIndex = 1;
        PlayerDataForGame.instance.getBackTiLiNums = 0;
    }

    public void GetBaYeProgressReward(int index)
    {
        var baYe = PlayerDataForGame.instance.warsData.baYe;
        var baYeRewardList = DataTable.BaYeRenWuData
            .Select(row =>
                new { exp = int.Parse(row[1]), rewardId = int.Parse(row[2]) })
            .ToList();
        if (baYe.CurrentExp < baYeRewardList[index].exp)
        {
            PlayerDataForGame.instance.ShowStringTips("当前经验不足以领取！");
            return;
        }
        baYeChestButtons[index].Opened();
        var data = DataTable.BaYeRenWu[index].Select(int.Parse).ToList();
        var isOpen = baYe.openedChest[index];
        if (isOpen)
        {
            PlayerDataForGame.instance.ShowStringTips("该奖励已经领取了噢！");
            return;
        }
        var rewardId = data[2];
        var chestData = DataTable.WarChestData[rewardId];
        var exp = int.Parse(chestData[3]);
        var yvQue = RewardManager.instance.GetYvQue(rewardId);
        var yuanBao = RewardManager.instance.GetYuanBao(rewardId);
        var cards = RewardManager.instance.GetCards(rewardId, false);
        ConsumeManager.instance.AddYuQue(yvQue);
        ConsumeManager.instance.AddYuanBao(yuanBao);
        PlayerDataForGame.instance.warsData.baYe.openedChest[index] = true;
        PlayerDataForGame.instance.isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData(3);
        ShowRewardsThings(yuanBao, yvQue, exp, 0, cards, 0.5f);
        AudioController0.instance.ForcePlayAudio(0);
    }

    /// <summary> 
    /// 领取战役首通宝箱 
    /// </summary> 
    public void GetWarFirstRewards(int warId)
    {
        var playerUnlockProgress = PlayerDataForGame.instance.warsData.warUnlockSaveData.Single(w => w.warId == warId);
        if (playerUnlockProgress.isTakeReward) PlayerDataForGame.instance.ShowStringTips("首通宝箱已领取！");
        int yuanBaoNums = int.Parse(DataTable.WarData[warId][8]);
        int yuQueNums = int.Parse(DataTable.WarData[warId][9]);
        int tiLiNums = int.Parse(DataTable.WarData[warId][10]);
        if (yuanBaoNums > 0)
        {
            ConsumeManager.instance.AddYuanBao(yuanBaoNums);
        }
        if (yuQueNums > 0)
        {
            ConsumeManager.instance.AddYuQue(yuQueNums);
        }
        if (tiLiNums > 0)
        {
            AddTiLiNums(tiLiNums);
        }

        string rewardsStr = DataTable.WarData[warId][PlayerDataForGame.instance.pyData.ForceId + 5];

        List<RewardsCardClass> rewards = new List<RewardsCardClass>();

        if (rewardsStr != "")
        {
            string[] arrs = rewardsStr.Split(',');
            int cardType = int.Parse(arrs[0]);
            int cardId = int.Parse(arrs[1]);
            int cardChips = int.Parse(arrs[2]);
            rewardManager.RewardCard((GameCardType)cardType, cardId, cardChips);
            RewardsCardClass rewardCard = new RewardsCardClass();
            rewardCard.cardType = cardType;
            rewardCard.cardId = cardId;
            rewardCard.cardChips = cardChips;
            rewards.Add(rewardCard);
            PlayerDataForGame.instance.isNeedSaveData = true;
            LoadSaveData.instance.SaveGameData(2);
        }

        playerUnlockProgress.isTakeReward = true;
        PlayerDataForGame.instance.isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData(3);

        ShowRewardsThings(yuanBaoNums, yuQueNums, 0, tiLiNums, rewards, 0);
    }

    int showTiLiNums = 0;

    //刷新体力相关的内容显示 
    public void UpdateShowTiLiInfo(string recordStr)
    {
        if (recordStr != tiLiRecordTimer.text)
        {
            tiLiRecordTimer.text = recordStr;
        }

        int nowStaminaNums = PlayerDataForGame.instance.pyData.Stamina;
        if (showTiLiNums != nowStaminaNums)
        {
            showTiLiNums = nowStaminaNums;
            tiLiNumText.text = nowStaminaNums + "/90";
        }
    }

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
            int staminaCost = staminaMap[0];
            if (PlayerDataForGame.instance.pyData.Stamina >= staminaCost)
            {
                ShowOrHideGuideObj(3, false);
                IsJumping = true;
                AudioController0.instance.ChangeAudioClip(12);
                AudioController0.instance.PlayAudioSource(0);
                TimeSystemControl.instance.LetTiLiTimerTake(staminaCost);
                PlayerDataForGame.instance.AddStamina(-staminaCost);
                showTiLiNums = PlayerDataForGame.instance.pyData.Stamina;
                tiLiNumText.text = showTiLiNums + "/90";
                cutTiLiTextObj.SetActive(false);
                cutTiLiTextObj.GetComponent<Text>().color = ColorDataStatic.name_red;
                cutTiLiTextObj.GetComponent<Text>().text = "-" + staminaCost;
                cutTiLiTextObj.SetActive(true);

                PlayerDataForGame.instance.getBackTiLiNums = staminaMap[1];
                PlayerDataForGame.instance.boxForTiLiNums = staminaMap[2];

                StartCoroutine(LateGoToFightScene());
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
        yield return new WaitForSeconds(1f);
        if (!PlayerDataForGame.instance.isJumping)
        {
            PlayerDataForGame.instance.JumpSceneFun(2, false);
        }
    }

    //刷新上阵数量的显示 
    private void UpdateCardNumsShow()
    {
        cardsListTitle.text = "出战";
        cardsNumsTitle.text = PlayerDataForGame.instance.CalculationFightCount() + "/" + DataTable.PlayerLevelData[PlayerDataForGame.instance.pyData.Level - 1][2];
    }

    //是否展示卡牌详情显示 
    private void ShowOrHideInfo(bool isShow)
    {
        showCardObj.SetActive(isShow);
        infoTran.gameObject.SetActive(isShow);
        heChengBtn.SetActive(isShow);
        holdOrFightBtn.SetActive(isShow);
        sellCardBtn.SetActive(isShow);
    }

    public int indexChooseListForceId = 0; //标记主城展示哪个势力的id 

    /// <summary> 
    /// 改变武将列表和辅助列表显示 
    /// </summary> 
    public void ChangeScrollView()
    {
        AudioController0.instance.RandomPlayGuZhengAudio();//播放随机音效 

        indexChooseListForceId++;
        if (indexChooseListForceId > int.Parse(DataTable.PlayerLevelData[PlayerDataForGame.instance.pyData.Level - 1][6]))
        {
            indexChooseListForceId = 0;
        }
        changeCardsListBtn.sprite = Resources.Load("Image/shiLi/Flag/" + indexChooseListForceId, typeof(Sprite)) as Sprite;
        changeCardsListNameImg.sprite = Resources.Load("Image/shiLi/Name/" + indexChooseListForceId, typeof(Sprite)) as Sprite;
        CreateHeroAndTowerContent();
        UpdateCardNumsShow();
        StartCoroutine(LateToChangeViewShow(0));
    }

    /// <summary> 
    /// 延时刷新列表置顶 
    /// </summary> 
    IEnumerator LateToChangeViewShow(float startTime)
    {
        yield return new WaitForSeconds(startTime);

        //Debug.Log("----列表大小控制"); 

        int showCardCount = 0;
        for (int i = 0; i < zhuChengHeroContentObj.transform.childCount; i++)
        {
            if (zhuChengHeroContentObj.transform.GetChild(i).gameObject.activeSelf)
                showCardCount++;
        }

        //列表大小控制 
        if (showCardCount >= 16)
        {
            zhuChengHeroContentObj.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;
        }
        else
        {
            zhuChengHeroContentObj.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            //zhuChengHeroContentObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0); 
            //zhuChengHeroContentObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1); 
            zhuChengHeroContentObj.GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
            zhuChengHeroContentObj.GetComponent<RectTransform>().offsetMax = new Vector2(1, 1);
        }
        zhuChengHeroContentObj.transform.parent.parent.GetComponent<ScrollRect>().DOVerticalNormalizedPos(1f, 0.2f);
        //zhuChengHeroContentObj.transform.parent.parent.GetComponent<ScrollRect>().listView.localPosition = Vector2.left; 
    }

    /// <summary> 
    /// 显示单个辅助 
    /// </summary> 
    private void ShowOneFuZhuRules(IReadOnlyDictionary<int, IReadOnlyList<string>> data, NowLevelAndHadChip card, int indexIcon)
    {
        GameObject obj = GetHeroCardFromPool();
        //名字 
        ShowNameTextRules(obj.transform.GetChild(3).GetComponent<Text>(), data[card.id][1]);
        //名字颜色根据稀有度 
        obj.transform.GetChild(3).GetComponent<Text>().color = NameColorChoose(data[card.id][3]);
        //卡牌 
        obj.transform.GetChild(1).GetComponent<Image>().sprite = GameResources.FuZhuImg[int.Parse(data[card.id][indexIcon])];
        //兵种框 
        obj.transform.GetChild(5).GetComponent<Image>().sprite = GameResources.ClassImg[1];
        //兵种名 
        obj.transform.GetChild(5).GetComponentInChildren<Text>().text = data[card.id][5];
        //边框 
        FrameChoose(data[card.id][3], obj.transform.GetChild(6).GetComponent<Image>());
        //碎片 
        if (card.level < DataTable.UpGradeData.Count)
        {
            obj.transform.GetChild(2).GetComponent<Text>().text = card.chips + "/" + DataTable.UpGradeData[card.level][1];
            obj.transform.GetChild(2).GetComponent<Text>().color = card.chips >= int.Parse(DataTable.UpGradeData[card.level][1]) ? ColorDataStatic.deep_green : Color.white;

        }
        else
        {
            obj.transform.GetChild(2).GetComponent<Text>().text = "";
        }
        if (card.level > 0)
        {
            obj.transform.GetChild(4).GetComponent<Image>().enabled = true;
            //设置星级展示 
            obj.transform.GetChild(4).GetComponent<Image>().sprite = GameResources.GradeImg[card.level];
            obj.transform.GetChild(8).gameObject.SetActive(false);
            //出战标记 
            if (card.isFight > 0)
            {
                PlayerDataForGame.instance.AddOrCutFightCardId(card.typeIndex, card.id, true);
                obj.transform.GetChild(7).gameObject.SetActive(true);
            }
            else
            {
                obj.transform.GetChild(7).gameObject.SetActive(false);
            }
        }
        else
        {
            obj.transform.GetChild(4).GetComponent<Image>().enabled = false;
            obj.transform.GetChild(7).gameObject.SetActive(false);
            obj.transform.GetChild(8).gameObject.SetActive(true);
        }
        obj.GetComponent<Button>().onClick.RemoveAllListeners();
        obj.GetComponent<Button>().onClick.AddListener(delegate ()
        {
            OnClickFuZhuCardFun(data, card, obj.transform.GetChild(9).GetComponent<Image>(), indexIcon);
        });
    }

    /// <summary> 
    /// 点击辅助卡牌的方法 
    /// </summary> 
    /// <param name="fuzhuData"></param> 
    private void OnClickFuZhuCardFun(IReadOnlyDictionary<int, IReadOnlyList<string>> data, NowLevelAndHadChip fuzhuData, Image selectImg, int indexIcon)
    {
        PlayOnClickMusic();

        //名字 
        infoTran.GetChild(0).GetComponent<Text>().text = data[fuzhuData.id][1];
        //名字颜色 
        infoTran.GetChild(0).GetComponent<Text>().color = NameColorChoose(data[fuzhuData.id][3]);
        //属性 为空 
        infoTran.GetChild(1).GetComponent<Text>().text = "";
        infoTran.GetChild(2).GetComponent<Text>().text = "";
        //介绍 
        infoTran.GetChild(3).GetComponent<Text>().text = data[fuzhuData.id][2];
        //名字 
        ShowNameTextRules(showCardObj.transform.GetChild(3).GetComponent<Text>(), data[fuzhuData.id][1]);
        //名字颜色 
        showCardObj.transform.GetChild(3).GetComponent<Text>().color = NameColorChoose(data[fuzhuData.id][3]);
        //卡牌 
        showCardObj.transform.GetChild(1).GetComponent<Image>().sprite = GameResources.FuZhuImg[int.Parse(data[fuzhuData.id][indexIcon])];
        //兵种框 
        showCardObj.transform.GetChild(5).GetComponent<Image>().sprite = GameResources.ClassImg[1];
        //兵种名 
        showCardObj.transform.GetChild(5).GetComponentInChildren<Text>().text = data[fuzhuData.id][5];
        //边框 
        FrameChoose(data[fuzhuData.id][3], showCardObj.transform.GetChild(6).GetComponent<Image>());
        //碎片 
        if (fuzhuData.level < DataTable.UpGradeData.Count)
        {
            showCardObj.transform.GetChild(2).GetComponent<Text>().text = fuzhuData.chips + "/" + DataTable.UpGradeData[fuzhuData.level][1];
            showCardObj.transform.GetChild(2).GetComponent<Text>().color = fuzhuData.chips >= int.Parse(DataTable.UpGradeData[fuzhuData.level][1]) ? ColorDataStatic.deep_green : Color.black;
        }
        else
        {
            showCardObj.transform.GetChild(2).GetComponent<Text>().text = "";
        }

        int getGoldNums = GetGoldNumsForSellCard(fuzhuData);
        sellCardBtn.transform.GetChild(0).GetComponent<Text>().text = getGoldNums.ToString();
        sellCardBtn.GetComponent<Button>().onClick.RemoveAllListeners();
        sellCardBtn.GetComponent<Button>().onClick.AddListener(delegate ()
        {
            OnClickForSellCard(fuzhuData, getGoldNums);
        });
        sellCardBtn.SetActive(true);

        if (fuzhuData.level > 0)
        {
            showCardObj.transform.GetChild(4).GetComponent<Image>().enabled = true;
            //设置星级展示 
            showCardObj.transform.GetChild(4).GetComponent<Image>().sprite = GameResources.GradeImg[fuzhuData.level];
            //出战相关设置 
            holdOrFightBtn.SetActive(true);
            if (fuzhuData.isFight > 0)
            {
                showCardObj.transform.GetChild(7).gameObject.SetActive(true);
                holdOrFightBtn.GetComponentInChildren<Text>().text = DataTable.GetStringText(30);
            }
            else
            {
                showCardObj.transform.GetChild(7).gameObject.SetActive(false);
                holdOrFightBtn.GetComponentInChildren<Text>().text = DataTable.GetStringText(31);
            }
        }
        else
        {
            holdOrFightBtn.SetActive(false);
            showCardObj.transform.GetChild(7).gameObject.SetActive(false);
            showCardObj.transform.GetChild(4).GetComponent<Image>().enabled = false;
        }
        //选择框处理 
        if (lastSelectImg != null)
        {
            lastSelectImg.enabled = false;
        }
        lastSelectImg = selectImg;
        lastSelectImg.enabled = true;

        selectCardData = fuzhuData;

        CalculatedNeedYuanBao(fuzhuData.level);
    }

    /// <summary> 
    /// 创建并展示单位列表 
    /// </summary> 
    private void CreateHeroAndTowerContent()
    {
        TakeBackHeroCardPooling();

        PlayerDataForGame.instance.fightHeroId.Clear();
        PlayerDataForGame.instance.fightTowerId.Clear();
        PlayerDataForGame.instance.fightTrapId.Clear();

        int cardNums = 0;

        SortHSTData(PlayerDataForGame.instance.hstData.heroSaveData);   //  排序 

        NowLevelAndHadChip heroDataIndex = new NowLevelAndHadChip();    //临时记录武将存档信息 
        for (int i = 0; i < PlayerDataForGame.instance.hstData.heroSaveData.Count; i++)
        {
            heroDataIndex = PlayerDataForGame.instance.hstData.heroSaveData[i];
            if (indexChooseListForceId == int.Parse(DataTable.Hero[heroDataIndex.id][6]))
            {
                if (heroDataIndex.level > 0 || heroDataIndex.chips > 0)
                {
                    if (heroDataIndex.isFight > 0)
                    {
                        PlayerDataForGame.instance.fightHeroId.Add(heroDataIndex.id);
                    }
                    cardNums++;
                    ShowOneHeroRules(heroDataIndex);
                }
            }
        }

        NowLevelAndHadChip card = new NowLevelAndHadChip();
        SortHSTData(PlayerDataForGame.instance.hstData.towerSaveData);
        for (int i = 0; i < PlayerDataForGame.instance.hstData.towerSaveData.Count; i++)
        {
            card = PlayerDataForGame.instance.hstData.towerSaveData[i];
            if (indexChooseListForceId == int.Parse(DataTable.TowerData[card.id][15]))
            {
                if (card.level > 0 || card.chips > 0)
                {
                    if (card.isFight > 0)
                    {
                        PlayerDataForGame.instance.fightTowerId.Add(card.id);
                    }
                    cardNums++;
                    ShowOneFuZhuRules(DataTable.Tower, card, 10);
                }
            }
        }
        SortHSTData(PlayerDataForGame.instance.hstData.trapSaveData);
        for (int i = 0; i < PlayerDataForGame.instance.hstData.trapSaveData.Count; i++)
        {
            card = PlayerDataForGame.instance.hstData.trapSaveData[i];
            if (indexChooseListForceId == int.Parse(DataTable.Trap[card.id][14]))
            {
                if (card.level > 0 || card.chips > 0)
                {
                    if (card.isFight > 0)
                    {
                        PlayerDataForGame.instance.fightTrapId.Add(card.id);
                    }
                    cardNums++;
                    ShowOneFuZhuRules(DataTable.Trap, card, 9);
                }
            }
        }
        if (cardNums > 0)
        {
            StartCoroutine(LiteUpdateListChooseFirst(0));
            ShowOrHideInfo(true);
        }
        else
        {
            ShowOrHideInfo(false);
        }
    }

    /// <summary> 
    /// 名字显示规则 
    /// </summary> 
    /// <param name="nameText"></param> 
    /// <param name="str"></param> 
    public void ShowNameTextRules(Text nameText, string str)
    {
        nameText.text = str;
        switch (str.Length)
        {
            case 1:
                nameText.fontSize = 50;
                nameText.lineSpacing = 1.1f;
                break;
            case 2:
                nameText.fontSize = 50;
                nameText.lineSpacing = 1.1f;
                break;
            case 3:
                nameText.fontSize = 50;
                nameText.lineSpacing = 0.9f;
                break;
            case 4:
                nameText.fontSize = 45;
                nameText.lineSpacing = 0.8f;
                break;
            default:
                nameText.fontSize = 45;
                nameText.lineSpacing = 0.8f;
                break;
        }
    }

    /// <summary> 
    /// 显示单个武将 
    /// </summary> 
    /// <param name="heroData"></param> 
    private void ShowOneHeroRules(NowLevelAndHadChip heroData)
    {
        GameObject obj = GetHeroCardFromPool();
        //名字 
        ShowNameTextRules(obj.transform.GetChild(3).GetComponent<Text>(), DataTable.Hero[heroData.id][1]);
        //名字颜色根据稀有度 
        obj.transform.GetChild(3).GetComponent<Text>().color = NameColorChoose(DataTable.Hero[heroData.id][3]);
        //卡牌 
        obj.transform.GetChild(1).GetComponent<Image>().sprite = GameResources.HeroImg[heroData.id];
        //兵种名 
        obj.transform.GetChild(5).GetComponentInChildren<Text>().text = DataTable.ClassData[int.Parse(DataTable.Hero[heroData.id][5])][3];
        //兵种框 
        obj.transform.GetChild(5).GetComponent<Image>().sprite = GameResources.ClassImg[0];
        //边框 
        FrameChoose(DataTable.Hero[heroData.id][3], obj.transform.GetChild(6).GetComponent<Image>());
        //碎片 
        if (heroData.level < DataTable.UpGradeData.Count)
        {
            obj.transform.GetChild(2).GetComponent<Text>().text = heroData.chips + "/" + DataTable.UpGradeData[heroData.level][1];
            obj.transform.GetChild(2).GetComponent<Text>().color = heroData.chips >= int.Parse(DataTable.UpGradeData[heroData.level][1]) ? ColorDataStatic.deep_green : Color.white;
        }
        else
        {
            obj.transform.GetChild(2).GetComponent<Text>().text = "";
        }
        if (heroData.level > 0)
        {
            obj.transform.GetChild(4).GetComponent<Image>().enabled = true;
            obj.transform.GetChild(8).gameObject.SetActive(false);
            //设置星级展示 
            obj.transform.GetChild(4).GetComponent<Image>().sprite = GameResources.GradeImg[heroData.level];
            obj.transform.GetChild(7).gameObject.SetActive(false);
            if (heroData.isFight > 0) //出战标记 
            {
                PlayerDataForGame.instance.AddOrCutFightCardId(heroData.typeIndex, heroData.id, true);
                obj.transform.GetChild(7).gameObject.SetActive(true);
            }
            else
            {
                obj.transform.GetChild(7).gameObject.SetActive(false);
            }
        }
        else
        {
            obj.transform.GetChild(4).GetComponent<Image>().enabled = false;
            obj.transform.GetChild(7).gameObject.SetActive(false);
            obj.transform.GetChild(8).gameObject.SetActive(true);
        }
        obj.GetComponent<Button>().onClick.RemoveAllListeners();
        obj.GetComponent<Button>().onClick.AddListener(delegate ()
        {
            OnClickHeroCardFun(heroData, obj.transform.GetChild(9).GetComponent<Image>());
        });
    }

    /// <summary> 
    /// 点击武将卡牌的方法 
    /// </summary> 
    /// <param name="heroData"></param> 
    private void OnClickHeroCardFun(NowLevelAndHadChip heroData, Image selectImg)
    {
        PlayOnClickMusic();

        //Debug.Log("点击的武将id：" + heroData.id); 
        //武将名字 
        infoTran.GetChild(0).GetComponent<Text>().text = DataTable.Hero[heroData.id][1];
        //武将名字颜色 
        infoTran.GetChild(0).GetComponent<Text>().color = NameColorChoose(DataTable.Hero[heroData.id][3]);
        //武将属性 
        string[] strs_attack = DataTable.Hero[heroData.id][7].Split(',');
        infoTran.GetChild(1).GetComponent<Text>().text = string.Format(DataTable.GetStringText(32), strs_attack[heroData.level > 0 ? heroData.level - 1 : 0]);
        string[] strs_health = DataTable.Hero[heroData.id][8].Split(',');
        infoTran.GetChild(2).GetComponent<Text>().text = string.Format(DataTable.GetStringText(33), strs_health[heroData.level > 0 ? heroData.level - 1 : 0]);
        //武将介绍 
        infoTran.GetChild(3).GetComponent<Text>().text = DataTable.Hero[heroData.id][2];

        //名字 
        ShowNameTextRules(showCardObj.transform.GetChild(3).GetComponent<Text>(), DataTable.Hero[heroData.id][1]);
        //名字颜色 
        showCardObj.transform.GetChild(3).GetComponent<Text>().color = NameColorChoose(DataTable.Hero[heroData.id][3]);
        //卡牌 
        showCardObj.transform.GetChild(1).GetComponent<Image>().sprite = GameResources.HeroImg[heroData.id];
        //兵种名 
        showCardObj.transform.GetChild(5).GetComponentInChildren<Text>().text = DataTable.ClassData[int.Parse(DataTable.Hero[heroData.id][5])][3];
        //兵种框 
        showCardObj.transform.GetChild(5).GetComponent<Image>().sprite = GameResources.ClassImg[0];
        //边框 
        FrameChoose(DataTable.Hero[heroData.id][3], showCardObj.transform.GetChild(6).GetComponent<Image>());
        //碎片 
        if (heroData.level < DataTable.UpGradeData.Count)
        {
            showCardObj.transform.GetChild(2).GetComponent<Text>().text = heroData.chips + "/" + DataTable.UpGradeData[heroData.level][1];
            showCardObj.transform.GetChild(2).GetComponent<Text>().color = heroData.chips >= int.Parse(DataTable.UpGradeData[heroData.level][1]) ? ColorDataStatic.deep_green : Color.black;
        }
        else
        {
            showCardObj.transform.GetChild(2).GetComponent<Text>().text = "";
        }

        int getGoldNums = GetGoldNumsForSellCard(heroData);
        sellCardBtn.transform.GetChild(0).GetComponent<Text>().text = getGoldNums.ToString();
        sellCardBtn.GetComponent<Button>().onClick.RemoveAllListeners();
        sellCardBtn.GetComponent<Button>().onClick.AddListener(delegate ()
        {
            OnClickForSellCard(heroData, getGoldNums);
        });
        sellCardBtn.SetActive(true);

        if (heroData.level > 0)
        {
            showCardObj.transform.GetChild(4).GetComponent<Image>().enabled = true;
            //设置星级展示 
            showCardObj.transform.GetChild(4).GetComponent<Image>().sprite = GameResources.GradeImg[heroData.level];
            //出战相关设置 
            holdOrFightBtn.SetActive(true);
            if (heroData.isFight > 0)
            {
                showCardObj.transform.GetChild(7).gameObject.SetActive(true);
                holdOrFightBtn.GetComponentInChildren<Text>().text = DataTable.GetStringText(30);
            }
            else
            {
                showCardObj.transform.GetChild(7).gameObject.SetActive(false);
                holdOrFightBtn.GetComponentInChildren<Text>().text = DataTable.GetStringText(31);
            }
        }
        else
        {
            //sellCardBtn.SetActive(false); 
            holdOrFightBtn.SetActive(false);
            showCardObj.transform.GetChild(7).gameObject.SetActive(false);
            showCardObj.transform.GetChild(4).GetComponent<Image>().enabled = false;
        }
        //选择框处理 
        if (lastSelectImg != null)
        {
            lastSelectImg.enabled = false;
        }
        lastSelectImg = selectImg;
        lastSelectImg.enabled = true;

        selectCardData = heroData;

        CalculatedNeedYuanBao(heroData.level);
    }

    //根据卡牌类型和id得到其稀有度 
    private int GetIdBackCardRarity(int cardType, int cardId)
    {
        string rarityStr = string.Empty;
        switch (cardType)
        {
            case 0:
                rarityStr = DataTable.Hero[cardId][3];
                break;
            case 1:
                rarityStr = DataTable.SoldierData[cardId][3];
                break;
            case 2:
                rarityStr = DataTable.TowerData[cardId][3];
                break;
            case 3:
                rarityStr = DataTable.Trap[cardId][3];
                break;
            case 4:
                rarityStr = DataTable.SpellData[cardId][3];
                break;
            default:
                break;
        }
        return int.Parse(rarityStr);
    }

    //出售卡牌可得金币 
    private int GetGoldNumsForSellCard(NowLevelAndHadChip heroData)
    {
        int chips = heroData.chips;
        for (int i = 0; i < heroData.level; i++)
        {
            chips += int.Parse(DataTable.UpGradeData[i][1]);
        }
        int golds = 0;
        switch (GetIdBackCardRarity(heroData.typeIndex, heroData.id))
        {
            case 1:
                golds = 10;
                break;
            case 2:
                golds = 20;
                break;
            case 3:
                golds = 50;
                break;
            case 4:
                golds = 100;
                break;
            case 5:
                golds = 200;
                break;
            case 6:
                golds = 500;
                break;
            default:
                break;
        }
        return golds * chips;
    }

    //出售卡牌 
    private void OnClickForSellCard(NowLevelAndHadChip heroData, int getGoldNums)
    {
        //if (GetIdBackCardRarity(heroData.typeIndex, heroData.id) >= 4) 
        {
            AudioController0.instance.ChangeAudioClip(18);
            AudioController0.instance.PlayAudioSource(0);
            queRenWindows.transform.GetChild(0).GetChild(1).GetComponent<Button>().onClick.RemoveAllListeners();
            queRenWindows.transform.GetChild(0).GetChild(1).GetComponent<Button>().onClick.AddListener(delegate ()
            {
                List<NowLevelAndHadChip> datas = new List<NowLevelAndHadChip>();
                switch (heroData.typeIndex)
                {
                    case 0:
                        datas = PlayerDataForGame.instance.hstData.heroSaveData;
                        break;
                    case 1:
                        datas = PlayerDataForGame.instance.hstData.soldierSaveData;
                        break;
                    case 2:
                        datas = PlayerDataForGame.instance.hstData.towerSaveData;
                        break;
                    case 3:
                        datas = PlayerDataForGame.instance.hstData.trapSaveData;
                        break;
                    case 4:
                        datas = PlayerDataForGame.instance.hstData.spellSaveData;
                        break;
                    default:
                        break;
                }
                //Debug.Log("---出售" + heroData.typeIndex + "类型的卡牌：" + heroData.id); 
                heroData.chips = 0;
                heroData.level = 0;
                heroData.isFight = 0;
                //datas.Remove(heroData); 
                //LoadSaveData.instance.SaveByJson(PlayerDataForGame.instance.hstData); 
                PlayerDataForGame.instance.isNeedSaveData = true;
                LoadSaveData.instance.SaveGameData(2);
                ConsumeManager.instance.AddYuanBao(getGoldNums);
                AudioController0.instance.ChangeAudioClip(17);
                AudioController0.instance.PlayAudioSource(0);
                //刷新主城列表 
                ChangeScrollView();
                PlayerDataForGame.instance.AddOrCutFightCardId(heroData.typeIndex, heroData.id, false);
                UpdateCardNumsShow();
                queRenWindows.SetActive(false);
            });
            queRenWindows.SetActive(true);
        }
    }

    /// <summary> 
    /// 匹配稀有度的颜色 
    /// </summary> 
    /// <returns></returns> 
    public Color NameColorChoose(string rarity)
    {
        Color color = new Color();
        switch (rarity)
        {
            case "1":
                color = ColorDataStatic.name_gray;
                break;
            case "2":
                color = ColorDataStatic.name_green;
                break;
            case "3":
                color = ColorDataStatic.name_blue;
                break;
            case "4":
                color = ColorDataStatic.name_purple;
                break;
            case "5":
                color = ColorDataStatic.name_orange;
                break;
            case "6":
                color = ColorDataStatic.name_red;
                break;
            case "7":
                color = ColorDataStatic.name_black;
                break;
            default:
                color = ColorDataStatic.name_gray;
                break;
        }
        return color;
    }

    // <summary> 
    /// 匹配稀有度边框 
    /// </summary> 
    public void FrameChoose(string rarity, Image img)
    {
        img.enabled = false;//暂时不提供边框 
        return;
        img.enabled = true;
        switch (rarity)
        {
            case "4":
                img.sprite = GameResources.FrameImg[3];
                break;
            case "5":
                img.sprite = GameResources.FrameImg[2];
                break;
            case "6":
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
    private void SortHSTData(List<NowLevelAndHadChip> dataList)
    {
        //dataList.Sort((NowLevelAndHadChip n1, NowLevelAndHadChip n2) => n2.Level.CompareTo(n1.Level)); 
        dataList.Sort((c1, c2) =>
        {
            if (c2.isFight.CompareTo(c1.isFight) != 0)
            {
                return c2.isFight.CompareTo(c1.isFight);
            }

            if (c2.level.CompareTo(c1.level) != 0)
            {
                return c2.level.CompareTo(c1.level);
            }

            return GetIdBackCardRarity(c2.typeIndex, c2.id).CompareTo(GetIdBackCardRarity(c1.typeIndex, c1.id));
        });
    }

    /// <summary> 
    /// 初始化玩家信息显示 
    /// </summary> 
    public void InitializationPlayerInfo()
    {
        //player`s name 
        playerInfoObj.transform.GetChild(1).GetChild(1).GetComponent<Text>().text = DataTable.PlayerInitialData[PlayerDataForGame.instance.pyData.ForceId][1];
        if (PlayerDataForGame.instance.pyData.Level >= DataTable.PlayerLevelData.Count)
        {
            playerInfoObj.transform.GetChild(0).GetComponent<Slider>().value = 1;
            playerInfoObj.transform.GetChild(2).GetChild(1).GetComponent<Text>().text = DataTable.GetStringText(34);
            playerInfoObj.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = PlayerDataForGame.instance.pyData.Exp + "/" + 99999;
        }
        else
        {
            //Exp 
            playerInfoObj.transform.GetChild(0).GetComponent<Slider>().value = PlayerDataForGame.instance.pyData.Exp / float.Parse(DataTable.PlayerLevelData[PlayerDataForGame.instance.pyData.Level][1]);
            //Level 
            playerInfoObj.transform.GetChild(2).GetChild(1).GetComponent<Text>().text = string.Format(DataTable.GetStringText(35), PlayerDataForGame.instance.pyData.Level);//玩家等级 
            playerInfoObj.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = PlayerDataForGame.instance.pyData.Exp + "/" + DataTable.PlayerLevelData[PlayerDataForGame.instance.pyData.Level][1];
        }
        //货币 
        yuanBaoNumText.text = PlayerDataForGame.instance.pyData.YuanBao.ToString();
        yvQueNumText.text = PlayerDataForGame.instance.pyData.YvQue.ToString();
        showTiLiNums = PlayerDataForGame.instance.pyData.Stamina;
        tiLiNumText.text = showTiLiNums + "/90";

        CreateHeroAndTowerContent();
        UpdateCardNumsShow();

        StartCoroutine(LateToChangeViewShow(0));
    }

    //得到合成所需元宝 
    private void CalculatedNeedYuanBao(int nowLevel)
    {
        if (nowLevel == 0)
        {
            heImgObj.SetActive(true);
            ShowOrHideGuideObj(2, true);
        }
        else
        {
            heImgObj.SetActive(false);
        }
        heImgObj.SetActive(nowLevel == 0);
        if (nowLevel < DataTable.UpGradeData.Count)
        {
            needYuanBaoNums = int.Parse(DataTable.UpGradeData[nowLevel][2]);
            heChengBtn.transform.GetComponentInChildren<Text>().text = "" + needYuanBaoNums;
            heChengBtn.SetActive(true);
        }
        else
        {
            heChengBtn.SetActive(false);
        }
    }

    /// <summary> 
    /// 合成卡牌 
    /// </summary> 
    public void SynthesizeCard()
    {
        if (selectCardData.chips >= int.Parse(DataTable.UpGradeData[selectCardData.level][1]))
        {
            if (ConsumeManager.instance.DeductYuanBao(needYuanBaoNums))
            {
                selectCardData.chips -= int.Parse(DataTable.UpGradeData[selectCardData.level][1]);

                selectCardData.level++;
                if (!selectCardData.isHad)
                {
                    selectCardData.isHad = true;
                    MainWuBeiUIManager.instance.UpdateArmsBtnTextShow();
                }
                if (selectCardData.maxLevel < selectCardData.level)
                {
                    selectCardData.maxLevel = selectCardData.level;
                }
                //LoadSaveData.instance.SaveByJson(PlayerDataForGame.instance.hstData); 
                PlayerDataForGame.instance.isNeedSaveData = true;
                LoadSaveData.instance.SaveGameData(2);

                upStarEffectObj.SetActive(false);
                upStarEffectObj.SetActive(true);
                StartCoroutine(HideTheEffectOfUpStar());

                AudioController0.instance.ChangeAudioClip(16);
                AudioController0.instance.PlayAudioSource(0);

                UpdateLevelCard();

                ShowOrHideGuideObj(2, false);
            }
            else
            {
                //Debug.Log("元宝不足，合成失败"); 
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(36));
                PlayOnClickMusic();
            }
        }
        else
        {
            //Debug.Log("碎片不足，合成失败"); 
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(37));
            PlayOnClickMusic();
        }
    }

    //隐藏升星特效 
    IEnumerator HideTheEffectOfUpStar()
    {
        yield return new WaitForSeconds(1.7f);
        upStarEffectObj.SetActive(false);
    }

    /// <summary> 
    /// 出战或回城设置方法 
    /// </summary> 
    public void ChuZhanOrStaySetFun()
    {
        Transform listCard = lastSelectImg.transform.parent;
        if (selectCardData.isFight > 0)
        {
            if (PlayerDataForGame.instance.AddOrCutFightCardId(selectCardData.typeIndex, selectCardData.id, false))
            {
                listCard.GetChild(7).gameObject.SetActive(false);
                showCardObj.transform.GetChild(7).gameObject.SetActive(false);
                holdOrFightBtn.GetComponentInChildren<Text>().text = DataTable.GetStringText(31);
                selectCardData.isFight = 0;
                AudioController0.instance.ChangeAudioClip(15);
                AudioController0.instance.PlayAudioSource(0);
            }
            else
            {
                PlayOnClickMusic();
            }
        }
        else
        {
            if (PlayerDataForGame.instance.AddOrCutFightCardId(selectCardData.typeIndex, selectCardData.id, true))
            {
                listCard.GetChild(7).gameObject.SetActive(true);
                showCardObj.transform.GetChild(7).gameObject.SetActive(true);
                holdOrFightBtn.GetComponentInChildren<Text>().text = DataTable.GetStringText(30);
                selectCardData.isFight = 1;
                AudioController0.instance.ChangeAudioClip(14);
                AudioController0.instance.PlayAudioSource(0);
            }
            else
            {
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(38));

                PlayOnClickMusic();
            }
        }
        UpdateCardNumsShow();
        //LoadSaveData.instance.SaveByJson(PlayerDataForGame.instance.hstData); 
        PlayerDataForGame.instance.isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData(2);
    }

    //升级卡牌后更新显示 
    private void UpdateLevelCard()
    {
        //Debug.Log("selectCardData.Level: " + selectCardData.Level); 
        Transform listCard = lastSelectImg.transform.parent;
        if (selectCardData.level < DataTable.UpGradeData.Count)
        {
            listCard.GetChild(2).GetComponent<Text>().text = selectCardData.chips + "/" + DataTable.UpGradeData[selectCardData.level][1];
            listCard.GetChild(2).GetComponent<Text>().color = selectCardData.chips >= int.Parse(DataTable.UpGradeData[selectCardData.level][1]) ? ColorDataStatic.deep_green : Color.white;
        }
        else
        {
            listCard.GetChild(2).GetComponent<Text>().text = "";
        }
        listCard.GetChild(4).GetComponent<Image>().enabled = true;
        //设置星级展示 
        listCard.GetChild(4).GetComponent<Image>().sprite = GameResources.GradeImg[selectCardData.level];
        listCard.GetChild(8).gameObject.SetActive(false);
        listCard.GetComponent<Button>().onClick.Invoke();
    }

    /// <summary> 
    /// 展示奖励 
    /// </summary> 
    /// <param name="yuanBaoNums">元宝</param> 
    /// <param name="yuQueNums">玉阙</param> 
    /// <param name="expNums">经验</param> 
    /// <param name="tiLiNums">体力</param> 
    /// <param name="rewardsCards">卡牌奖励</param> 
    /// <param name="waitTime">展示等待时间</param> 
    public void ShowRewardsThings(int yuanBaoNums, int yuQueNums, int expNums, int tiLiNums, List<RewardsCardClass> rewardsCards, float waitTime)
    {
        for (int i = 0; i < rewardsParent.childCount; i++)
        {
            if (rewardsParent.GetChild(i).gameObject.activeSelf)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (rewardsParent.GetChild(i).GetChild(j).gameObject.activeSelf)
                    {
                        rewardsParent.GetChild(i).GetChild(j).gameObject.SetActive(false);
                    }
                }
                rewardsParent.GetChild(i).gameObject.SetActive(false);
            }
        }

        //rewardsShowObj.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = str; 
        if (yuanBaoNums > 0)
        {
            ShowOneReward(0, new RewardsCardClass() { cardChips = yuanBaoNums });
        }
        if (yuQueNums > 0)
        {
            ShowOneReward(1, new RewardsCardClass() { cardChips = yuQueNums });
        }
        if (expNums > 0)
        {
            ShowOneReward(2, new RewardsCardClass() { cardChips = expNums });
        }
        if (tiLiNums > 0)
        {
            ShowOneReward(3, new RewardsCardClass() { cardChips = tiLiNums });
        }
        for (int i = 0; i < rewardsCards.Count; i++)
        {
            ShowOneReward(4, rewardsCards[i]);
        }
        StartCoroutine(OpenRewardsWindows(waitTime));
    }
    //展示奖品 
    IEnumerator OpenRewardsWindows(float startTime)
    {
        yield return new WaitForSeconds(startTime);
        taoYuan.CloseAllChests();
        rewardsShowObj.SetActive(true);
        //刷新主城列表 
        ChangeScrollView();
        rewardsShowObj.transform.GetComponentInChildren<ScrollRect>().horizontalNormalizedPosition = 0f;
        yield return new WaitForSeconds(1f);
        rewardsShowObj.transform.GetComponentInChildren<ScrollRect>().DOHorizontalNormalizedPos(1f, 1f);

        yield return new WaitForSeconds(1f);
        PlayerDataForGame.instance.ClearGarbageStationObj();
    }

    //获取单个奖品展示框 
    private GameObject FindShowRewardsBox()
    {
        GameObject go = new GameObject();
        PlayerDataForGame.garbageStationObjs.Add(go);

        for (int i = 0; i < rewardsParent.childCount; i++)
        {
            go = rewardsParent.GetChild(i).gameObject;
            if (!go.activeSelf)
            {
                go.SetActive(true);
                return go;
            }
        }
        go = Instantiate(rewardObj, rewardsParent);

        return go;
    }

    /// <summary> 
    /// 展示单个奖品 
    /// </summary> 
    /// <param name="rewardType">0元宝1玉阙2经验3卡牌</param> 
    /// <param name="rewardsCard"></param> 
    private void ShowOneReward(int rewardType, RewardsCardClass rewardsCard)
    {
        if (rewardsCard.cardChips <= 0)
        {
            return;
        }

        GameObject obj = FindShowRewardsBox();

        obj.transform.GetChild(rewardType).gameObject.SetActive(true);
        if (rewardType == 4)
        {
            Transform cardTran = obj.transform.GetChild(4);
            switch (rewardsCard.cardType)
            {
                case 0:
                    cardTran.GetComponent<Image>().sprite = GameResources.HeroImg[rewardsCard.cardId];
                    ShowNameTextRules(cardTran.GetChild(0).GetComponent<Text>(), DataTable.Hero[rewardsCard.cardId][1]);
                    cardTran.GetChild(0).GetComponent<Text>().color = NameColorChoose(DataTable.Hero[rewardsCard.cardId][3]);
                    cardTran.GetChild(1).GetComponent<Image>().sprite = GameResources.ClassImg[0];
                    cardTran.GetChild(1).GetChild(0).GetComponentInChildren<Text>().text = DataTable.ClassData[int.Parse(DataTable.Hero[rewardsCard.cardId][5])][3];
                    FrameChoose(DataTable.Hero[rewardsCard.cardId][3], cardTran.GetChild(2).GetComponent<Image>());
                    break;
                case 1:
                    //cardTran.GetChild(0).GetComponent<Text>().text = LoadJsonFile.soldierTableDatas[rewardsCard.cardId][1]; 
                    break;
                case 2:
                    cardTran.GetComponent<Image>().sprite = GameResources.FuZhuImg[int.Parse(DataTable.TowerData[rewardsCard.cardId][10])];
                    ShowNameTextRules(cardTran.GetChild(0).GetComponent<Text>(), DataTable.TowerData[rewardsCard.cardId][1]);
                    cardTran.GetChild(0).GetComponent<Text>().color = NameColorChoose(DataTable.TowerData[rewardsCard.cardId][3]);
                    cardTran.GetChild(1).GetComponent<Image>().sprite = GameResources.ClassImg[1];
                    cardTran.GetChild(1).GetChild(0).GetComponentInChildren<Text>().text = DataTable.TowerData[rewardsCard.cardId][5];
                    FrameChoose(DataTable.TowerData[rewardsCard.cardId][3], cardTran.GetChild(2).GetComponent<Image>());
                    break;
                case 3:
                    cardTran.GetComponent<Image>().sprite = GameResources.FuZhuImg[int.Parse(DataTable.TrapData[rewardsCard.cardId][9])];
                    ShowNameTextRules(cardTran.GetChild(0).GetComponent<Text>(), DataTable.Trap[rewardsCard.cardId][1]);
                    cardTran.GetChild(0).GetComponent<Text>().color = NameColorChoose(DataTable.Trap[rewardsCard.cardId][3]);
                    cardTran.GetChild(1).GetComponent<Image>().sprite = GameResources.ClassImg[1];
                    cardTran.GetChild(1).GetChild(0).GetComponentInChildren<Text>().text = DataTable.Trap[rewardsCard.cardId][5];
                    FrameChoose(DataTable.Trap[rewardsCard.cardId][3], cardTran.GetChild(2).GetComponent<Image>());
                    break;
                case 4:
                    //cardTran.GetChild(0).GetComponent<Text>().text = LoadJsonFile.spellTableDatas[rewardsCard.cardId][1]; 
                    break;
                default:
                    break;
            }
        }
        obj.transform.GetChild(5).GetComponent<Text>().text = "×" + rewardsCard.cardChips;
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
        //暂时未开启的页面 
        waitWhileImpress.gameObject.SetActive(
            index == 3 //对决 
        );
        switch (index)
        {
            case 0://桃园 
                ShowOrHideGuideObj(0, true);
                if (PlayerDataForGame.instance.gbocData.fightBoxs.Count > 0) ShowOrHideGuideObj(1, true);
                break;
            case 2://战役 
                ShowOrHideGuideObj(3, true);
                warsChooseListObj.transform.parent.parent.GetComponent<ScrollRect>().DOVerticalNormalizedPos(0f, 0.3f);
                expedition.OnClickChangeWarsFun(expedition.RecordedExpeditionWarId);
                break;
            case 4://霸业 
                bayeBelowLevelPanel.gameObject.SetActive(PlayerDataForGame.instance.pyData.Level < 5);
                if (!SystemTimer.IsToday(PlayerDataForGame.instance.warsData.baYe.lastBaYeActivityTime))
                {
                    BaYeManager.instance.Init();
                    InitBaYeFun();
                }
                if (BaYeManager.instance.isShowTips)
                {
                    PlayerDataForGame.instance.ShowStringTips(BaYeManager.instance.tipsText);
                    BaYeManager.instance.tipsText = string.Empty;
                    BaYeManager.instance.isShowTips = false;
                }
                break;
            case 1://主城 
                break;
            case 3://对决 
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(67));
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
            if (PlayerDataForGame.instance.guideObjsShowed[index] == 0)
            {
                guideObjs[index].SetActive(true);
            }
        }
        else
        {
            if (PlayerDataForGame.instance.guideObjsShowed[index] == 0)
            {
                guideObjs[index].SetActive(false);
                PlayerDataForGame.instance.guideObjsShowed[index] = 1;
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

    //用于刷新列表后选择第一个单位 
    IEnumerator LiteUpdateListChooseFirst(float startTime)
    {
        yield return new WaitForSeconds(startTime);
        if (zhuChengHeroContentObj.transform.childCount > 0)
        {
            zhuChengHeroContentObj.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
        }
    }

    /// <summary> 
    /// 获取玩家经验 
    /// </summary> 
    /// <param name="expNums"></param> 
    public void GetPlayerExp(int expNums)
    {
        if (PlayerDataForGame.instance.pyData.Level >= DataTable.PlayerLevelData.Count)
        {
            PlayerDataForGame.instance.pyData.Exp += expNums;
            playerInfoObj.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = PlayerDataForGame.instance.pyData.Exp + "/" + 99999;
        }
        else
        {
            PlayerDataForGame.instance.pyData.Exp += expNums;
            while (int.Parse(DataTable.PlayerLevelData[PlayerDataForGame.instance.pyData.Level][1]) <= PlayerDataForGame.instance.pyData.Exp)
            {
                PlayerDataForGame.instance.pyData.Exp -= int.Parse(DataTable.PlayerLevelData[PlayerDataForGame.instance.pyData.Level][1]);
                PlayerDataForGame.instance.pyData.Level++;
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(39));
                if (PlayerDataForGame.instance.pyData.Level >= DataTable.PlayerLevelData.Count)
                {
                    PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(40));
                    break;
                }
            }
            if (PlayerDataForGame.instance.pyData.Level >= DataTable.PlayerLevelData.Count)
            {
                playerInfoObj.transform.GetChild(0).GetComponent<Slider>().value = 1;
                playerInfoObj.transform.GetChild(2).GetChild(1).GetComponent<Text>().text = DataTable.GetStringText(34);
                playerInfoObj.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = PlayerDataForGame.instance.pyData.Exp + "/" + 99999;
            }
            else
            {
                playerInfoObj.transform.GetChild(0).GetComponent<Slider>().value = PlayerDataForGame.instance.pyData.Exp / float.Parse(DataTable.PlayerLevelData[PlayerDataForGame.instance.pyData.Level][1]);
                playerInfoObj.transform.GetChild(2).GetChild(1).GetComponent<Text>().text = string.Format(DataTable.GetStringText(35), PlayerDataForGame.instance.pyData.Level);
                playerInfoObj.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = PlayerDataForGame.instance.pyData.Exp + "/" + DataTable.PlayerLevelData[PlayerDataForGame.instance.pyData.Level][1];
            }
            UpdateCardNumsShow();
        }
        //LoadSaveData.instance.SaveByJson(PlayerDataForGame.instance.pyData); 
        PlayerDataForGame.instance.isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData(1);
    }

    /// <summary> 
    /// 初始化卡牌池 
    /// </summary> 
    private void InitHeroCardPooling()
    {
        for (int i = 0; i < minInitCardCount; i++)
        {
            GameObject go = Instantiate(heroCardCityPre, zhuChengHeroContentObj.transform);
            go.SetActive(false);
            heroCardPoolList.Add(go);
        }
    }

    /// <summary> 
    /// 从卡牌池中获取空卡牌 
    /// </summary> 
    /// <returns></returns> 
    private GameObject GetHeroCardFromPool()
    {
        foreach (GameObject item in heroCardPoolList)
        {
            if (!item.activeSelf)
            {
                item.SetActive(true);
                return item;
            }
        }
        GameObject go = Instantiate(heroCardCityPre, zhuChengHeroContentObj.transform);
        heroCardPoolList.Add(go);
        return go;
    }

    /// <summary> 
    /// 回收卡牌池 
    /// </summary> 
    private void TakeBackHeroCardPooling()
    {
        for (int i = 0; i < heroCardPoolList.Count; i++)
        {
            if (heroCardPoolList[i].activeSelf)
            {
                heroCardPoolList[i].SetActive(false);
            }
        }
    }

    //添加体力 
    public void AddTiLiNums(int addNums)
    {
        TimeSystemControl.instance.AddTiLiNums(addNums);
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
        if (AudioController0.instance.isPlayMusic != 1)
        {
            musicBtnText.text = DataTable.GetStringText(41);
        }
        else
        {
            musicBtnText.text = DataTable.GetStringText(42);
        }
    }

    //开关音乐 
    public void OpenOrCloseMusic()
    {
        if (AudioController0.instance.isPlayMusic != 1)
        {
            //打开 
            PlayerPrefs.SetInt(LoadSaveData.instance.IsPlayMusicStr, 1);
            AudioController0.instance.isPlayMusic = 1;
            AudioController1.instance.audioSource.Play();
            musicBtnText.text = DataTable.GetStringText(42);
            PlayOnClickMusic();
        }
        else
        {
            //关闭 
            PlayerPrefs.SetInt(LoadSaveData.instance.IsPlayMusicStr, 0);
            AudioController0.instance.isPlayMusic = 0;
            AudioController0.instance.audioSource.Pause();
            AudioController1.instance.audioSource.Pause();
            musicBtnText.text = DataTable.GetStringText(41);
        }
    }

    bool isJinNangReady = true;  //记录是否可以开启锦囊 

    //刷新锦囊入口的显示 
    public void UpdateShowJinNangBtn(bool isReady)
    {
        if (isJinNangReady == isReady) return;
        taoYuan.jinNangBtn.gameObject.SetActive(isReady);
        isJinNangReady = isReady;
    }

    [SerializeField]
    Button rtCloseBtn;  //兑换界面关闭按钮 
    [SerializeField]
    InputField rtInputField;  //兑换界面输入控件 
    [SerializeField]
    Button rtconfirmBtn;  //兑换界面确认兑换按钮 

    //兑换礼包方法 
    public void RedemptionCodeFun()
    {
        string str = rtInputField.text;
        if (str == "")
        {
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(45));
            PlayOnClickMusic();
        }
        else
        {
            int indexId = -1;
            for (int i = 0; i < DataTable.RCodeData.Count; i++)
            {
                if (str == DataTable.RCodeData[i][1])
                {
                    indexId = i;
                    break;
                }
            }
            if (indexId != -1)
            {
                string[] arr = DataTable.RCodeData[indexId][2].Split('-');
                DateTime startTime = DateTime.ParseExact(arr[0], "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                DateTime endTime = DateTime.ParseExact(arr[1], "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                DateTime nowTime = TimeSystemControl.instance.SystemTimer.Now.LocalDateTime;

                if (nowTime < startTime || nowTime > endTime)
                {
                    rtInputField.text = "";
                    PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(47));
                    PlayOnClickMusic();
                }
                else
                {
                    if (!PlayerDataForGame.instance.gbocData.redemptionCodeGotList[indexId].isGot)
                    {
                        //获得奖励 
                        int addYvQueNums = int.Parse(DataTable.RCodeData[indexId][4]);
                        ConsumeManager.instance.AddYuQue(addYvQueNums);
                        int addYuanBaoNums = int.Parse(DataTable.RCodeData[indexId][5]);
                        ConsumeManager.instance.AddYuanBao(addYuanBaoNums);
                        int tiLiNums = int.Parse(DataTable.RCodeData[indexId][6]);
                        AddTiLiNums(tiLiNums);
                        string[] arrRewards = DataTable.RCodeData[indexId][7].Split(';');
                        List<RewardsCardClass> rewards = new List<RewardsCardClass>();
                        for (int i = 0; i < arrRewards.Length; i++)
                        {
                            if (arrRewards[i] != "")
                            {
                                string[] arrs = arrRewards[i].Split(',');
                                var cardType = int.Parse(arrs[0]);
                                var cardId = int.Parse(arrs[1]);
                                var chips = int.Parse(arrs[2]);
                                rewardManager.RewardCard((GameCardType)cardType, cardId, chips);
                                RewardsCardClass rewardCard = new RewardsCardClass();
                                rewardCard.cardType = cardType;
                                rewardCard.cardId = cardId;
                                rewardCard.cardChips = chips;
                                rewards.Add(rewardCard);
                            }
                        }
                        PlayerDataForGame.instance.isNeedSaveData = true;
                        LoadSaveData.instance.SaveGameData(2);
                        ShowRewardsThings(addYuanBaoNums, addYvQueNums, 0, tiLiNums, rewards, 0);

                        PlayerDataForGame.instance.gbocData.redemptionCodeGotList[indexId].isGot = true;
                        PlayerDataForGame.instance.isNeedSaveData = true;
                        LoadSaveData.instance.SaveGameData(4);

                        rtInputField.text = "";
                        PlayerDataForGame.instance.ShowStringTips(DataTable.RCodeData[indexId][3]);
                        rtCloseBtn.onClick.Invoke();
                        AudioController0.instance.ChangeAudioClip(0);
                        AudioController0.instance.PlayAudioSource(0);
                    }
                    else
                    {
                        rtInputField.text = "";
                        PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(48));
                        PlayOnClickMusic();
                    }
                }
            }
            else
            {
                rtInputField.text = "";
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(49));
                PlayOnClickMusic();
            }
        }
    }

    ///////////////////////////鸡坛相关///////////////////////////////// 

    //给体力商店按钮添加方法 
    private void InitChickenBtnFun()
    {
        for (int i = 0; i < chickenShopBtns.Length; i++)
        {
            int index = i;
            chickenShopBtns[i].onClick.AddListener(delegate ()
            {
                ChickenShoppingGetTiLi(index);
            });
            //显示体力的数量 
            chickenShopBtns[i].transform.parent.GetChild(1).GetComponent<Text>().text = "×" + DataTable.TiLiStoreData[i][1];
            //显示消耗玉阙的数量 
            if (i != 0)
            {
                chickenShopBtns[i].transform.GetChild(0).GetComponent<Text>().text = "×" + DataTable.TiLiStoreData[i][2];
            }
        }

        chickenEntObj.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(delegate ()
        {
            AudioController0.instance.ChangeAudioClip(25);
            AudioController0.instance.PlayAudioSource(0);
            chickenShopWindowObj.SetActive(true);
        });
    }

    //体力商店按钮统一处理 
    private void OpenOrCloseChickenBtn(bool isCanTake)
    {
        for (int i = 0; i < chickenShopBtns.Length; i++)
        {
            chickenShopBtns[i].enabled = isCanTake;
        }
    }

    //消耗玉阙获得体力 
    private bool GetTiLiForChicken(int quQueNums, int tiLiNums)
    {
        if (ConsumeManager.instance.DeductYuQue(quQueNums))
        {
            AddTiLiNums(tiLiNums);
            return true;
        }
        else
        {
            return false;
        }
    }

    //商店购买体力 
    [Skip]
    private void ChickenShoppingGetTiLi(int indexBtn)
    {
        AudioController0.instance.ChangeAudioClip(13);
        OpenOrCloseChickenBtn(false);
        int getTiLiNums = int.Parse(DataTable.TiLiStoreData[indexBtn][1]);
        int needYvQueNums = int.Parse(DataTable.TiLiStoreData[indexBtn][2]);
        switch (indexBtn)
        {
            case 0:
                AdAgent.instance.BusyRetry(() =>
                {

                    GetTiLiForChicken(needYvQueNums, getTiLiNums);
                    PlayerDataForGame.instance.ShowStringTips(string.Format(DataTable.GetStringText(50),
                        getTiLiNums));
                    GetCkChangeTimeAndWindow();
                    AudioController0.instance.ChangeAudioClip(25);
                    AudioController0.instance.PlayAudioSource(0);
                }, () =>
                {
                    PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(6));
                    OpenOrCloseChickenBtn(true);
                });
                break;
            case 1:
            case 2:
                if (GetTiLiForChicken(needYvQueNums, getTiLiNums))
                {
                    PlayerDataForGame.instance.ShowStringTips(string.Format(DataTable.GetStringText(51), getTiLiNums));
                    GetCkChangeTimeAndWindow();
                    AudioController0.instance.ChangeAudioClip(25);
                }
                else
                {
                    OpenOrCloseChickenBtn(true);
                }
                break;
        }
        AudioController0.instance.PlayAudioSource(0);
    }

    //成功获得体力后的方法 
    private void GetCkChangeTimeAndWindow()
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

        chickenShopWindowObj.SetActive(false);
        OpenOrCloseChickenBtn(true);
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

    //对开启鸡坛时间进行矫正 
    public void InitOpenChickenTime(bool isGetNetTime)
    {
        if (!isGetNetTime)
        {
            //没有网络连接关闭鸡坛入口 
            if (chickenEntObj.activeSelf)
            {
                chickenEntObj.SetActive(false);
            }
        }
        else
        {
            bool isOpen = CanOpenChickenEntr();
            if (chickenEntObj.activeSelf != isOpen)
            {
                chickenEntObj.SetActive(isOpen);
            }
        }
    }

    int openCKTime0 = 0;    //0未到时1可开启2已领取 
    int openCKTime1 = 0;
    int openCKTime2 = 0;

    int closeCkWinSeconds = 7201;

    //刷新鸡坛关闭时间显示 
    private void UpdateChickenCloseTime(TimeSpan dspNow, TimeSpan dspEnd)
    {
        int seconds = (int)(dspEnd.TotalSeconds - dspNow.TotalSeconds);
        if (seconds < closeCkWinSeconds)
        {
            closeCkWinSeconds = seconds;
            chickenCloseText.text = TimeSystemControl.instance.TimeDisplayText(closeCkWinSeconds);
        }
    }

    //是否可以开启鸡坛 
    private bool CanOpenChickenEntr()
    {
        //当前时间点TimeOfDay 
        TimeSpan dspNow = TimeSystemControl.instance.SystemTimer.Now.LocalDateTime.TimeOfDay;
        //TimeSpan dspNow = DateTime.Now.TimeOfDay; 

        //在12点-14点之间 
        if (chickenOpenTs[0][0] < dspNow && dspNow < chickenOpenTs[0][1])
        {
            //如果未领取过 
            if (openCKTime0 != 2)
            {
                if (openCKTime0 == 0)
                {
                    openCKTime0 = 1;
                    PlayerPrefs.SetInt(TimeSystemControl.openCKTime0_str, openCKTime0);

                    openCKTime2 = 0;
                    PlayerPrefs.SetInt(TimeSystemControl.openCKTime2_str, openCKTime2);

                    closeCkWinSeconds = 7201;

                    TimeSystemControl.instance.UpdateIsNotFirstInGame();
                }
                UpdateChickenCloseTime(dspNow, chickenOpenTs[0][1]);
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (openCKTime0 != 0)
            {
                openCKTime0 = 0;
                PlayerPrefs.SetInt(TimeSystemControl.openCKTime0_str, openCKTime0);
            }
        }
        //在17点-19点之间 
        if (chickenOpenTs[1][0] < dspNow && dspNow < chickenOpenTs[1][1])
        {
            if (openCKTime1 != 2)
            {
                if (openCKTime1 == 0)
                {
                    openCKTime1 = 1;
                    PlayerPrefs.SetInt(TimeSystemControl.openCKTime1_str, openCKTime1);

                    openCKTime0 = 0;
                    PlayerPrefs.SetInt(TimeSystemControl.openCKTime0_str, openCKTime0);

                    closeCkWinSeconds = 7201;

                    TimeSystemControl.instance.UpdateIsNotFirstInGame();
                }
                UpdateChickenCloseTime(dspNow, chickenOpenTs[1][1]);
                return false;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (openCKTime1 != 0)
            {
                openCKTime1 = 0;
                PlayerPrefs.SetInt(TimeSystemControl.openCKTime1_str, openCKTime1);
            }
        }
        //在21点-23点之间 
        if (chickenOpenTs[2][0] < dspNow && dspNow < chickenOpenTs[2][1])
        {
            if (openCKTime2 != 2)
            {
                if (openCKTime2 == 0)
                {
                    openCKTime2 = 1;
                    PlayerPrefs.SetInt(TimeSystemControl.openCKTime2_str, openCKTime2);

                    openCKTime1 = 0;
                    PlayerPrefs.SetInt(TimeSystemControl.openCKTime1_str, openCKTime1);

                    openCKTime0 = 0;
                    PlayerPrefs.SetInt(TimeSystemControl.openCKTime1_str, openCKTime1);

                    closeCkWinSeconds = 7201;

                    TimeSystemControl.instance.UpdateIsNotFirstInGame();
                }
                UpdateChickenCloseTime(dspNow, chickenOpenTs[2][1]);
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (openCKTime2 != 0)
            {
                openCKTime2 = 0;
                PlayerPrefs.SetInt(TimeSystemControl.openCKTime2_str, openCKTime2);
            }
        }
        return false;
    }

    bool isShowQuitTips = false;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isShowQuitTips)
            {
                ExitGame();
            }
            else
            {
                isShowQuitTips = true;
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(52));
                Invoke("ResetQuitBool", 2f);
            }
        }
    }
    //重置退出游戏判断参数 
    private void ResetQuitBool()
    {
        isShowQuitTips = false;
    }

    /// <summary> 
    /// 存储游戏 
    /// </summary> 
    public void SaveGame()
    {
        LoadSaveData.instance.SaveGameData();
    }

    //退出游戏 
    public void ExitGame()
    {
        PlayOnClickMusic();

#if UNITY_ANDROID
        Application.Quit();
#endif
    }
}