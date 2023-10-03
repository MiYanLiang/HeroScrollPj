using Assets;
using Beebyte.Obfuscator;
using CorrelateLib;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class WarsUIManager : MonoBehaviour
{
    public static WarsUIManager instance;

    [SerializeField] private WarBoardUi WarBoard;
    [SerializeField] private ChessboardVisualizeManager chessboardManager;
    [SerializeField] private PlayerInfoUis infoUis; //玩家信息UI
    [SerializeField] GameObject cityLevelObj; //主城信息obj

    //[SerializeField]
    //GameObject cardForWarListPres; //列表卡牌预制件
    [SerializeField] GuanQiaUi guanQiaPreObj; //关卡按钮预制件
    [SerializeField] Button operationButton; //关卡执行按钮
    [SerializeField] Text operationText; //关卡执行文字
    [SerializeField] GameObject upLevelBtn; //升级城池btn

    [SerializeField] SanXuanWindowUi SanXuanWindow;
    [SerializeField] WarQuizWindowUi QuizWindow; //答题
    [SerializeField] GenericWarWindow GenericWindow;
    
    public WarMiniWindowUI gameOverWindow; //战役结束ui
    public Dictionary<int, GameStage> StagesMap { get; } = new Dictionary<int, GameStage>();
    [SerializeField] float percentReturnHp; //回春回血百分比

    public int baYeCityLevel = 1;
    public int cityLevel = 1; //记录城池等级
    public int goldForCity; //记录城池金币
    private int battleId = -1;

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

    int indexLastGuanQiaId; //记录上一个关卡id

    //int passedGuanQiaNums;  //记录通过的关卡数
    private bool[] PassCheckpoints;

    public float cardMoveSpeed; //卡牌移动速度

    public AudioClip[] audioClipsFightEffect;
    [Range(0, 1)]
    public float[] audioVolumeFightEffect;

    public AudioClip[] audioClipsFightBack;
    [Range(0, 1)]
    public float[] audioVolumeFightBack;

    [SerializeField] AudioSource audioSource;

    [SerializeField] Text levelIntroText; //关卡介绍文本
    [SerializeField] Text battleNameText; //战役名文本
    [SerializeField] Text battleScheduleText; //战役进度文本
    int nowGuanQiaIndex; //记录当前关卡进度

    bool isGettingStage; //记录是否进入了关卡

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
        WarBoard.Init();
        WarBoard.OnGameSet.AddListener(playerWin =>
        {
            if (!playerWin)
            {
                ExpeditionFinalize(false);
                return;
            }
            StartCoroutine(OnPlayerWin());
        });
        chessboardManager.OnResourceUpdate.AddListener(OnResourceUpdate);
        WarBoard.OnRoundPause += OnRoundPause; 
        WarBoard.Chessboard.SetStartWarUi(OnRoundStartClick);
    }

    private void OnRoundPause() => waitForRoundStart = true;

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
                cityLevel = baYeCityLevel;
                break;
            case PlayerDataForGame.WarTypes.None:
                throw XDebug.Throw<WarsUIManager>($"未确认战斗类型[{PlayerDataForGame.WarTypes.None}]，请在调用战斗场景前预设战斗类型。");
            default:
                throw new ArgumentOutOfRangeException();
        }

        indexLastGuanQiaId = 0;
        PassCheckpoints = new bool[DataTable.War[PlayerDataForGame.instance.selectedWarId].CheckPoints];
        cardMoveSpeed = 2f;
        nowGuanQiaIndex = 0;
        point0Pos = point0Tran.position;
        point1Pos = point1Tran.position;
        point2Pos = point2Tran.position;

        Input.multiTouchEnabled = false; //限制多指拖拽
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

        InitCardToRack();

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
            levelIntroText.transform.parent.GetComponent<Image>().DOFade(0, 0.5f).OnComplete(delegate ()
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
        Time.timeScale = 1f;
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
            //注意: 如果这里报错, 有时候是因为请求token的时候不成功而导致初始化失败
            PlayerDataForGame.instance.UpdateWarUnlockProgress(PassCheckpoints.Count(isPass => isPass));
            var ca = PlayerDataForGame.instance.warsData.GetCampaign(reward.WarId);
            //if (treasureChestNums > 0) rewardMap.Trade(2, treasureChestNums); //index2是宝箱图
            var viewBag = ViewBag.Instance()
                .WarCampaignDto(new WarCampaignDto
                { IsFirstRewardTaken = ca.isTakeReward, UnlockProgress = ca.unLockCount, WarId = ca.warId })
                .SetValues(reward.Token, reward.Chests);

            ApiPanel.instance.InvokeVb(vb =>
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

            GameSystem.Instance.DisplayStaminaUiChangeEffect = true;
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

    private List<int> PlayerRewardChests => PlayerDataForGame.instance.WarReward.Chests;
    private int TempGold { get; set; }
    private List<int> TempChest { get; set; } = new List<int>();

    private void OnResourceUpdate(bool isChallenger, int id, int value)
    {
        if (!isChallenger) return;
        if (id == -1)
        {
            TempGold += value;
#if UNITY_EDITOR
            XDebug.Log<WarsUIManager>($"获得金币[{value}]!");
#endif
        }

        if (id >= 0)
        {
            for (int i = 0; i < value; i++)
            {
                TempChest.Add(id);
#if UNITY_EDITOR
                XDebug.Log<WarsUIManager>($"获得宝箱[{value}]!");
#endif
            }
        }
    }

    public void InitChessboard(GameStage stage)
    {
        var playerBase = FightCardData.PlayerBaseCard(PlayerDataForGame.instance.pyData.Level, cityLevel);
        var enemyBase = FightCardData.BaseCard(false, stage.BattleEvent.BaseHp, 1);

        var battle = stage.BattleEvent;
        WarBoard.ChangeChessboardBg(GameResources.BattleBG[stage.Checkpoint.BattleBG]);
        battleId = battle.Id;
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
                enemyCards.Add(ChessCard.Instance(chessman.CardId, chessman.CardType, chessman.Star, arouse: 0, pos: i));
            }
        }
        else
        {
            var enemies = DataTable.Enemy[enemyRandId].Poses();
            for (int i = 0; i < 20; i++)
            {
                var enemyId = enemies[i];
                if (enemyId == 0) continue;
                var enemyUnit = DataTable.EnemyUnit[enemyId];
                if (enemyUnit.Rarity == 0) continue;
                var cardType = enemyUnit.CardType;
                ChessCard chessCard;
                if (enemyUnit.CardType >= 0)
                {
                    var card = GameCardInfo.RandomPick((GameCardType)enemyUnit.CardType, enemyUnit.Rarity);
                    var level = enemyUnit.Star;
                    chessCard = ChessCard.Instance(card.Id, cardType, level, arouse: 0, pos: i);
                }
                else//宝箱类
                {
                    var trap = DataTable.Trap[enemyUnit.Rarity];
                    chessCard = ChessCard.Instance(trap.Id, GameCardType.Trap, enemyUnit.Star, arouse: 0, pos: i);
                }
                enemyCards.Add(chessCard);
            }
        }

        WarBoard.InitNewChessboard(enemyBase, playerBase, enemyCards);
        waitForRoundStart = true;
        WarBoard.gameObject.SetActive(true);
    }

    private bool waitForRoundStart;
    private void OnRoundStartClick()
    {
        if(!waitForRoundStart)return;
        waitForRoundStart = false;
        WarBoard.OnLocalRoundStart();
    }


    /// <summary>
    /// 进入战斗
    /// </summary>
    private void GoToBattle(GameStage stage)
    {
        var checkPoint = stage.Checkpoint;
        currentEvent = EventTypes.战斗;
        PlayAudioClip(21);
        int bgmIndex = checkPoint.BattleBGM;
        WarMusicController.Instance.PlayBgm(bgmIndex);
        //AudioController1.instance.isNeedPlayLongMusic = true;
        //AudioController1.instance.ChangeAudioClip(audioClipsFightBack[bgmIndex], audioVolumeFightBack[bgmIndex]);
        //AudioController1.instance.PlayLongBackMusInit();
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
        foreach (var card in WarBoard.PlayerScope)
        {
            card.ResetHp(card.MaxHitPoint);
            card.cardObj.War.UpdateHpUi(card.Status.HpRate);
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

        point1Tran.DOMove(point1Pos, 1.5f).SetEase(Ease.Unset).OnComplete(delegate ()
        {
            isPointMoving = false;

            point0Tran.gameObject.SetActive(true);
        });
    }

    [SerializeField] float waySpeedFlo; //通路时长

    [SerializeField] float randomChaZhi; //随机插值

    [SerializeField] int pointNums; //中途点数


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
        WarBoard.CloseChessboard();
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
            InitGiftCard(i, ui, mercenary);
            //广告概率
            if (Random.Range(0, 100) < 25)
            {
                ui.SetAd(success =>
                {
                    if (!success) return;
                    GetOrBuyCards(true, 0, ui, btnIndex);
                    PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(57));
                });
                continue;
            }

            ui.SetPrice(mercenary.Cost, () =>
                GetOrBuyCards(true, mercenary.Cost, ui, btnIndex));
        }

        return true;
    }

    private void InitGiftCard(int index, TradableGameCardUi ui, MercenaryTable mercenary)
    {
        var cardType = (GameCardType)mercenary.Produce.CardType;
        var cardRarity = mercenary.Produce.Rarity;
        var cardLevel = mercenary.Produce.Star;
        var info = RandomPickFromRareClass(cardType, cardRarity);
        ui.SetGameCard(GameCard.Instance(cardId: info.Id, type: mercenary.Produce.CardType, level: cardLevel));
        ui.OnClickAction.RemoveAllListeners();
        ui.OnClickAction.AddListener(() => OnClickToShowShopInfo(index, info.About));
        ui.GameCard.SetCardInfo(info);
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
            InitGiftCard(i, ui, mercenary);
            ui.SetPrice(0, () => GetOrBuyCards(false, 0, ui, index));
        }
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
    private void GetOrBuyCards(bool isBuy, int cost, TradableGameCardUi cardUi, int btnIndex)
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

        WarBoard.CreateCardToRack(cardUi.Card, cardUi.GameCard.CardInfo);
        if (!isBuy) PassStage();

        ui.SetSelect(false);
    }

    //答题结束
    private void OnAnswerQuiz(bool isCorrect)
    {
        PlayAudioClip(13);
        string rewardStr = "";
        if (isCorrect)
        {
            rewardStr = DataTable.GetStringText(58);
            var pick = DataTable.QuestReward.Values.Select(r => new WeightElement { Id = r.Id, Weight = r.Weight })
                .PickOrDefault().Id;
            var reward = DataTable.QuestReward[pick].Produce;

            var info = RandomPickFromRareClass((GameCardType)reward.CardType, reward.Rarity);
            var card = GameCard.Instance(info.Id, info.Type, reward.Star);
            WarBoard.CreateCardToRack(card, info);
        }
        else
        {
            rewardStr = DataTable.GetStringText(59);
        }

        PlayerDataForGame.instance.ShowStringTips(rewardStr);
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
    private void InitCardToRack()
    {
        var forceId = PlayerDataForGame.instance.CurrentWarForceId;
#if UNITY_EDITOR
        if (forceId == -2) //-2为测试用不重置卡牌，直接沿用卡牌上的阵容
        {
            var hst = PlayerDataForGame.instance.hstData;
            PlayerDataForGame.instance.fightHeroId
                .Select(id => GameCard.Instance(hst.heroSaveData.First(h => h.CardId == id)))
                .Concat(PlayerDataForGame.instance.fightTowerId.Select(id =>
                    GameCard.InstanceTower(id, GetLevel(hst.towerSaveData, id))))
                .Concat(PlayerDataForGame.instance.fightTrapId.Select(id =>
                    GameCard.InstanceTrap(id, GetLevel(hst.trapSaveData, id))))
                .ToList().ForEach(c => WarBoard.CreateCardToRack(c,null));
            return;
        }

        int GetLevel(List<GameCard> list, int id) => list.First(c => c.CardId == id).Level;
#endif
        PlayerDataForGame.instance.fightHeroId.Clear();
        PlayerDataForGame.instance.fightTowerId.Clear();
        PlayerDataForGame.instance.fightTrapId.Clear();

        var hstData = PlayerDataForGame.instance.hstData;
        //临时记录武将存档信息
        hstData.heroSaveData.Enlist(forceId).ToList()
            .ForEach(c => WarBoard.CreateCardToRack(c,null));
        hstData.towerSaveData.Enlist(forceId).ToList()
            .ForEach(c => WarBoard.CreateCardToRack(c,null));
        hstData.trapSaveData.Enlist(forceId).ToList()
            .ForEach(c => WarBoard.CreateCardToRack(c, null));
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
    private void UpdateInfoUis() => infoUis.Set(GoldForCity, PlayerDataForGame.instance.WarReward.Chests);

    //更新等级相关显示
    private void UpdateLevelInfo()
    {
        var baseCfg = DataTable.BaseLevel[cityLevel];
        //等级
        cityLevelObj.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = cityLevel + "级";
        //武将可上阵
        WarBoard.MaxCards = baseCfg.CardMax;
        cityLevelObj.transform.GetChild(2).GetChild(0).GetComponent<Text>().text = baseCfg.CardMax.ToString();
        //升级金币
        upLevelBtn.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = baseCfg.Cost.ToString();
        WarBoard.UpdateHeroEnlistText();
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

            WarBoard.UpdateHeroEnlistText();
            PlayAudioClip(19);
        }
    }

    IEnumerator OnPlayerWin()
    {
        var battle = DataTable.BattleEvent[battleId];
        TempGold += battle.GoldReward;
        TempChest.AddRange(battle.WarChestTableIds);
        PlayerRewardChests.AddRange(TempChest);
        UpdateInfoUis();
        WarBoard.ChangeTimeScale(1, false);
        yield return new WaitForSeconds(2);
        GenericWindow.SetReward(TempGold, TempChest.Count);
        GoldForCity += TempGold;
        UpdateInfoUis();
        TempGold = 0;
        TempChest.Clear();
    }


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
    /// 卡牌移动
    /// </summary>
    public void CardMoveToPos(GameObject needMoveObj, Vector3 vec3)
    {
        needMoveObj.transform.DOMove(vec3, cardMoveSpeed)
            .SetEase(Ease.Unset)
            .OnComplete(delegate ()
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

    //public void PlayAudioForSecondClip(int clipIndex, float delayedTime)
    //{
    //    var clip = audioClipsFightEffect[clipIndex];
    //    var volume = audioVolumeFightEffect[clipIndex];
    //    if (AudioController0.instance.ChangeAudioClip(clip, volume))
    //    {
    //        AudioController0.instance.PlayAudioSource(0);
    //        return;
    //    }

    //    AudioController0.instance.audioSource.volume *= 0.75f;
    //    audioSource.clip = audioClipsFightEffect[clipIndex];
    //    audioSource.volume = audioVolumeFightEffect[clipIndex];
    //    if (!GamePref.PrefMusicPlay)
    //        return;
    //    audioSource.PlayDelayed(delayedTime);
    //}

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