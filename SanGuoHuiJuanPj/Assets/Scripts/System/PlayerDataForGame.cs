using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerDataForGame : MonoBehaviour
{
    public static PlayerDataForGame instance;

    public int secsPerStamina = 600;
    //修复v1.89无限刷霸业宝箱3的Bug 
    [Serializable]
    public enum WarTypes
    {
        None = 0,
        Expedition = 1, //主线战役 
        Baye = 2, //霸业 
    }
    public enum RedeemTypes
    {
        JinNang = 0, // 锦囊 
        JiuTan = 1  //酒坛 
    }
    public WarTypes WarType;//标记当前战斗类型 

    public Dictionary<WarTypes, int> WarForceMap { get; } = new Dictionary<WarTypes, int>
    {
        {WarTypes.Expedition, 0},
        {WarTypes.Baye, 0}
    };
    /// <summary>
    /// 当前战役选择的势力
    /// </summary>
    public int CurrentWarForceId => WarForceMap[WarType];

    [HideInInspector]
    public bool isNeedSaveData; //记录是否需要存档 

    [HideInInspector]
    public bool isHadNewSaveData; //记录游戏内是否有最新的读档数据 

    public UserInfo acData = new UserInfo();  //玩家账户信息 
    public int Arrangement { get; set; }//服务器账号标记

    [HideInInspector]public PlayerData pyData;  //玩家基本信息 
    public Character Character;//角色信息
    public GetBoxOrCodeData gbocData = new GetBoxOrCodeData();  //玩家宝箱与兑换码信息 
    public HSTDataClass hstData = new HSTDataClass(); //玩家武将士兵塔等信息 
    public BaYeDataClass baYe = new BaYeDataClass();

    public WarsDataClass warsData
    {
        get => _warsData;
        set
        {
            _warsData = value;
        }
    } //玩家战役解锁+霸业进度信息 

    public int[] GuideObjsShowed
    {
        get => guideObjsShowed;
        set => guideObjsShowed = value;
    } //存放各个指引展示情况 

    //记录出战单位 
    [HideInInspector]
    public List<int> fightHeroId = new List<int>();
    [HideInInspector]
    public List<int> fightSoLdierId = new List<int>();
    [HideInInspector]
    public List<int> fightTowerId = new List<int>();
    [HideInInspector]
    public List<int> fightTrapId = new List<int>();
    [HideInInspector]
    public List<int> fightSpellId = new List<int>();

    [HideInInspector]
    public int selectedWarId = -1;    //记录选择的战役id 

    [HideInInspector]
    public int zhanYiColdNums = 0;  //记录战役的金币数 
                                    //[HideInInspector] 
                                    //public int baYeGoldNums = 0;    //记录霸业金币数 

    float fadeSpeed = 1.5f;   //渐隐渐显时间 
    [HideInInspector]
    public bool isJumping;
    float loadPro;      //加载进度 
    AsyncOperation asyncOp;
    [SerializeField]
    Text loadingText;   //加载进度文本 
    [SerializeField]
    Text infoText;      //小提示文本 
    [SerializeField]
    Image loadingImg;   //遮布 

    [HideInInspector]
    public int lastSenceIndex;  //上一个场景索引记录 
    private bool isRequestingSaveFile; //存档请求中
    //计算出战总数量 
    public int TotalCardsEnlisted => fightHeroId.Count + fightTowerId.Count + fightTrapId.Count;
    public int UnlockForceId =>
        DataTable.Force.Values.Where(f => f.UnlockLevel <= pyData.Level).Max(f => f.Id);// DataTable.PlayerLevelConfig[pyData.Level].UnlockForces;
    public LocalStamina Stamina
    {
        get
        {
            if (stamina == null) GenerateLocalStamina();
            return stamina;
        }
    }

    private LocalStamina stamina;

    public WarReward WarReward { get; set; }
    public StaminaCost CurrentStaCost { get; private set; }
    public BaYeManager BaYeManager;
    public bool IsCompleteLoading { get; private set; }
    public int AdPass => pyData.AdPass;
    public int MilitaryPower => GetCards(false).Sum(c => c.Power());

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public virtual void Init(UnityAction onCompleteAction)
    {
        selectedWarId = -1;
        isJumping = false;
        loadPro = 0;
        asyncOp = null;

        lastSenceIndex = 0;
        isNeedSaveData = false;
        isHadNewSaveData = false;

        StartCoroutine(InitFade());
        onCompleteAction?.Invoke();
    }

    private void Update()
    {
        if (!isJumping) return;
        loadPro = asyncOp?.progress??0.3f; //获取加载进度,最大为0.9 
        loadingText.text = string.Format(DataTable.GetStringText(63), (int)(loadPro * 100));
        if (loadPro < 1) return;
        if (LoadSaveData.instance.isLoadingSaveData) return;
        loadPro = 0;
        isJumping = false;
    }

    IEnumerator InitFade()
    {
        yield return new WaitUntil(() => GameSystem.CurrentScene == GameSystem.GameScene.StartScene);
        loadingText.gameObject.SetActive(false);
        infoText.gameObject.SetActive(false);
        loadingImg.gameObject.SetActive(true);
        loadingImg.DOFade(0, fadeSpeed).OnComplete(() => loadingImg.gameObject.SetActive(false));
    }

    /// <summary> 
    /// 跳转场景 
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="isRequestSyncData">是否请求同步存档</param>
    /// <param name="untilTrue">加载锁，直到返回true才会转换场景</param> 
    public void JumpSceneFun(GameSystem.GameScene scene, bool isRequestSyncData, Func<bool> untilTrue = null)
    {
        if (isJumping) return;
        Time.timeScale = 1;
        loadingImg.DOPause();
        StartCoroutine(ShowTransitionEffect(scene, isRequestSyncData, untilTrue));
    }

    IEnumerator ShowTransitionEffect(GameSystem.GameScene scene, bool isRequestSyncData,Func<bool> untilTrue)
    {
        isJumping = true;
        IsCompleteLoading = false;
        if(isRequestSyncData)
        {
            isRequestingSaveFile = true;
            if (Arrangement == 0)
            {
                LoadSaveData.instance.LoadByJson();
                isRequestingSaveFile = false;
            }else ApiPanel.instance.SyncSaved(() => isRequestingSaveFile = false);
        }
        loadingImg.gameObject.SetActive(true);
        loadingImg.DOFade(1, fadeSpeed/2);

        yield return new WaitForSeconds(fadeSpeed);

        var tips = DataTable.Tips.RandomPick().Value;
        infoText.text = tips.Text;
        infoText.transform.GetChild(0).GetComponent<Text>().text = tips.Sign;

        loadingText.gameObject.SetActive(true);
        infoText.gameObject.SetActive(true);
        yield return new WaitWhile(() => isRequestingSaveFile);//这里阻塞下个场景初始化逻辑。等待存档加载完毕
        if (untilTrue != null) yield return new WaitUntil(untilTrue);
        asyncOp = SceneManager.LoadSceneAsync((int)scene, LoadSceneMode.Single);//异步加载场景，Single:不保留现有场景 
        yield return new WaitUntil(() => asyncOp.isDone);
        StartCoroutine(FadeTransitionEffect(fadeSpeed));
    }

    //隐藏 
    IEnumerator FadeTransitionEffect(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        loadingText.gameObject.SetActive(false);
        infoText.gameObject.SetActive(false);
        loadingImg.gameObject.SetActive(true);
        loadingImg.DOFade(0, fadeSpeed).OnComplete(() =>
        {
            loadingImg.gameObject.SetActive(false);
            IsCompleteLoading = true;
        });
    }

    /// <summary> 
    /// 添加或删除卡牌id到出战列表 
    /// </summary> 
    public bool EnlistCard(GameCard card, bool isAdd)
    {
        var cardType = (GameCardType) card.Type;
        var cardLimit = DataTable.PlayerLevelConfig[pyData.Level].CardLimit;
        List<int> cardList = null;
        switch (cardType)
        {
            case GameCardType.Hero:
                cardList = fightHeroId;
                break;
            case GameCardType.Tower:
                cardList = fightTowerId;
                break;
            case GameCardType.Trap:
                cardList = fightTrapId;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (isAdd)
        {
            if (TotalCardsEnlisted >= cardLimit)
            {
                ShowStringTips(DataTable.GetStringText(38));
                return false;
            }

            if (!cardList.Contains(card.CardId))
                cardList.Add(card.CardId);
        }
        else if (cardList.Contains(card.CardId)) cardList.Remove(card.CardId);

        card.IsFight = isAdd ? 1 : 0;
        return true;
    }


    [SerializeField]
    GameObject textTipsObj;     //文本提示obj  

    public int selectedBaYeEventId; //当前选择的霸业城池 
    public int selectedCity;
    public string mainSceneTips;
    private WarsDataClass _warsData = new WarsDataClass();
    private int[] guideObjsShowed = new int[7];
    


    /// <summary> 
    /// 场景底部文本提示 
    /// </summary> 
    /// <param name="str"></param> 
    public void ShowStringTips(string str)
    {
        if (!Application.isPlaying) return;
        textTipsObj.SetActive(false);
        textTipsObj.transform.GetComponent<Text>().text = str;
        textTipsObj.SetActive(true);
    }

    public bool ConsumeZhanLing(int amt = 1)
    {
        var force = WarForceMap[WarTypes.Baye];
        if (BaYeManager.instance.TradeZhanLing(force, amt * -1)) return true;
        ShowStringTips("战令不足以消费！");
        return false;
    }

    public void SaveBaYeWarEvent(int cityId)
    {
        var city = BaYeManager.instance.Map.Single(e => e.CityId == cityId);
        var index = baYe.data.FindIndex(d => d.CityId == cityId);
        var passes = city.PassedStages.Count(p => p);
        var pList = new bool[city.ExpList.Count].Select((_, i) => passes > i).ToArray();
        if (index == -1)
            baYe.data.Add(new BaYeCityEvent
            {
                CityId = cityId,
                EventId = city.EventId,
                WarIds = city.WarIds,
                ExpList = city.ExpList,
                PassedStages = pList
            });
        else
        {
            var saved = baYe.data[index];
            saved.ExpList = city.ExpList;
            saved.PassedStages = pList;
            saved.WarIds = city.WarIds;
            saved.EventId = city.EventId;
        }
        GamePref.SaveBaYe(baYe);
    }

    public void UpdateWarUnlockProgress(int totalStagesPass)
    {
        if (totalStagesPass > warsData.GetCampaign(selectedWarId).unLockCount)
            warsData.GetCampaign(selectedWarId).unLockCount = totalStagesPass;
        isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData(3);
    }

    public bool TryAddStamina(int sta)
    {
        Stamina.AddStamina(sta);
        pyData.Stamina += sta;
        if (pyData.Stamina < 0)
        {
            ApiPanel.instance.SyncSaved(null);
            ShowStringTips("体力不够了吗？，请确保有足够的体力执行。");
            return false;
            //throw new InvalidOperationException($"体力小于0! stamina = ({stamina})");
        }
        isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData(1);
        return true;
    }

    public void UpdateGameCards(TroopDto[] troops, GameCardDto[] gameCardList)
    {
        var cards = gameCardList.Select(GameCard.Instance)
            .Where(c => c.IsOwning())
            .GroupBy(c => (GameCardType) c.Type, c => c)
            .ToDictionary(c => c.Key, c => c.ToList());
        if (troops != null)
        {
            var enlisted = troops.SelectMany(t => t.EnList).ToList();
            foreach (var chip in enlisted.SelectMany(set =>
                cards[set.Key].Join(set.Value, c => c.CardId, i => i, (c, _) => c)))
                chip.IsFight = 1;
        }

        hstData.heroSaveData = cards.TryGetValue(GameCardType.Hero, out var heroCards) ? heroCards : new List<GameCard>();
        hstData.towerSaveData = cards.TryGetValue(GameCardType.Tower, out var towerCards) ? towerCards : new List<GameCard>();
        hstData.trapSaveData = cards.TryGetValue(GameCardType.Trap, out var trapCards) ? trapCards : new List<GameCard>();
    }

    public void SendTroopToWarApi()
    {
        //todo: 暂时霸业不请求Api
        if (WarType != WarTypes.Expedition)
        {
            WarReward = new WarReward(string.Empty, selectedWarId, 0);
            return;
        }

        var cards = hstData.heroSaveData.Concat(hstData.towerSaveData).Concat(hstData.trapSaveData)
            .Enlist(CurrentWarForceId).Select(c => c.ToDto()).ToList();
        var troopDto = new TroopDto
        {
            EnList = cards.GroupBy(c => c.Type, c => c.CardId).ToDictionary(c => c.Key, c => c.ToArray()),
            ForceId = CurrentWarForceId
        };
        //ApiPanel.instance.InvokeVb(SuccessAction, FailedAction, EventStrings.Req_TroopToCampaign,
        //    ViewBag.Instance().TroopDto(troopDto)
        //        .SetValues(selectedWarId, UIManager.instance.expedition.CurrentMode.Id));
        ApiPanel.instance.HttpCallVb(SuccessAction, FailedAction, EventStrings.Call_TroopToCampaign,
            DataBag.SerializeBag(EventStrings.Call_TroopToCampaign, 
                selectedWarId, 
                UIManager.instance.expedition.CurrentMode.Id,
                troopDto));
        return;

        void FailedAction(string msg)
        {
            ShowStringTips(msg);
            WarReward = new WarReward(string.Empty, selectedWarId, 0);
        }

        void SuccessAction(ViewBag vb)
        {
            WarReward = new WarReward(vb.Values[0].ToString(), selectedWarId, 0);
            var troop = vb.GetTroopDto();
            UpdateTroopEnlist(troop);
        }
    }

    public void UpdateTroopEnlist(TroopDto troop)
    {
        var isContainHero = troop.EnList.ContainsKey(GameCardType.Hero);
        var isContainTower = troop.EnList.ContainsKey(GameCardType.Tower);
        var isContainTrap = troop.EnList.ContainsKey(GameCardType.Trap);
        foreach (var h in hstData.heroSaveData.Where(h => h.GetForceId() == troop.ForceId))
        {
            h.IsFight = isContainHero
                ? troop.EnList[GameCardType.Hero].Any(id => h.CardId == id) ? 1 : 0
                : 0;
        }
        foreach (var t in hstData.towerSaveData.Where(h => h.GetForceId() == troop.ForceId))
        {
            t.IsFight = isContainTower
                ? troop.EnList[GameCardType.Tower].Any(id => t.CardId == id) ? 1 : 0
                : 0;
        }

        foreach (var t in hstData.trapSaveData.Where(h => h.GetForceId() == troop.ForceId))
        {
            t.IsFight = isContainTrap
                ? troop.EnList[GameCardType.Trap].Any(id => t.CardId == id) ? 1 : 0
                : 0;
        }
        RefreshEnlisted(troop.ForceId);
    }

    public void RefreshEnlisted(int forceId)
    {
        fightHeroId = hstData.heroSaveData.Enlist(forceId).Select(h=>h.CardId).ToList();
        fightTowerId = hstData.towerSaveData.Enlist(forceId).Select(t => t.CardId).ToList();
        fightTrapId = hstData.trapSaveData.Enlist(forceId).Select(t => t.CardId).ToList();
    }

    public void GenerateLocalStamina()
    {
        ResourceConfigTable staConfig = null;
        if (DataTable.ResourceConfig != null)
            staConfig = DataTable.ResourceConfig[2];
        stamina = new LocalStamina(pyData?.Stamina ?? 0, secsPerStamina, staConfig?.NewPlayerValue ?? 100,
            staConfig?.Limit ?? 200);
    }

    public IEnumerable<GameCard> GetCards(bool isAllForces)
    {
        var list = hstData.heroSaveData
            .Concat(hstData.towerSaveData)
            .Concat(hstData.trapSaveData).Where(c=>c.IsOwning());
        return isAllForces ? list : list.Where(c => c.GetInfo().ForceId <= UnlockForceId);
    }

    public IEnumerable<GameCardDto> GetLocalDtos()
    {
        return GenerateDtoList(hstData.heroSaveData)
            .Concat(GenerateDtoList(hstData.towerSaveData))
            .Concat(GenerateDtoList(hstData.trapSaveData)).ToArray();

        IEnumerable<GameCardDto> GenerateDtoList(IEnumerable<GameCard> list) => list
            .Where(c => c.IsOwning()).Select(c => new GameCardDto(c.CardId, c.Type, c.Level, c.Chips, c.Arouse,
                c.Deputy1Id, c.Deputy1Level,
                c.Deputy2Id, c.Deputy2Level,
                c.Deputy3Id, c.Deputy3Level,
                c.Deputy4Id, c.Deputy4Level));

    }

    public void SetStaminaBeforeWar(StaminaCost cost) => CurrentStaCost = cost;

    public int StaminaReturnFromLastWar()
    {
        if (CurrentStaCost == null) return 0;
        var cost = CurrentStaCost.TotalCost(WarReward.Chests.Count, true);
        return CurrentStaCost.Cost - cost;
    }

    public void UpdateCharacter(ICharacter cha)
    {
        Character = Character.Instance(cha);
    }

    public void NotifyDataUpdate()
    {
        switch (GameSystem.CurrentScene)
        {
            case GameSystem.GameScene.PreloadScene:
            case GameSystem.GameScene.StartScene:
                break;
            case GameSystem.GameScene.MainScene:
                UIManager.instance.UpdateMainSceneUi();
                break;
            case GameSystem.GameScene.WarScene:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void UpdateFreeAdTicket(int tickets)
    {
        pyData.AdPass = tickets;
        LoadSaveData.instance.SaveGameData(1);
        NotifyDataUpdate();
    }

    public void SetDto(PlayerDataDto playerData,
        CharacterDto character,
        int[] warChestList,
        string[] redeemedList,
        WarCampaignDto[] warCampaignList,
        GameCardDto[] gameCardList,
        TroopDto[] troops)
    {
        pyData = PlayerData.Instance(playerData);
        UpdateCharacter(character);
        GenerateLocalStamina();
        warsData.warUnlockSaveData = warCampaignList.Select(w => new UnlockWarCount
        {
            warId = w.WarId,
            isTakeReward = w.IsFirstRewardTaken,
            unLockCount = w.UnlockProgress
        }).ToList();
        UpdateGameCards(troops, gameCardList);
        gbocData.redemptionCodeGotList = redeemedList.ToList();
        gbocData.fightBoxs = warChestList.ToList();
        isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData();
    }
}