using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets;
using Assets.System.WarModule;
using Beebyte.Obfuscator;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class WarsUIManager : MonoBehaviour
{
    public static WarsUIManager instance;

    public Transform herosCardListTran;
    public ScrollRect herosCardListScrollRect;
    public bool isDragDelegated;
    public bool isDragDisable;
    public ChessboardVisualizeManager chessboardManager;
    [SerializeField] private PlayerInfoUis infoUis; //玩家信息UI
    [SerializeField] GameObject cityLevelObj; //主城信息obj

    [SerializeField] public HorizontalLayoutGroup PlayerCardsRack; //武将卡牌架

    //[SerializeField]
    //GameObject cardForWarListPres; //列表卡牌预制件
    [SerializeField] GuanQiaUi guanQiaPreObj; //关卡按钮预制件
    [SerializeField] Button operationButton; //关卡执行按钮
    [SerializeField] Text operationText; //关卡执行文字
    [SerializeField] GameObject upLevelBtn; //升级城池btn

    public Chessboard Chessboard;
    [SerializeField] SanXuanWindowUi SanXuanWindow;
    [SerializeField] WarQuizWindowUi QuizWindow; //答题
    [SerializeField] GenericWarWindow GenericWindow;
    [SerializeField] AboutCardUi aboutCardUi; //阵上卡牌详情展示位

    public WarMiniWindowUI gameOverWindow; //战役结束ui
    public Dictionary<int, GameStage> StagesMap { get; } = new Dictionary<int, GameStage>();
    [SerializeField] float percentReturnHp; //回春回血百分比

    public int baYeDefaultLevel = 3;
    public int cityLevel = 1; //记录城池等级
    public int goldForCity; //记录城池金币

    public int GoldForCity
    {
        get
        {
            if (PlayerDataForGame.instance.WarType == PlayerDataForGame.WarTypes.Baye)
                return PlayerDataForGame.instance.baYe.gold;
            return goldForCity;
        }
        set
        {
            goldForCity = value;
            if (PlayerDataForGame.instance.WarType != PlayerDataForGame.WarTypes.Baye) return;
            BaYeManager.instance.SetGold(value);
        }
    }

    private List<int> WarChests;

    int indexLastGuanQiaId; //记录上一个关卡id

    //int passedGuanQiaNums;  //记录通过的关卡数
    private bool[] PassCheckpoints;

    public List<FightCardData> playerCardsDatas; //我方卡牌信息集合

    public float cardMoveSpeed; //卡牌移动速度

    public AudioClip[] audioClipsFightEffect;
    [Range(0,1)]
    public float[] audioVolumeFightEffect;

    public AudioClip[] audioClipsFightBack;
    [Range(0,1)]
    public float[] audioVolumeFightBack;

    [SerializeField] AudioSource audioSource;

    [SerializeField] Image chessboardBg;

    [SerializeField] Text levelIntroText; //关卡介绍文本
    [SerializeField] Text battleNameText; //战役名文本
    [SerializeField] Text battleScheduleText; //战役进度文本
    int nowGuanQiaIndex; //记录当前关卡进度

    bool isGettingStage; //记录是否进入了关卡
    [SerializeField] private ChessboardVisualizeManager ChessboardManager;
    [SerializeField] private Text heroEnlistText; //武将上阵文本

    #region EventTypes

    private enum EventTypes
    {
        初始 = 0, //通用
        战斗 = 1, //战斗，注意！有很多数字都代表战斗
        故事 = 2, //故事
        答题 = 3, //答题
        回春 = 4, //回春
        奇遇 = 5, //奇遇
        交易 = 6,
    }

    private static int[] BattleEventTypeIds = { 1, 7, 8, 9, 10, 11, 12 };

    //判断是否是战斗关卡
    private static bool IsBattle(int eventId) => BattleEventTypeIds.Contains(eventId);

    private static Dictionary<int, EventTypes> NonBattleEventTypes = new Dictionary<int, EventTypes>
    {
        { 2, EventTypes.故事 },
        { 3, EventTypes.答题 },
        { 4, EventTypes.回春 },
        { 5, EventTypes.奇遇 },
        { 6, EventTypes.交易 }
    };

    private static EventTypes GetEvent(int eventTypeId)
    {
        if (IsBattle(eventTypeId)) return EventTypes.战斗;
        if (NonBattleEventTypes.TryGetValue(eventTypeId, out var eventValue)) return eventValue;
        throw XDebug.Throw<WarsUIManager>($"无效事件 = {eventTypeId}!");
    }

    // 上一个关卡类型
    private EventTypes currentEvent = EventTypes.初始;

    #endregion

    private GameResources GameResources => GameResources.Instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        isPointMoving = false;
    }

    public void Init()
    {
        StartCoroutine(Initialize());
        if (!EffectsPoolingControl.instance.IsInit) EffectsPoolingControl.instance.Init();
        chessboardManager.Init();
        chessboardManager.OnRoundBegin += OnChessRoundBegin;
        chessboardManager.OnResourceUpdate.AddListener(OnResourceUpdate);
        chessboardManager.OnCardDefeated.AddListener(OnCardDefeated);
        chessboardManager.OnGameSet.AddListener(FinalizeWar);
        speedBtn.onClick.AddListener(()=>ChangeTimeScale());
    }

    IEnumerator Initialize()
    {
        if (PlayerDataForGame.instance.WarType == PlayerDataForGame.WarTypes.Expedition &&
            string.IsNullOrWhiteSpace(PlayerDataForGame.instance.WarReward.Token))
        {
            ExpeditionFinalize(false);
            yield return null;
        }

        switch (PlayerDataForGame.instance.WarType)
        {
            //战斗金币
            case PlayerDataForGame.WarTypes.Expedition:
                GoldForCity = PlayerDataForGame.instance.zhanYiColdNums;
                break;
            case PlayerDataForGame.WarTypes.Baye:
                GoldForCity = PlayerDataForGame.instance.baYe.gold;
                cityLevel = baYeDefaultLevel;
                break;
            case PlayerDataForGame.WarTypes.None:
                throw XDebug.Throw<WarsUIManager>($"未确认战斗类型[{PlayerDataForGame.WarTypes.None}]，请在调用战斗场景前预设战斗类型。");
            default:
                throw new ArgumentOutOfRangeException();
        }

        WarChests = new List<int>();
        indexLastGuanQiaId = 0;
        PassCheckpoints = new bool[DataTable.War[PlayerDataForGame.instance.selectedWarId].CheckPoints];
        playerCardsDatas = new List<FightCardData>();
        cardMoveSpeed = 2f;
        nowGuanQiaIndex = 0;
        point0Pos = point0Tran.position;
        point1Pos = point1Tran.position;
        point2Pos = point2Tran.position;

        Input.multiTouchEnabled = false; //限制多指拖拽
        isDragDelegated = false;
        isGettingStage = false;
        //------------Awake----------------//
        PlayerDataForGame.instance.lastSenceIndex = 2;
        SanXuanWindow.Init();
        SanXuanWindow.AdConsume.SetCallBackAction(success =>
        {
            if (success)
                UpdateQiYuInventory();
        }, _ => UpdateQiYuInventory(), ViewBag.Instance().SetValue(0), true);
        QuizWindow.Init();
        //adRefreshBtn.onClick.AddListener(WatchAdForUpdateQiYv);
        gameOverWindow.Init();
        GenericWindow.Init();
        yield return new WaitUntil(() => PlayerDataForGame.instance.WarReward != null);
        InitMainUiShow();

        InitCardListShow();

        InitState();
    }

    //初始化关卡
    private void InitState()
    {
        currentEvent = EventTypes.初始;
        //尝试展示指引
        //ShowOrHideGuideObj(0, true);
        var checkpointId = DataTable.War[PlayerDataForGame.instance.selectedWarId].BeginPoint;
        var checkpoint = DataTable.Checkpoint[checkpointId];
        InstanceStage(checkpoint);
        PreInitNextStage(new[] { checkpoint.Id });
    }

    private GameStage InstanceStage(CheckpointTable checkPoint)
    {
        var stage = new GameStage();
        if (!StagesMap.ContainsKey(checkPoint.Id))
            StagesMap.Add(checkPoint.Id, stage);
        var battle = DataTable.BattleEvent[checkPoint.BattleEventTableId];
        var index = Random.Range(0, battle.EnemyTableIndexes.Length); //敌人随机库抽取一个id
        stage.Checkpoint = checkPoint;
        stage.BattleEvent = battle;
        stage.RandomId = index;
        var isBattle = IsBattle(stage.Checkpoint.EventType);
        if (isBattle)
            stage.BattleEvent = DataTable.BattleEvent[checkPoint.BattleEventTableId];
        return stage;
    }

    int selectParentIndex = -1;

    //选择某个父级关卡初始化子集关卡
    private void SelectStage(GameStage stage)
    {
        var checkpointId = stage.Checkpoint.Id;
        UpdateLevelInfoText(checkpointId);
        int randArtImg = Random.Range(0, 25); //随机艺术图
        if (selectParentIndex != checkpointId)
        {
            point0Tran.gameObject.SetActive(false);
        }

        indexLastGuanQiaId = checkpointId;

        for (int i = 0; i < point0Tran.childCount; i++)
        {
            Destroy(point0Tran.GetChild(i).gameObject);
        }

        //最后一关
        var nexPoints = DataTable.Checkpoint[checkpointId].Next;
        if (nowGuanQiaIndex >= DataTable.War[PlayerDataForGame.instance.selectedWarId].CheckPoints)
        {
            nexPoints = new int[0];
        }

        List<Transform> childsTranform = new List<Transform>();
        GameStage nextStage = null;
        for (int i = 0; i < nexPoints.Length; i++)
        {
            int guanQiaId = nexPoints[i];
            var checkpoint = DataTable.Checkpoint[guanQiaId];
            nextStage = !StagesMap.ContainsKey(checkpoint.Id) ? InstanceStage(checkpoint) : StagesMap[checkpoint.Id];
            var ui = Instantiate(guanQiaPreObj, point0Tran);
            ui.Set(new Vector3(0.8f, 0.8f, 1), nextStage, IsBattle(checkpoint.EventType));
            childsTranform.Add(ui.transform);
        }

        if (selectParentIndex != checkpointId)
        {
            selectParentIndex = checkpointId;

            //关卡艺术图
            levelIntroText.transform.parent.GetComponent<Image>().DOFade(0, 0.5f).OnComplete(delegate()
            {
                levelIntroText.transform.parent.GetComponent<Image>().sprite = GameResources.ArtWindow[randArtImg];
                levelIntroText.transform.parent.GetComponent<Image>().DOFade(1, 1f);
            });

            pointWays.SetActive(false);
            //展示云朵动画
            ShowClouds();
            StartCoroutine(DisplayPointAfterSecs(2f));
        }
    }

    IEnumerator DisplayPointAfterSecs(float startTime)
    {
        yield return new WaitForSeconds(startTime / 2);
        //显示子关卡
        point0Tran.gameObject.SetActive(true);
    }

    //更新关卡介绍显示
    private void UpdateLevelInfoText(int guanQiaId)
    {
        levelIntroText.DOPause();
        levelIntroText.text = "";
        levelIntroText.color = new Color(levelIntroText.color.r, levelIntroText.color.g, levelIntroText.color.b, 0);
        levelIntroText.DOFade(1, 3f);
        levelIntroText.DOText(("\u2000\u2000\u2000\u2000" + DataTable.Checkpoint[guanQiaId].Intro), 3f)
            .SetEase(Ease.Linear).SetAutoKill(false);
    }

    //战役结束
    public void ExpeditionFinalize(bool isWin)
    {
        Time.timeScale = 1;
        var reward = PlayerDataForGame.instance.WarReward;
        if (isWin)
        {
            //通关不返还体力
            PlayerDataForGame.instance.WarReward.Stamina = 0;
        }

        //如果是霸业
        if (PlayerDataForGame.instance.WarType == PlayerDataForGame.WarTypes.Baye)
        {
            /**
             * -判断.上一个场景是不是战斗。
             * -判断.第几个霸业战斗
             * -判断.霸业经验奖励是否已被领取
             * 1.加经验
             */
            if (isWin)
            {
                var baYeMgr = BaYeManager.instance;
                var baYe = PlayerDataForGame.instance.baYe;
                if (baYeMgr.CurrentEventType == BaYeManager.EventTypes.City)
                {
                    var cityEvent = baYe.data.Single(f => f.CityId == PlayerDataForGame.instance.selectedCity);
                    var warIndex = cityEvent.WarIds.IndexOf(PlayerDataForGame.instance.selectedWarId);
                    if (!cityEvent.PassedStages[warIndex]) //如果过关未被记录
                    {
                        var exp = cityEvent.ExpList[warIndex]; //获取相应经验值
                        cityEvent.PassedStages[warIndex] = true;
                        PlayerDataForGame.instance.BaYeManager.AddExp(cityEvent.CityId, exp); //给玩家加经验值
                        PlayerDataForGame.instance.mainSceneTips = $"获得经验值：{exp}";
                        reward.BaYeExp = exp;
                    }
                }
                else
                {
                    var sEvent = baYeMgr.CachedStoryEvent;
                    reward.Gold = sEvent.GoldReward; //0
                    reward.BaYeExp = sEvent.ExpReward; //1
                    reward.YuanBao = sEvent.YuanBaoReward; //3
                    reward.YuQue = sEvent.YvQueReward; //4
                    var ling = sEvent.ZhanLing.First();
                    reward.Ling.Trade(ling.Key, ling.Value);
                    baYeMgr.OnBayeStoryEventReward(baYeMgr.CachedStoryEvent);
                }
            }

            //霸业的战斗金币传到主城
            PlayerDataForGame.instance.baYe.gold = GoldForCity;
        }
        else if (PlayerDataForGame.instance.WarType == PlayerDataForGame.WarTypes.Expedition)
        {
            PlayerDataForGame.instance.UpdateWarUnlockProgress(PassCheckpoints.Count(isPass => isPass));
            var ca = PlayerDataForGame.instance.warsData.GetCampaign(reward.WarId);
            //if (treasureChestNums > 0) rewardMap.Trade(2, treasureChestNums); //index2是宝箱图
            var viewBag = ViewBag.Instance()
                .WarCampaignDto(new WarCampaignDto
                    { IsFirstRewardTaken = ca.isTakeReward, UnlockProgress = ca.unLockCount, WarId = ca.warId })
                .SetValues(reward.Token, reward.Chests);

            ApiPanel.instance.Invoke(vb =>
                {
                    var player = vb.GetPlayerDataDto();
                    var campaign = vb.GetWarCampaignDto();
                    var chests = vb.GetPlayerWarChests();
                    PlayerDataForGame.instance.gbocData.fightBoxs.AddRange(chests);
                    var war = PlayerDataForGame.instance.warsData.warUnlockSaveData
                        .First(c => c.warId == campaign.WarId);
                    war.unLockCount = campaign.UnlockProgress;
                    ConsumeManager.instance.SaveChangeUpdatePlayerData(player, 0);
                }, PlayerDataForGame.instance.ShowStringTips,
                EventStrings.Req_WarReward, viewBag);

            GameSystem.Instance.ShowStaminaEffect = true;
        }

        gameOverWindow.Show(reward, PlayerDataForGame.instance.WarType == PlayerDataForGame.WarTypes.Baye);

        PlayerDataForGame.instance.isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData(3);
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
    }

    //初始化父级关卡
    private void PreInitNextStage(int[] checkPoints)
    {
        if (PassCheckpoints.All(isPass => isPass))
        {
            ExpeditionFinalize(true); //通过所有关卡
            return;
        }

        selectParentIndex = -1;

        for (int i = 0; i < point1Tran.childCount; i++)
        {
            Destroy(point1Tran.GetChild(i).gameObject);
        }

        for (int i = 0; i < checkPoints.Length; i++)
        {
            var cId = checkPoints[i];

            var stage = StagesMap.ContainsKey(cId) ? StagesMap[cId] : InstanceStage(DataTable.Checkpoint[cId]);
            //下个关卡点
            var ui = Instantiate(guanQiaPreObj, point1Tran);
            ui.Set(Vector3.one, stage, IsBattle(stage.Checkpoint.EventType));
            ui.Button.onClick.AddListener(() =>
            {
                operationText.text = IsBattle(stage.Checkpoint.EventType)
                    ? DataTable.GetStringText(53)
                    : DataTable.GetStringText(54);
                operationButton.gameObject.SetActive(true);
                SelectOneGuanQia(ui);
                SelectStage(stage);
                OnCheckpointInvoke(stage.Checkpoint.Id);
            });
        }

        StartCoroutine(SelectPointAfterSecs(0));
    }

    IEnumerator SelectPointAfterSecs(float startTime)
    {
        yield return new WaitForSeconds(startTime);
        //默认选择第一个
        point1Tran.GetComponentInChildren<Button>().onClick.Invoke();
    }

    Image indexSelectGuanQia;

    private void SelectOneGuanQia(GuanQiaUi chooseObj)
    {
        AudioController0.instance.RandomPlayGuZhengAudio();

        operationButton.GetComponent<Button>().onClick.RemoveAllListeners();

        if (indexSelectGuanQia != null)
        {
            indexSelectGuanQia.gameObject.SetActive(false);
        }

        chooseObj.SelectedImg.gameObject.SetActive(true);
        indexSelectGuanQia = chooseObj.SelectedImg;
    }

    /// <summary>
    /// 进入不同关卡
    /// </summary>
    private void OnCheckpointInvoke(int checkpointId)
    {
        var stage = StagesMap[checkpointId];
        operationButton.onClick.AddListener(InvokeToTheNextStage);

        void InvokeToTheNextStage()
        {
            if (isPointMoving || isGettingStage) return;
            var eventType = GetEvent(stage.Checkpoint.EventType);
            switch (eventType)
            {
                case EventTypes.战斗:
                    GoToBattle(stage);
                    break;
                case EventTypes.答题:
                    GoToQuiz();
                    break;
                case EventTypes.回春:
                    GoToRecovery();
                    break;
                case EventTypes.奇遇:
                    GoToSanXuan(false);
                    break;
                case EventTypes.交易:
                    GoToSanXuan(true);
                    break;
                case EventTypes.故事:
                case EventTypes.初始:
                default:
                    throw new ArgumentOutOfRangeException($"事件异常 = {eventType}");
            }

            isGettingStage = true;
        }
    }

    [SerializeField] Text speedBtnText;
    [SerializeField] private Button speedBtn;

    private const string Multiply = "×";

    //改变游戏速度
    public void ChangeTimeScale(int scale = 0,bool save = true)
    {
        var warScale = GamePref.PrefWarSpeed;
        if (scale <= 0)
        {
            warScale *= 2;
            if (warScale > 2)
                warScale = 1;
        }
        else warScale = scale;
        if(save) GamePref.SetPrefWarSpeed(warScale);
        Time.timeScale = warScale;
        speedBtnText.text = Multiply + warScale;
    }

    #region 棋子暂存区与棋盘区

    /**
     * 主要目的：
     * 1.当棋子拖动摆放的时候，记录在暂存区，因为棋子还可以随意提取，在按下开始游戏之前，是不确定的。
     * 2.当棋局结束的时候，将残余的棋子提取以便在新棋局重复使用。(维持当前血量)
     * 3.统计当前玩家手上已上阵的棋子。(死亡忽略)
     */

    //记录当前在棋盘上的卡牌与UI，棋局结束后会统计并销毁死亡的卡牌
    private Dictionary<FightCardData, WarGameCardUi> PlayerScope { get; set; } =
        new Dictionary<FightCardData, WarGameCardUi>();
    //记录暂时摆放的卡牌与位置，直到点击开始战斗的时候会把卡牌记录到棋盘上
    public Dictionary<FightCardData, WarGameCardUi> TempScope { get; private set; } =
        new Dictionary<FightCardData, WarGameCardUi>();
    private void OnCardDefeated(FightCardData defeatedCard)
    {
        if (!defeatedCard.IsPlayer ||
            defeatedCard.CardType == GameCardType.Base) return;//老巢不提取棋子
        var ui = PlayerScope[defeatedCard];
        PlayerScope.Remove(defeatedCard);
        Destroy(ui);
        UpdateHeroEnlistText();
    }

    private bool OnChessRoundBegin()
    {
        //从暂存区，交接到棋盘区
        foreach (var tmp in TempScope.ToDictionary(c => c.Key, c => c.Value))
        {
            var card = tmp.Key;
            var ui = tmp.Value;
            ui.DragDisable();
            ui.transform.SetParent(Chessboard.transform);
            PlayerScope.Add(card, ui);
            TempScope.Remove(card);
            ui.gameObject.SetActive(false);
            chessboardManager.SetPlayerChess(card);
            chessboardManager.InstanceChessmanUi(card);
        }
        return true;
    }
    //让所有棋子回到暂存区
    private void ResetChessboardPlacingScope()
    {
        foreach (var gc in PlayerScope.ToDictionary(c => c.Key, c => c.Value))
        {
            var card = gc.Key;
            var ui = gc.Value;
            var chessman = card.cardObj;
            PlayerScope.Remove(card);
            if (card.Status.IsDeath)//死亡棋子直接销毁
            {
                Destroy(ui.gameObject);
                continue;
            }

            TempScope.Add(card, ui);
            ui.transform.position = chessman.transform.position; //被改位置重置
            var hp = Math.Min((int)(card.Status.Hp + (card.Status.Hp * card.Info.GameSetRecovery * 0.01f)),
                card.Status.MaxHp);
            card.Status.SetHp(hp);
            //上面把ui与卡分离了。所以更新UI的时候需要手动改血量值
            ui.War.UpdateHpUi(card.Status.HpRate);
            ui.gameObject.SetActive(true);
            ui.DragComponent.UpdateSelectionUi();
            UpdateHeroEnlistText();
        }
    }
    /// <summary>
    /// 根据棋子PosIndex标记更新暂存区
    /// </summary>
    /// <param name="card"></param>
    public void UpdateCardTempScope(FightCardData card)
    {
        if (!TempScope.ContainsKey(card))
            TempScope.Add(card, card.cardObj);

        if (card.Pos > -1)
        {
            var chessPos = Chessboard.GetChessPos(card.Pos, true);
            card.cardObj.transform.position = chessPos.transform.position;
            EffectsPoolingControl.instance.GetSparkEffect(
                Effect.OnChessboardEventEffect(Effect.ChessboardEvent.PlaceCardToBoard), card.cardObj.transform);
        }
        else
        {
            TempScope.Remove(card);
            card.cardObj.transform.position = Vector3.zero;
        }

        UpdateHeroEnlistText();
    }

    #endregion
    private List<int> TempChest { get; } = new List<int>();
    private int TempGold { get; set; }
    private WarGameCardUi playerBaseObj { get; set; }
    private WarGameCardUi enemyBaseObj { get; set; }

    private void OnResourceUpdate(bool isChallenger, int id, int value)
    {
        if (!isChallenger) return;
        if (id == -1) TempGold += value;
        if (id >= 0)
            for (int i = 0; i < value; i++)
                TempChest.Add(id);
    }
    public void FinalizeWar(bool isChallengerWin)
    {
        ResetChessboardPlacingScope();
        if (!isChallengerWin)
        {
            ExpeditionFinalize(false);
            return;
        }
        StartCoroutine(OnPlayerWin());
    }

    IEnumerator OnPlayerWin()
    {
        WarChests.AddRange(TempChest);
        UpdateInfoUis();
        ChangeTimeScale(1, false);
        if (TempChest.Count > 0)
        {
            var warReward = PlayerDataForGame.instance.WarReward;
            foreach (var id in TempChest)
                warReward.Chests.Add(id);
        }
        yield return new WaitForSeconds(2);
        GenericWindow.SetReward(TempGold, TempChest.Count);
        GoldForCity += TempGold;
        UpdateInfoUis();
        PlayerDataForGame.instance.WarReward.Chests.AddRange(TempChest);
        TempGold = 0;
        TempChest.Clear();
    }

    public void InitChessboard(GameStage stage)
    {
        if(playerBaseObj!=null && playerBaseObj.gameObject) Destroy(playerBaseObj.gameObject);
        if(enemyBaseObj!=null && enemyBaseObj.gameObject) Destroy(enemyBaseObj.gameObject);
        chessboardManager.NewGame();
        var playerBase = FightCardData.PlayerBaseCard(PlayerDataForGame.instance.pyData.Level, cityLevel);
        var enemyBase = FightCardData.BaseCard(false, stage.BattleEvent.BaseHp, 1);

        var battle = stage.BattleEvent;
        var enemyRandId = stage.BattleEvent.EnemyTableIndexes[stage.RandomId];
        var enemyCards = new List<ChessCard>();
        //随机敌人卡牌
        if (battle.IsStaticEnemies > 0)
        {
            var enemies = DataTable.StaticArrangement[enemyRandId].Poses();
            //固定敌人卡牌
            for (int i = 0; i < 20; i++)
            {
                var chessman = enemies[i];
                if (chessman == null) continue;
                if (chessman.Star <= 0)
                    throw new InvalidOperationException(
                        $"卡牌初始化异常，[{chessman.CardId}.{chessman.CardType}]等级为{chessman.Star}!");
                //var card = CreateEnemyFightUnit(i,1, true, chessman);
                //PlaceCardOnBoard(card, i, false);
                enemyCards.Add(ChessCard.Instance(chessman.CardId, chessman.CardType, chessman.Star, i));

            }

        }
        else
        {
            var enemies = DataTable.Enemy[enemyRandId].Poses();
            for (int i = 0; i < 20; i++)
            {
                var enemyId = enemies[i];
                if (enemyId == 0) continue;
                var enemy = DataTable.EnemyUnit[enemyId];
                var cardType = enemy.CardType;
                ChessCard chessCard;
                if (enemy.CardType >= 0)
                {
                    var card = GameCardInfo.RandomPick((GameCardType)enemy.CardType, enemy.Rarity);
                    var level = enemy.Star;
                    chessCard = ChessCard.Instance(card.Id, cardType, level, i);
                }
                else
                {
                    var trap = DataTable.Trap[enemy.Rarity];
                    chessCard = ChessCard.Instance(trap.Id, GameCardType.Trap, enemy.Star, i);
                }
                enemyCards.Add(chessCard);
            }
        }

        foreach (var card in chessboardManager.SetEnemyChess(enemyBase, enemyCards.ToArray()))
            chessboardManager.InstanceChessmanUi(card);
        chessboardManager.SetPlayerBase(playerBase);
        chessboardManager.InstanceChessmanUi(playerBase);
        enemyBaseObj = enemyBase.cardObj;
        playerBaseObj = playerBase.cardObj;
        //调整游戏速度
        var speed = GamePref.PrefWarSpeed;
        Time.timeScale = speed;
        speedBtnText.text = Multiply + speed;

        Chessboard.gameObject.SetActive(true);
    }

    /// <summary>
    /// 进入战斗
    /// </summary>
    private void GoToBattle(GameStage stage)
    {
        var checkPoint = stage.Checkpoint;
        currentEvent = EventTypes.战斗;
        PlayAudioClip(21);
        chessboardBg.sprite = GameResources.BattleBG[checkPoint.BattleBG];
        int bgmIndex = checkPoint.BattleBGM;
        AudioController1.instance.isNeedPlayLongMusic = true;
        AudioController1.instance.ChangeAudioClip(audioClipsFightBack[bgmIndex], audioVolumeFightBack[bgmIndex]);
        AudioController1.instance.PlayLongBackMusInit();
        InitChessboard(stage);
    }

    /// <summary>
    /// 进入答题
    /// </summary>
    private void GoToQuiz()
    {
        currentEvent = EventTypes.答题;
        PlayAudioClip(19);

        QuizWindow.Show();
        var pick = DataTable.Quest.Values.Select(q => new WeightElement { Id = q.Id, Weight = q.Weight })
            .PickOrDefault();
        var quest = DataTable.Quest[pick.Id];
        QuizWindow.SetQuiz(quest, OnAnswerQuiz);
    }

    // 进入奇遇或购买
    private void GoToSanXuan(bool isTrade)
    {
        currentEvent = isTrade ? EventTypes.交易 : EventTypes.奇遇;
        //尝试关闭指引
        //ShowOrHideGuideObj(0, false);

        //ShowOrHideGuideObj(1, true);

        PlayAudioClip(19);

        InitializeQYOrSp(isTrade);
        SanXuanWindow.Show(isTrade);
        //eventsWindows[3].SetActive(true);
    }

    // 进入回血事件
    private void GoToRecovery()
    {
        currentEvent = EventTypes.回春;
        PlayAudioClip(19);
        GenericWindow.SetRecovery(DataTable.GetStringText(55));
        foreach (var itm in TempScope)
        {
            var ui = itm.Value;
            var card = itm.Key;
            card.ResetHp(card.MaxHitPoint);
            ui.War.UpdateHpUi(card.Status.HpRate);
        }
    }

    [SerializeField] Transform point0Tran; //子关卡Transform
    [SerializeField] Transform point1Tran; //父关卡Transform
    [SerializeField] Transform point2Tran; //通过后的关卡Transform
    [SerializeField] GameObject warCityCloudsObj; //子关卡遮盖云朵

    private Vector3 point0Pos;
    private Vector3 point1Pos;
    private Vector3 point2Pos;

    private bool isPointMoving;

    [SerializeField] GameObject pointWays;

    //通关关卡转换的动画表现
    private void TongGuanCityPointShow()
    {
        isPointMoving = true;

        point0Tran.gameObject.SetActive(false);
        point1Tran.position = point0Pos;

        point1Tran.DOMove(point1Pos, 1.5f).SetEase(Ease.Unset).OnComplete(delegate()
        {
            isPointMoving = false;

            point0Tran.gameObject.SetActive(true);
        });
    }

    //父关卡通路子关卡
    private void TheWayToTheChildPoint(Transform parentPoint, List<Transform> childPoints)
    {
        pointWays.SetActive(true);

        List<GameObject> wayObjsList = new List<GameObject>();

        for (int i = 0; i < pointWays.transform.childCount; i++)
        {
            GameObject obj = pointWays.transform.GetChild(i).gameObject;
            wayObjsList.Add(obj);
            if (obj.activeSelf)
                obj.SetActive(false);
        }

        for (int i = 0; i < childPoints.Count; i++)
        {
            if (childPoints[i] != null)
            {
                wayObjsList[i].transform.position = parentPoint.position;
                wayObjsList[i].transform.DOPause();
                wayObjsList[i].SetActive(true);

                Vector3[] points = GetVector3Points(parentPoint.position, childPoints[i].position);

                wayObjsList[i].transform.DOPath(points, waySpeedFlo).SetEase(Ease.Unset);
            }
        }
    }

    [SerializeField] float waySpeedFlo; //通路时长

    [SerializeField] float randomChaZhi; //随机插值

    [SerializeField] int pointNums; //中途点数

    //返回两点之间的随机路径点
    private Vector3[] GetVector3Points(Vector3 point0, Vector3 point1)
    {
        Vector3[] wayPoints = new Vector3[pointNums];
        float floX = (point1.x - point0.x) /
                     wayPoints.Length; // + Random.Range(-randomInterpolation, randomInterpolation);
        float floY = (point1.y - point0.y) / wayPoints.Length;
        for (int i = 0; i < wayPoints.Length - 1; i++)
        {
            Vector3 vec = new Vector3();
            vec.x = point0.x + floX * (i + 1) + Random.Range(-randomChaZhi, randomChaZhi);
            vec.y = point0.y + floY * (i + 1);
            vec.z = point0.z;
            wayPoints[i] = vec;
        }

        wayPoints[wayPoints.Length - 1] = point1;
        return wayPoints;
    }

    //展示关卡前进的云朵动画
    private void ShowClouds()
    {
        if (PassCheckpoints.All(isPass => isPass)) return;
        if (warCityCloudsObj.activeInHierarchy)
        {
            warCityCloudsObj.SetActive(false);
        }

        warCityCloudsObj.SetActive(true);
    }

    /// <summary>
    /// 通关
    /// </summary>
    public void PassStage()
    {
        if (isPointMoving)
            return;

        isGettingStage = false;
        for (var i = 0; i < PassCheckpoints.Length; i++)
        {
            var passCheckpoint = PassCheckpoints[i];
            if (passCheckpoint) continue;
            PassCheckpoints[i] = true;
            break;
        }

        PlayAudioClip(13);

        UpdateBattleSchedule();
        PreInitNextStage(DataTable.Checkpoint[indexLastGuanQiaId].Next);
        TongGuanCityPointShow();
        //关闭所有战斗事件的物件
        if (Chessboard.gameObject.activeSelf)
        {
            Chessboard.gameObject.SetActive(false);
            AudioController1.instance.ChangeBackMusic();
        }

        GenericWindow.Off();
        QuizWindow.Off();
        SanXuanWindow.Off();
    }

    private class WeightElement : IWeightElement
    {
        public int Id { get; set; }
        public int Weight { get; set; }
    }

    //关闭三选的附加方法
    public void CloseSanXuanWin() => SanXuanWindow.ResetUi();

    int updateShopNeedGold; //商店刷新所需金币

    //刷新商店添加金币
    public void AddUpdateShoppingGold()
    {
        bool isSuccessed = UpdateShoppingList(updateShopNeedGold);
        if (isSuccessed)
            updateShopNeedGold++;
        SanXuanWindow.RefreshText.text = updateShopNeedGold.ToString();
    }

    /// <summary>
    /// 刷新商店列表
    /// </summary>
    /// <param name="refreshCost">刷新所需金币</param>
    [Skip]
    private bool UpdateShoppingList(int refreshCost = 0)
    {
        if (refreshCost != 0)
        {
            PlayAudioClip(13);
        }

        if (refreshCost != 0 && refreshCost > GoldForCity)
        {
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(56));
            return false;
        }

        var sanXuan = SanXuanWindow;
        sanXuan.SetTrade(refreshCost == 0 ? updateShopNeedGold : refreshCost + 1);
        GoldForCity -= refreshCost;
        UpdateInfoUis();
        for (int i = 0; i < sanXuan.GameCards.Length; i++)
        {
            var pick = DataTable.Mercenary.Values.Select(m => new WeightElement { Id = m.Id, Weight = m.Weight })
                .PickOrDefault().Id;
            var mercenary = DataTable.Mercenary[pick];
            int btnIndex = i;
            var ui = sanXuan.GameCards[i];
            var info = GenerateCard(i, ui, mercenary);
            //广告概率
            if (Random.Range(0, 100) < 25)
            {
                ui.SetAd(success =>
                {
                    if (!success) return;
                    GetOrBuyCards(true, 0, ui.Card, info, btnIndex);
                    PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(57));
                });
                continue;
            }

            ui.SetPrice(mercenary.Cost, () =>
                GetOrBuyCards(true, mercenary.Cost, ui.Card, info, btnIndex));
        }

        return true;
    }

    private GameCardInfo GenerateCard(int index, TradableGameCardUi ui, MercenaryTable mercenary)
    {
        var cardType = (GameCardType)mercenary.Produce.CardType;
        var cardRarity = mercenary.Produce.Rarity;
        var cardLevel = mercenary.Produce.Star;
        var card = RandomPickFromRareClass(cardType, cardRarity);
        ui.SetGameCard(GameCard.Instance(card.Id, mercenary.Produce.CardType, cardLevel));
        ui.OnClickAction.RemoveAllListeners();
        ui.OnClickAction.AddListener(() => OnClickToShowShopInfo(index, card.About));
        return card;
    }

    //刷新奇遇商品
    private void UpdateQiYuInventory()
    {
        var sanXuan = SanXuanWindow;
        var pick = DataTable.Mercenary.Values.Select(m => new WeightElement { Id = m.Id, Weight = m.Weight })
            .PickOrDefault().Id;
        var mercenary = DataTable.Mercenary[pick];
        for (int i = 0; i < sanXuan.GameCards.Length; i++)
        {
            var index = i;
            var ui = sanXuan.GameCards[i];
            var info = GenerateCard(i, ui, mercenary);
            ui.SetPrice(0, () => GetOrBuyCards(false, 0, ui.Card, info, index));
        }
    }

    // 匹配稀有度的颜色
    private Color GetNameColor(int rarity)
    {
        Color color = new Color();
        switch (rarity)
        {
            case 1:
                color = ColorDataStatic.name_gray;
                break;
            case 2:
                color = ColorDataStatic.name_green;
                break;
            case 3:
                color = ColorDataStatic.name_blue;
                break;
            case 4:
                color = ColorDataStatic.name_purple;
                break;
            case 5:
                color = ColorDataStatic.name_orange;
                break;
            case 6:
                color = ColorDataStatic.name_red;
                break;
            case 7:
                color = ColorDataStatic.name_black;
                break;
            default:
                color = ColorDataStatic.name_gray;
                break;
        }

        return color;
    }

    //展示三选单位个体信息
    private void OnClickToShowShopInfo(int btnIndex, string text)
    {
        //ShowOrHideGuideObj(1, false);

        var sanXuan = SanXuanWindow;
        for (var i = 0; i < sanXuan.GameCards.Length; i++)
        {
            var ui = sanXuan.GameCards[i];
            ui.SetSelect(i == btnIndex);
        }

        sanXuan.ShowInfo(text);
        //if (!shopInfoObj.activeSelf)
        //{
        //    infoText.text = "";
        //    Image infoImg = shopInfoObj.GetComponentInChildren<Image>();
        //    infoImg.color = new Color(1f, 1f, 1f, 0f);
        //    shopInfoObj.SetActive(true);
        //    infoImg.DOFade(1, 0.2f);
        //}
    }

    //奇遇或商店界面初始化
    private void InitializeQYOrSp(bool isBuy)
    {
        var ui = SanXuanWindow;
        if (isBuy)
        {
            //重置刷新商店所需金币
            updateShopNeedGold = 2;
            ui.SetTrade(updateShopNeedGold);
            UpdateShoppingList();
        }
        else
        {
            //奇遇刷新按钮
            ui.SetRecruit();
            UpdateQiYuInventory();
        }
    }

    //获得或购买三选物品
    private void GetOrBuyCards(bool isBuy, int cost, GameCard card, GameCardInfo info, int btnIndex)
    {
        var sanXuan = SanXuanWindow;
        var ui = sanXuan.GameCards[btnIndex];
        if (isBuy)
        {
            PlayAudioClip(13);
            if (GoldForCity < cost)
            {
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(56));
                return;
            }

            GoldForCity -= cost;
            UpdateInfoUis();

            ui.Off();
        }

        CreateCardToList(card, info);
        if (!isBuy) PassStage();

        ui.SetSelect(false);
    }

    //答题结束
    private void OnAnswerQuiz(bool isCorrect)
    {
        PlayAudioClip(13);

        //for (int i = 3; i < 6; i++)
        //{
        //    eventsWindows[2].transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
        //}

        string rewardStr = "";
        if (isCorrect)
        {
            rewardStr = DataTable.GetStringText(58);
            //eventsWindows[2].transform.GetChild(btnIndex).GetChild(0).GetComponent<Text>().color = Color.green;
            var pick = DataTable.QuestReward.Values.Select(r => new WeightElement { Id = r.Id, Weight = r.Weight })
                .PickOrDefault().Id;
            var reward = DataTable.QuestReward[pick].Produce;

            var info = RandomPickFromRareClass((GameCardType)reward.CardType, reward.Rarity);
            var card = new GameCard().Instance(info.Type, info.Id, reward.Star);
            CreateCardToList(card, info);
        }
        else
        {
            rewardStr = DataTable.GetStringText(59);
            //eventsWindows[2].transform.GetChild(btnIndex).GetChild(0).GetComponent<Text>().color = Color.red;
        }

        PlayerDataForGame.instance.ShowStringTips(rewardStr);
        //eventsWindows[2].transform.GetChild(6).gameObject.SetActive(true);
    }

    //根据稀有度返回随机id
    public GameCardInfo RandomPickFromRareClass(GameCardType cardType, int rarity)
    {
        var info = GameCardInfo.RandomPick(cardType, rarity);
        if (cardType != GameCardType.Hero || rarity != 1) return info;
        if (GameSystem.MapService.GetCharacterInRandom(50, out var cha)) info.Rename(cha.Name, cha.Nickname, cha.Sign);
        return info;
    }

    //初始化卡牌列表
    private void InitCardListShow()
    {
        var forceId = PlayerDataForGame.instance.CurrentWarForceId;
#if UNITY_EDITOR
        if (forceId == -2) //-2为测试用不重置卡牌，直接沿用卡牌上的阵容
        {
            PlayerDataForGame.instance.fightHeroId.Select(id => new GameCard().Instance(GameCardType.Hero, id, 1))
                .Concat(PlayerDataForGame.instance.fightTowerId.Select(id =>
                    new GameCard().Instance(GameCardType.Tower, id, 1)))
                .Concat(PlayerDataForGame.instance.fightTrapId.Select(id =>
                    new GameCard().Instance(GameCardType.Trap, id, 1)))
                .ToList().ForEach(CreateCardToList);
            return;
        }
#endif
        PlayerDataForGame.instance.fightHeroId.Clear();
        PlayerDataForGame.instance.fightTowerId.Clear();
        PlayerDataForGame.instance.fightTrapId.Clear();

        var hstData = PlayerDataForGame.instance.hstData;
        //临时记录武将存档信息
        hstData.heroSaveData.Enlist(forceId).ToList()
            .ForEach(CreateCardToList);
        hstData.towerSaveData.Enlist(forceId).ToList()
            .ForEach(CreateCardToList);
        hstData.trapSaveData.Enlist(forceId).ToList()
            .ForEach(CreateCardToList);
    }

    //创建玩家卡牌
    private void CreateCardToList(GameCard card) => CreateCardToList(card, card.GetInfo());

    private void CreateCardToList(GameCard card, GameCardInfo info)
    {
        var ui = PrefabManager.NewWarGameCardUi(PlayerCardsRack.transform);
        var cardDrag = ui.DragComponent;
        ui.Init(card);
        ui.SetSize(Vector3.one);
        ui.tag = GameSystem.PyCard;
        GiveGameObjEventForHoldOn(ui, info.About);
        var fightCard = new FightCardData(card);
        fightCard.cardObj = ui;
        fightCard.isPlayerCard = true;
        cardDrag.Init(fightCard, herosCardListTran, herosCardListScrollRect);
        playerCardsDatas.Add(fightCard);
    }

    //展示卡牌详细信息
    private void ShowInfoOfCardOrArms(string text)
    {

        aboutCardUi.InfoText.text = text;
        aboutCardUi.gameObject.SetActive(true);

        //StartCoroutine(ShowOrHideCardInfoWin());
    }

    //隐藏卡牌详细信息
    private void HideInfoOfCardOrArms()
    {
        //isNeedAutoHideInfo = false;
        aboutCardUi.gameObject.SetActive(false);
    }

    //卡牌附加按下抬起方法
    public void GiveGameObjEventForHoldOn(WarGameCardUi obj, string str)
    {
        EventTriggerListener.Get(obj.gameObject).onDown += _ => ShowInfoOfCardOrArms(str);
        EventTriggerListener.Get(obj.gameObject).onUp += _ => HideInfoOfCardOrArms();
    }

    //初始化场景内容
    private void InitMainUiShow()
    {
        battleNameText.text = DataTable.War[PlayerDataForGame.instance.selectedWarId].Title;
        var py = PlayerDataForGame.instance;
        string flagShort;
        if (py.Character != null && py.Character.IsValidCharacter() && !string.IsNullOrWhiteSpace(py.Character.Name))
            flagShort = py.Character.Name.First().ToString();
        else flagShort = "玩";
        infoUis.Short.text = flagShort;
        UpdateInfoUis();
        UpdateLevelInfo();
        UpdateBattleSchedule();
    }

    //刷新战役进度显示
    private void UpdateBattleSchedule()
    {
        nowGuanQiaIndex++;
        var totalCheckPoints = DataTable.War[PlayerDataForGame.instance.selectedWarId].CheckPoints;
        if (nowGuanQiaIndex >= totalCheckPoints)
        {
            nowGuanQiaIndex = totalCheckPoints;
        }

        string str = nowGuanQiaIndex + "/" + totalCheckPoints;
        battleScheduleText.text = str;
    }

    //刷新金币宝箱的显示
    private void UpdateInfoUis() => infoUis.Set(GoldForCity, WarChests);

    public bool IsPlayerAvailableToPlace(int targetPos,bool isAddIn)
    {
        var isAvailable = Chessboard.IsPlayerScopeAvailable(targetPos);
        if (!isAvailable) return false;
        var balance = maxHeroNums - TempScope.Count - PlayerScope.Count;
        if (balance <= 0 && isAddIn) PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(38));
        return !isAddIn || balance > 0;
    }

    public void RemoveCardFromBoard(FightCardData card)
    {
        card.posIndex = -1;
        TempScope.Remove(card);
        UpdateHeroEnlistText();
    }

    /// <summary>
    /// 当前武将卡牌上阵最大数量
    /// </summary>
    [HideInInspector] public int maxHeroNums;


    //更新等级相关显示
    private void UpdateLevelInfo()
    {
        var baseCfg = DataTable.BaseLevel[cityLevel];
        //等级
        cityLevelObj.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = cityLevel + "级";
        //武将可上阵
        maxHeroNums = baseCfg.CardMax;
        cityLevelObj.transform.GetChild(2).GetChild(0).GetComponent<Text>().text = baseCfg.CardMax.ToString();
        //升级金币
        upLevelBtn.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = baseCfg.Cost.ToString();
        UpdateHeroEnlistText();
    }

    /// <summary>
    /// 升级城池
    /// </summary>
    public void OnClickUpLevel()
    {
        //ShowOrHideGuideObj(2, false);
        var baseCfg = DataTable.BaseLevel[cityLevel];
        if (GoldForCity < baseCfg.Cost)
        {
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(56));
            PlayAudioClip(20);
        }
        else
        {
            GoldForCity -= baseCfg.Cost;
            cityLevel++;
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(60));
            UpdateInfoUis();
            UpdateLevelInfo();
            //满级
            if (cityLevel >= DataTable.BaseLevel.Count)
            {
                upLevelBtn.GetComponent<Button>().enabled = false;
                upLevelBtn.transform.GetChild(0).gameObject.SetActive(false);
                upLevelBtn.transform.GetChild(1).GetComponent<Text>().text = DataTable.GetStringText(61);
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(62));
            }

            UpdateHeroEnlistText();
            PlayAudioClip(19);
        }
    }

    private void UpdateHeroEnlistText() => heroEnlistText.text = 
        string.Format(DataTable.GetStringText(24),
        TempScope.Count + PlayerScope.Count, 
        maxHeroNums);


    /// <summary>
    /// 返回主城
    /// </summary>
    public void OnClickGoBackToCity()
    {
        Time.timeScale = 1;
        PlayAudioClip(13);
        isGettingStage = true; //限制返回主城时还能进入关卡
        if (PlayerDataForGame.instance.isJumping) return;
        PlayerDataForGame.instance.JumpSceneFun(GameSystem.GameScene.MainScene, false);
    }

    /// <summary>
    /// 根据obj找到其父数据
    /// </summary>
    public int FindDataFromCardsDatas(GameObject obj)
    {
        int i = 0;
        for (; i < playerCardsDatas.Count; i++)
        {
            if (playerCardsDatas[i].cardObj == obj)
            {
                break;
            }
        }

        if (i >= playerCardsDatas.Count)
        {
            return -1;
        }
        else
        {
            return i;
        }
    }

    /// <summary>
    /// 卡牌移动
    /// </summary>
    public void CardMoveToPos(GameObject needMoveObj, Vector3 vec3)
    {
        needMoveObj.transform.DOMove(vec3, cardMoveSpeed)
            .SetEase(Ease.Unset)
            .OnComplete(delegate()
            {
                //Debug.Log("-----Move Over"); 
            }).SetAutoKill(true);
    }

    public void PlayAudioClip(int indexClips)
    {
        AudioController0.instance.ChangeAudioClip(indexClips);
        AudioController0.instance.PlayAudioSource(0);
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

    [SerializeField] Text musicBtnText; //音乐开关文本

    //打开设置界面
    public void OpenSettingWinInit()
    {
        PlayAudioClip(13);
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
            PlayAudioClip(13);
            return;
        }

        musicBtnText.text = DataTable.GetStringText(41);
    }

    [SerializeField] GameObject[] guideObjs; // 指引objs 0:开始关卡 1:查看奇遇 2:升级按钮

    public void PlayAudioForSecondClip(int clipIndex, float delayedTime)
    {
        var clip = audioClipsFightEffect[clipIndex];
        var volume = audioVolumeFightEffect[clipIndex];
        if (AudioController0.instance.ChangeAudioClip(clip, volume))
        {
            AudioController0.instance.PlayAudioSource(0);
            return;
        }

        AudioController0.instance.audioSource.volume *= 0.75f;
        audioSource.clip = audioClipsFightEffect[clipIndex];
        audioSource.volume = audioVolumeFightEffect[clipIndex];
        if (!GamePref.PrefMusicPlay)
            return;
        audioSource.PlayDelayed(delayedTime);
    }

    bool isShowQuitTips = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isShowQuitTips)
            {
                PlayAudioClip(13);
#if UNITY_ANDROID
                Application.Quit();
#endif
            }
            else
            {
                isShowQuitTips = true;
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(52));
                Invoke(nameof(ResetQuitBool), 2f);
            }
        }
    }

    //重置退出游戏判断参数
    private void ResetQuitBool()
    {
        isShowQuitTips = false;
    }

    [Serializable]
    private class PlayerInfoUis
    {
        public Text Gold;
        public Text Chests;
        public Text Short;

        public void Set(int gold, ICollection<int> chests)
        {
            Gold.text = gold.ToString();
            Chests.text = chests.Count.ToString();
        }
    }

    [Serializable]
    private class GenericWarWindow
    {
        private const string X = "×";

        [Serializable]
        public enum States
        {
            Reward,
            Recovery
        }

        public GameObject WindowObj;
        public Text GoldText;
        public GameObject GoldObj;
        public Text ChestText;
        public GameObject ChestObj;
        public Text MessageText;
        public Button OkButton;
        public GameObject CloudObj;
        [SerializeField] private CSwitch[] componentSwitch;
        private GameObjectSwitch<States> comSwitch;

        public void Init()
        {
            comSwitch = new GameObjectSwitch<States>(componentSwitch.Select(c => (c.State, c.Objs)).ToArray());
            OkButton.onClick.AddListener(instance.PassStage);
        }

        private void Show(States state)
        {
            WindowObj.SetActive(true);
            comSwitch.Set(state);
        }

        public void Off() => WindowObj.SetActive(false);

        [Serializable]
        private class CSwitch
        {
            public States State;
            public GameObject[] Objs;
        }

        public IEnumerator InvokeCloudAnimation()
        {
            CloudObj.SetActive(false);
            yield return new WaitForEndOfFrame();
            CloudObj.SetActive(true);
        }

        public void SetRecovery(string message)
        {
            MessageText.text = message;
            Show(States.Recovery);
        }

        public void SetReward(int gold, int chest)
        {
            GoldText.text = X + gold;
            GoldObj.SetActive(gold > 0);
            ChestText.text = X + chest;
            ChestObj.SetActive(chest > 0);
            Show(States.Reward);
        }
    }
}

public class GameStage
{
    public CheckpointTable Checkpoint { get; set; }
    public BattleEventTable BattleEvent { get; set; }
    public int RandomId { get; set; }
}