using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets;
using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

//对决页面
public class Versus : MonoBehaviour
{
    public enum WarIdentity
    {
        Anonymous,
        Challenger,//挑战者
        Host,//关主
        Uncertain//过期的挑战者(挑战WarInstanceId已经不存在了)
    }
    public enum FormationMode
    {
        自由军团,
        仅选中军团
    }
    private static Versus instance;

#if UNITY_EDITOR
    public FormationMode Mode;
    public static string RkApi { get; } = "https://localhost:5001/api/rkwar";
    
    public const int TestCharId = -7;

    public static DataBag OnProcessApi(string data)
    {
        var bag = DataBag.DeserializeBag(data);
        if (!bag.IsValid()) instance.GetBackToWarListPage(data);
        return bag;
    }

    public const string GetWarsV1 = "GetWarsV1";
    public const string GetStageV1 = "GetStageV1";
    public const string GetGetFormationV1 = "GetFormationV1";
    public const string GetCheckPointResultV1 = "GetCheckPointResultV1";
    public const string GetCheckPointResultV2 = "GetCheckPointResultV2";
    public const string StartChallengeV1 = "StartChallengeV1";
    public const string SubmitFormationV1 = "SubmitFormationV1";
    public const string CancelChallengeV1 = "CancelChallengeV1";

    public static void GetRkWars(Action<DataBag> apiAction) => Http.Get($"{RkApi}/{GetWarsV1}?charId={TestCharId}",
        result => apiAction(OnProcessApi(result)),true ,GetWarsV1);

    public static void GetRkWarStageInfo(int warId,int warIsd ,Action<DataBag> apiAction) => Http.Get(
        $"{RkApi}/{GetStageV1}?warId={warId}&warIsd={warIsd}&charId={TestCharId}", result => apiAction(OnProcessApi(result)), true, GetStageV1);
    public static void GetRkCheckpointFormation(int warId, int index, Action<DataBag> apiAction) => 
        Http.Get(
            $"{RkApi}/{GetGetFormationV1}?warId={warId}&charId={TestCharId}&index={index}",
            result => apiAction(OnProcessApi(result)), true, GetGetFormationV1);

    public static void GetRkCheckPointWarResult(int warId, int index, Action<DataBag> apiAction) =>
        Http.Get($"{RkApi}/{GetCheckPointResultV2}?warId={warId}&index={index}&charId={TestCharId}", result => apiAction(OnProcessApi(result)),
            true, GetCheckPointResultV2);

    public static void PostRkStartChallenge(int warId, int warIsd, int troopId, Action<DataBag> apiAction) =>
        Http.Post($"{RkApi}/{StartChallengeV1}?charId={TestCharId}&warId={warId}&warIsd={warIsd}&troopId={troopId}",
            string.Empty,
            result => apiAction(OnProcessApi(result)), true, StartChallengeV1);

    public static void PostRkSubmitFormation(int warId, int index, string content,
        Action<DataBag> apiAction) =>
        Http.Post($"{RkApi}/{SubmitFormationV1}?charId={TestCharId}&warId={warId}&index={index}",
            content, result => apiAction(OnProcessApi(result)), true,
            SubmitFormationV1);

    public static void PostRkCancelChallenge(int warId, Action<DataBag> apiAction) =>
        Http.Post($"{RkApi}/{CancelChallengeV1}?warId={warId}&charId={TestCharId}", string.Empty, result => apiAction(OnProcessApi(result)),
            true, CancelChallengeV1);
#else
    public static string GetWarsV1 { get; set; }
    public static string GetStageV1 { get; set; }
    public static string GetGetFormationV1 { get; set; }
    public static string GetCheckPointResultV1 { get; set; }
    public static string GetCheckPointResultV2 { get; set; }
    public static string StartChallengeV1 { get; set; }
    public static string SubmitFormationV1 { get; set; }
    public static string CancelChallengeV1 { get; set; }
#endif

    [SerializeField] private WarBoardUi WarBoard;
    [SerializeField] private ChessboardVisualizeManager ChessboardManager;
    [SerializeField] private Image Infoboard;
    [SerializeField] private int MaxCards;
    [SerializeField] private VsWarStageController warStageController;
    [SerializeField] private VersusWindow _versusWindow;
    [SerializeField] private Image BlockingPanel;
    [SerializeField] private Button XButton;
    [SerializeField] private Text RkTimer;
    [SerializeField] private VersusRestrictUi Restrict;
    [SerializeField] private RoundUi roundUi;
    [SerializeField] private AudioField AudioFields;

    
    public Sprite[] WarTitles;
    public VsWarListController warListController;
    public PlayerCharacterUi PlayerCharacterUi;
    public RkTimerDto RkState { get; private set; }
    public int CityLevel = 1;


    private event UnityAction UpdateEverySecond;

    public void Init(UIManager uiMgr)
    {
        instance = this;
        if (!EffectsPoolingControl.instance.IsInit)
            EffectsPoolingControl.instance.Init();
        UpdateEverySecond += RkTimerUpdater;
        SetRkTimer(string.Empty);
        if (uiMgr != null) Restrict.Init(uiMgr, PlayerCharacterUi);
        WarBoard.Init();
        WarBoard.MaxCards = MaxCards;
        WarBoard.UpdateHeroEnlistText();
        ChessboardManager.Init(WarBoard.Chessboard, WarBoard.JiBanManager);
        XButton.onClick.AddListener(() => WarboardActive(false));
        //SignalRClient.instance.SubscribeAction(EventStrings.Chn_RkListUpdate,  OnUpdateWarList);
        ControllerInit();
    }

    //private void OnUpdateWarList(string arg0)
    //{
    //    if (GameSystem.CurrentScene != GameSystem.GameScene.MainScene &&
    //        UIManager.instance != null && 
    //        UIManager.instance.currentPage == UIManager.Pages.对决 
    //       ) return;
    //    warListController.GetWarList();
    //}

    //private void OnDestroy() => SignalRClient.instance.UnSubscribeAction(EventStrings.Chn_RkListUpdate, OnUpdateWarList);


#if UNITY_EDITOR && !DEBUG

    void Start()
    {
        StartCoroutine(AwaitInit());
    }

    IEnumerator AwaitInit()
    {
        yield return new WaitUntil(() => GameSystem.IsInit);
        DataTable.instance.Init();
        Init();
    }
#endif

    private void ControllerInit()
    {
        warListController.Init(this, DisplayStagePage);
        warStageController.Init(this, OnReadyWarboard);
    }

    private void OnReadyWarboard((int warId, int warIsd, int pointId, int maxCards, int troopId) o,
        Dictionary<int, IGameCard> formation,GameCard[] except)
    {
        StartNewGame();
        SetEnemyFormation(formation);
        var forceId = o.troopId;
#if UNITY_EDITOR
        if(Mode == FormationMode.自由军团)
            forceId = -2;
#endif
        InitCardToRack(forceId, except);
        MaxCards = o.maxCards;
        WarBoard.MaxCards = o.maxCards;
        WarBoard.Chessboard.RemoveAllStartClicks();
        WarBoard.Chessboard.SetStartWarUi(() =>
            OnSubmitFormation(o.warId, o.warIsd, o.pointId));
        WarBoard.Chessboard.SetStartWarUi(OnPlayChessRounds);
        XButton.interactable = true;
        WarBoard.UpdateHeroEnlistText();
        roundUi.Off();
        WarboardActive(true);
    }

    private void OnPlayChessRounds()
    {
        XButton.interactable = false;
        roundUi.SetRound(0);
        WarBoard.OnRoundStart += OnEveryRound;
    }

    private int RoundCount = 0;
    private void OnEveryRound(int round)
    {
        RoundCount++;
        roundUi.SetRound(RoundCount);
        if(RoundCount <= 5)return;
        if(!XButton.interactable)
        {
            XButton.interactable = true;
            XButton.onClick.RemoveAllListeners();
            XButton.onClick.AddListener(() =>
            {
                if (currentChessAnimation != null)
                {
                    StopAllCoroutines();
                    currentChessAnimation = null;
                }
                WarboardActive(false);
            });
        }
    }

    private enum ChallengeResult
    {
        NotFound,
        InProgress,
        Clear
    }

    private void OnSubmitFormation(int warId, int warIsd, int pointId)
    {
        WarBoard.Chessboard.DisplayStartButton(false);
        var challengerFormation = WarBoard.PlayerScope.ToDictionary(c => c.Pos, c => new Card(c.Card) as IGameCard);
        ApiPanel.instance.InvokeRk(OnCallBack, msn =>
        {
            GetBackToWarListPage(msn);
            CancelWindow.Display(() => WarboardActive(false));
        }, SubmitFormationV1, 
            warId,
            pointId, 
            challengerFormation);
#if UNITY_EDITOR
        var json = Json.Serialize(challengerFormation);
        //PostRkSubmitFormation(warId, pointId, json, OnCallBack);
#endif

        void OnCallBack(DataBag bag)
        {
            PlayChessboard();
            var wId = bag.Get<int>(0);
            var isChallengerWin = bag.Get<bool>(1);
            var rounds = bag.Get<List<ChessRound>>(2);
            var chessmen = bag.Get<List<WarResult.Operator>>(3);
            var cr = bag.Get<int>(4);
            var result = (ChallengeResult)cr;
            PlayResult(new WarResult(isChallengerWin, chessmen.Cast<IOperatorInfo>().ToList(), rounds));
            warListController.GetWarList();
            switch (result)
            {
                case ChallengeResult.NotFound:
                    break;
                case ChallengeResult.InProgress:
                    DisplayStagePage(wId, warIsd);
                    break;
                case ChallengeResult.Clear:
                    DisplayWarlistPage();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [SerializeField] private WbCancelWindow CancelWindow;

    public void DisplayWarlistPage()
    {
        warListController.GetWarList(OnSuccessCallBack);

        void OnSuccessCallBack()
        {
            warListController.Display(true);
            warStageController.Display(false);
        }
    }


    private void DisplayStagePage(int warId,int warIsd)
    {
        warListController.Display(false);
        warStageController.Set(warId, warIsd);
    }

    #region PlayerVersus

    //初始化卡牌列表
    private void InitCardToRack(int forceId, GameCard[] except)
    {
        var hstData = PlayerDataForGame.instance.hstData;

#if UNITY_EDITOR
        if (forceId == -2) //-2为测试用不重置卡牌，直接沿用卡牌上的阵容
        {
            hstData.heroSaveData.ForEach(c => WarBoard.CreateCardToRack(c,null));
            hstData.towerSaveData.ForEach(c => WarBoard.CreateCardToRack(c,null));
            hstData.trapSaveData.ForEach(c => WarBoard.CreateCardToRack(c,null));
            return;
        }
#endif
        PlayerDataForGame.instance.fightHeroId.Clear();
        PlayerDataForGame.instance.fightTowerId.Clear();
        PlayerDataForGame.instance.fightTrapId.Clear();
        //临时记录武将存档信息
        var list = hstData.heroSaveData.Concat(hstData.towerSaveData)
            .Concat(hstData.trapSaveData)
            .Enlist(forceId)
            .Where(c=> !except.Any(e=> e.CardId == c.CardId && e.Type == c.Type))
            .ToList();
        list.ForEach(c => WarBoard.CreateCardToRack(c, null));
    }

    public void PlayResult(WarResult data)
    {
        //Infoboard.transform.DOLocalMoveY(1440, 2);
        WarBoard.InitNewGame(false, true);
        foreach (var op in data.Chessmen)
            WarBoard.SetCustomInstanceCardToBoard(op.Pos,
                GameCard.Instance(cardId: op.Card.CardId, type: op.Card.Type, level: op.Card.Level, arouse: op.Card.Arouse,
                    deputy1Id: op.Card.Deputy1Id,deputy1Level: op.Card.Deputy1Level,
                    deputy2Id: op.Card.Deputy2Id,deputy2Level: op.Card.Deputy2Level,
                    deputy3Id: op.Card.Deputy3Id,deputy3Level: op.Card.Deputy3Level,
                    deputy4Id: op.Card.Deputy4Id,deputy4Level: op.Card.Deputy4Level),
                op.IsChallenger,
                op.InstanceId);

        currentChessAnimation = ChessAnimation(data);
        RoundCount = 0;
        StartCoroutine(currentChessAnimation);
    }
    public void PlayResult(WarRecord data)
    {
        //Infoboard.transform.DOLocalMoveY(1440, 2);
        WarBoard.InitNewGame(false, true);
        foreach (var op in data.Chessmen)
            WarBoard.SetCustomInstanceCardToBoard(op.Pos, 
                GameCard.Instance(cardId: op.Card.CardId, type: op.Card.Type, level: op.Card.Level, arouse: op.Card.Arouse, 
                    deputy1Id: op.Card.Deputy1Id, deputy1Level: op.Card.Deputy1Level,
                    deputy2Id: op.Card.Deputy2Id, deputy2Level: op.Card.Deputy2Level,
                    deputy3Id: op.Card.Deputy3Id, deputy3Level: op.Card.Deputy3Level,
                    deputy4Id: op.Card.Deputy4Id, deputy4Level: op.Card.Deputy4Level),
                op.IsChallenger,
                op.InstanceId);

        currentChessAnimation = ChessAnimation(data);
        RoundCount = 0;
        StartCoroutine(currentChessAnimation);
    }

    private IEnumerator currentChessAnimation { get; set; }

    public void ReadyWarboard(bool isChallengerWin, List<ChessRound> rounds, List<IOperatorInfo> ops)
    {
        var result = new WarResult(isChallengerWin, ops, rounds);
        WarBoard.InitNewGame(true, true);
        foreach (var op in result.Chessmen)
        {
            var card = new FightCardData(GameCard.Instance(cardId: op.Card.CardId, type: op.Card.Type, level: op.Card.Level, arouse: op.Card.Arouse,
                deputy1Id: op.Card.Deputy1Id, deputy1Level: op.Card.Deputy1Level,
                deputy2Id: op.Card.Deputy2Id, deputy2Level: op.Card.Deputy2Level,
                deputy3Id: op.Card.Deputy3Id, deputy3Level: op.Card.Deputy3Level,
                deputy4Id: op.Card.Deputy4Id, deputy4Level: op.Card.Deputy4Level));
            card.SetPos(op.Pos);
            card.SetInstanceId(op.InstanceId);
            card.isPlayerCard = op.IsChallenger;
            ChessboardManager.InstanceChessman(card);
        }
        WarBoard.gameObject.SetActive(true);
        WarBoard.Chessboard.RemoveAllStartClicks();
        WarBoard.Chessboard.SetStartWarUi(() =>
        {
            WarBoard.Chessboard.DisplayStartButton(false);
            PlayResult(result);
            XButton.interactable = false;
        });
        XButton.interactable = true;
    }
    
    public void ReadyWarboard(bool isChallengerWin, List<ChessRoundRecord> records, List<IOperatorInfo> ops)
    {
        var result = new WarRecord(isChallengerWin, ops, records);
        WarBoard.InitNewGame(true, true);
        foreach (var op in result.Chessmen)
        {
            var card = new FightCardData(
                GameCard.Instance(cardId: op.Card.CardId, type: op.Card.Type, level: op.Card.Level, arouse: op.Card.Arouse,
                    deputy1Id: op.Card.Deputy1Id, deputy1Level: op.Card.Deputy1Level,
                    deputy2Id: op.Card.Deputy2Id, deputy2Level: op.Card.Deputy2Level,
                    deputy3Id: op.Card.Deputy3Id, deputy3Level: op.Card.Deputy3Level,
                    deputy4Id: op.Card.Deputy4Id, deputy4Level: op.Card.Deputy4Level
                ));
            card.SetPos(op.Pos);
            card.SetInstanceId(op.InstanceId);
            card.isPlayerCard = op.IsChallenger;
            ChessboardManager.InstanceChessman(card);
        }
        WarBoard.gameObject.SetActive(true);
        WarBoard.Chessboard.RemoveAllStartClicks();
        WarBoard.Chessboard.SetStartWarUi(() =>
        {
            WarBoard.Chessboard.DisplayStartButton(false);
            PlayResult(result);
            XButton.interactable = false;
        });
        XButton.interactable = true;
    }

    private IEnumerator ChessAnimation(WarResult result)
    {
        Time.timeScale = GamePref.PrefWarSpeed;
        yield return WarBoard.AnimRounds(result.Rounds, false);
        Time.timeScale = 1f;
        if (result.IsChallengerWin)
            yield return WarBoard.ChallengerWinAnimation();
        _versusWindow.Open(result.IsChallengerWin, () => WarboardActive(false));
    }
    private IEnumerator ChessAnimation(WarRecord result)
    {
        Time.timeScale = GamePref.PrefWarSpeed;
        yield return WarBoard.AnimRounds(result.Records, false);
        Time.timeScale = 1f;
        if (result.IsChallengerWin)
            yield return WarBoard.ChallengerWinAnimation();
        _versusWindow.Open(result.IsChallengerWin, () => WarboardActive(false));
    }

    private void WarboardActive(bool isActive)
    {
        WarBoard.gameObject.SetActive(isActive);
        BlockingPanel.gameObject.SetActive(isActive);
        if (!isActive)
        {
            EffectsPoolingControl.instance.ResetPools();
            Time.timeScale = 1.5f;
            AudioController1.instance.FadeEndMusic();
            WarBoard.OnRoundStart -= OnEveryRound;
            roundUi.Off();
            WarBoard.InitNewGame(false, false);
        }

        WarBoard.Chessboard.DisplayStartButton(true);
    }

    public void StartNewGame()
    {
        WarBoard.InitNewGame(true, true);
        var card = new FightCardData(
            GameCard.Instance(cardId: 0, type: (int)GameCardType.Base, level: CityLevel));
        card.SetPos(17);
        WarBoard.SetPlayerBase(card);
        WarBoard.Chessboard.UpdateWarSpeed();
    }

    public void SetEnemyFormation(Dictionary<int, IGameCard> formation)
    {
        var b = formation[17];
        var enemyBase = new FightCardData(GameCard.Instance(cardId: b.CardId, type: b.Type, level: b.Level, arouse: b.Arouse,
            deputy1Id: b.Deputy1Id, deputy1Level: b.Deputy1Level,
            deputy2Id: b.Deputy2Id, deputy2Level: b.Deputy2Level,
            deputy3Id: b.Deputy3Id, deputy3Level: b.Deputy3Level,
            deputy4Id: b.Deputy4Id, deputy4Level: b.Deputy4Level));
        enemyBase.SetPos(17);
        var list = new List<ChessCard>();
        foreach (var o in formation)
        {
            var pos = o.Key;
            if (pos == 17) continue;
            var card = o.Value;
            list.Add(InstanceCard(card, pos));
        }

        WarBoard.SetEnemiesIncludeUis(enemyBase, list);

        ChessCard InstanceCard(IGameCard c, int p) => ChessCard.Instance(c.CardId, c.Type, c.Level, arouse: c.Arouse, pos: p);
    }

    #endregion

    private float lastDelta;
    void Update()
    {
        lastDelta += Time.deltaTime;
        if (lastDelta < 1) return;
        lastDelta = Time.deltaTime;
        UpdateChallengerTimer();
        UpdateEverySecond?.Invoke();
    }

    private void UpdateChallengerTimer()
    {
        foreach (var obj in ChallengeExpSet.ToDictionary(c => c.Key, c => c.Value))
        {
            var warId = obj.Key;
            var uiTimer = obj.Value;
            var milSecs = uiTimer.ExpiredTime - SysTime.UnixNow;
            if (milSecs < 0)
            {
                ChallengeExpSet.Remove(warId);
                DisplayWarlistPage();
                continue;
            }

            var timeSpan = TimeSpan.FromMilliseconds(milSecs);
            uiTimer.Uis = uiTimer.Uis.Where(u => u != null).ToList();
            foreach (var ui in uiTimer.Uis)
                UpdateTimerUi(timeSpan, ui);
        }

        void UpdateTimerUi(TimeSpan ts, Text u) =>
            u.text = $"{(int)ts.TotalMinutes}:{ts.Seconds:00}";
    }

    public class Card : IGameCard
    {
        public int CardId { get; set; }
        public int Level { get; set; }
        public int Chips { get; set; }
        public int Type { get; set; }
        public int Arouse { get; set; }
        public int Deputy1Id { get; set; } = -1;
        public int Deputy1Level { get; set; }
        public int Deputy2Id { get; set; } = -1;
        public int Deputy2Level { get; set;  }
        public int Deputy3Id { get; set; } = -1;
        public int Deputy3Level { get; set; }
        public int Deputy4Id { get; set; } = -1;
        public int Deputy4Level { get; set; }

        public Card()
        {
        }

        public Card(IGameCard c)
        {
            CardId = c.CardId;
            Level = c.Level;
            Chips = c.Chips;
            Type = c.Type;
            Arouse = c.Arouse;
            Deputy1Id = c.Deputy1Id;
            Deputy1Level = c.Deputy1Level;
            Deputy2Id = c.Deputy2Id;
            Deputy2Level = c.Deputy2Level;
            Deputy3Id = c.Deputy3Id;
            Deputy3Level = c.Deputy3Level;
            Deputy4Id = c.Deputy4Id;
            Deputy4Level = c.Deputy4Level;
        }

    }

    public class WarResult 
    {
        public List<Operator> Chessmen { get; set; }

        public List<ChessRound> Rounds { get; set; }
        public bool IsChallengerWin { get; set; }
        public WarResult()
        {
            
        }
        public WarResult(bool isChallengerWin, List<IOperatorInfo> chessmen, List<ChessRound> rounds)
        {
            IsChallengerWin = isChallengerWin;
            Chessmen = chessmen
                .Select(o => new Operator(o.InstanceId, o.Pos, o.IsChallenger, new Card(o.Card)))
                .ToList();
            Rounds = rounds;
        }

        public class Operator : IOperatorInfo
        {
            public int InstanceId { get; set; }
            public int Pos { get; set; }
            public bool IsChallenger { get; set; }
            public Card Card { get; set; }
            IGameCard IOperatorInfo.Card => Card;

            public Operator()
            {
                
            }
            public Operator(int instanceId, int pos, bool isChallenger, Card card)
            {
                InstanceId = instanceId;
                Pos = pos;
                IsChallenger = isChallenger;
                Card = card;
            }
        }

    }
    public class WarRecord
    {
        public List<Operator> Chessmen { get; set; }

        public List<ChessRoundRecord> Records { get; set; }
        public bool IsChallengerWin { get; set; }

        public WarRecord()
        {
            
        }
        public WarRecord(bool isChallengerWin, List<IOperatorInfo> chessmen, List<ChessRoundRecord> records)
        {
            IsChallengerWin = isChallengerWin;
            Chessmen = chessmen
                .Select(o => new Operator(o.InstanceId, o.Pos, o.IsChallenger, new Card(o.Card)))
                .ToList();
            Records = records;
        }

        public class Operator : IOperatorInfo
        {
            public int InstanceId { get; set; }
            public int Pos { get; set; }
            public bool IsChallenger { get; set; }
            public Card Card { get; set; }
            IGameCard IOperatorInfo.Card => Card;

            public Operator()
            {
                
            }
            public Operator(int instanceId, int pos, bool isChallenger, Card card)
            {
                InstanceId = instanceId;
                Pos = pos;
                IsChallenger = isChallenger;
                Card = card;
            }
        }

    }

    public class ChallengeDto 
    {
        public int WarId { get; set; }
        public int WarIsd { get; set; }
        public int CharacterId { get; set; }
        public string HostName { get; set; }
        public int HostId { get; set; }
        public long StartTime { get; set; }
        public int Merit { get; set; }
        public int TotalMerit { get; set; }
        public long ExpiredTime { get; set; }

        /// <summary>
        /// key = index, value = units(ex -17)
        /// </summary>
        public Dictionary<int, int> StageUnit { get; set; }

        /// <summary>
        /// key = index, value = nextPoints
        /// </summary>
        public Dictionary<int, int[]> Checkpoints { get; set; }
        /// <summary>
        /// key = index
        /// </summary>
        public Dictionary<int, bool> StageProgress { get; set; }
        public int[] CurrentPoints { get; set; }
        public int TroopId { get; set; }
    }

    [Serializable]
    private class VersusWindow
    {
        public Image Window;
        public Button CloseButton;
        public GameObject[] WinObjs;
        public GameObject[] LoseObjs;
        private GameObjectSwitch<bool> _objectSwitch;

        private GameObjectSwitch<bool> ObjectSwitch
        {
            get
            {
                if (_objectSwitch == null)
                {
                    _objectSwitch = new GameObjectSwitch<bool>(new[]
                    {
                        (true, WinObjs),
                        (false,LoseObjs)
                    });
                }
                return _objectSwitch;
            }
        }

        public void Open(bool isWin, UnityAction onCloseWindowAction)
        {
            Window.gameObject.SetActive(true);
            CloseButton.onClick.RemoveAllListeners();
            CloseButton.onClick.AddListener(() =>
            {
                Window.gameObject.SetActive(false);
                onCloseWindowAction?.Invoke();
            });
            ObjectSwitch.Set(isWin);
        }

        public void Close() => Window.gameObject.SetActive(false);
    }

    public void GetBackToWarListPage(string text)
    {
        DisplayWarlistPage();
        PlayerDataForGame.instance.ShowStringTips(text);
    }

    //key = warId
    private static readonly Dictionary<int, ChallengerUiTimer> ChallengeExpSet =
        new Dictionary<int, ChallengerUiTimer>();

    public void RegChallengeUi(ChallengeDto challenge, Text text)
    {
        var warId = challenge.WarId;
        //如果有相同的warId但过期时间不同，去除旧的UI更新
        if (ChallengeExpSet.ContainsKey(warId) &&
            ChallengeExpSet[warId].ExpiredTime != challenge.ExpiredTime)
            ChallengeExpSet.Remove(warId);
        //如果表中无warId，生成
        if (!ChallengeExpSet.ContainsKey(warId))
            ChallengeExpSet.Add(warId, new ChallengerUiTimer(challenge.ExpiredTime));

        if (ChallengeExpSet[warId].Uis.Contains(text)) return;
        ChallengeExpSet[warId].Uis.Add(text);
        UpdateChallengerTimer();
    }

    public void RemoveFromTimer(Text text)
    {
        foreach (var cha in ChallengeExpSet.Where(cha => cha.Value.Uis.Contains(text))) cha.Value.Uis.Remove(text);
    }

    private class ChallengerUiTimer
    {
        public long ExpiredTime { get; }
        public List<Text> Uis { get; set; } 
        public ChallengerUiTimer(long expiredTime)
        {
            ExpiredTime = expiredTime;
            Uis = new List<Text>();
        }

    }

    public class RkCheckpoint
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public int[] NextPoints { get; set; }
        public int EventType { get; set; }
        public int MaxCards { get; set; }
        public int MaxRounds { get; set; }
        public int FormationCount { get; set; }

        public RkCheckpoint(int index, string title, int eventType, int maxCards, int maxRounds, int[] nextPoints, int formationCount)
        {
            Index = index;
            Title = title;
            EventType = eventType;
            MaxCards = maxCards;
            MaxRounds = maxRounds;
            NextPoints = nextPoints;
            FormationCount = formationCount;
        }
    }


    public class RkWarDto
    {
        public int WarId { get; set; }
        public int Index { get; set; }
        public Dictionary<int, Rank> RankingBoard { get; set; }

        public RkWarDto()
        {
        }
        public RkWarDto(int warId, int index, Dictionary<int, Rank> rankingBoard)
        {
            WarId = warId;
            Index = index;
            RankingBoard = rankingBoard;
        }

        public class Rank
        {
            public int WarIsd { get; set; }
            public int HostId { get; set; }
            public int TroopId { get; set; }
            public string CharName { get; set; }
            public int MPower { get; set; }
            public Rank()
            {

            }
        }
    }
    public enum RkTimerState
    {
        Process = 0,
        Done = 1,
        Finalized = 2,
        Cancelled = 3
    }

    public class RkTimerDto
    {
        public int State { get; set; }
        [JsonIgnore]public RkTimerState Status => (RkTimerState)State;
        public long TimeOut { get; set; }
        public long Finalize { get; set; }
    }

    [Serializable]private class WbCancelWindow
    {
        public GameObject Window;
        public Button CancelButton;

        public void Display(UnityAction action)
        {
            Window.gameObject.SetActive(true);
            CancelButton.onClick.RemoveAllListeners();
            CancelButton.onClick.AddListener(() => Window.SetActive(false));
            CancelButton.onClick.AddListener(action);
        }
    }

    public void SetState(RkTimerDto state) => RkState = state;

    private void RkTimerUpdater()
    {
        if(RkState==null)
        {
            SetRkTimer(string.Empty);
            return;
        }
        switch (RkState.Status)
        {
            case RkTimerState.Process:
            {
                var ts = SysTime.UtcFromUnixTicks(RkState.TimeOut).ToLocalTime() - DateTime.Now;
                SetRkTimer($"{ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}");
            }
                break;
            case RkTimerState.Done:
            {
                //var ts = SysTime.UtcFromUnixTicks(RkState.Finalize).ToLocalTime() - DateTime.Now;
                //SetRkTimer($"{ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}");
                SetRkTimer("战场清扫中...");
            }
                break;
            case RkTimerState.Finalized:
            case RkTimerState.Cancelled:
                SetRkTimer(string.Empty);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    public void SetRkTimer(string text) => RkTimer.text = text;
    public void Display(bool display)
    {
        gameObject.SetActive(display);
        if (display)
        {
            Restrict.Set();
            DisplayWarlistPage();
        }
        else Restrict.Reset();
    }

    [Serializable]
    private class VersusRestrictUi
    {
        public Image Window;
        public Text LevelNotReach;
        public Button CreateCharacterButton;
        public int MinLevel = 3;

        public void Init(UIManager uiMgr, PlayerCharacterUi ui)
        {
            CreateCharacterButton.onClick.RemoveAllListeners();
            ui.OnCloseAction -= SwitchMainPage;
            ui.OnCloseAction += SwitchMainPage;
            CreateCharacterButton.onClick.AddListener(ui.Show);
            LevelNotReach.text = $"等级{MinLevel}开启！！";
            void SwitchMainPage() => uiMgr.MainPageSwitching(3);
        }


        public void Set()
        {
            Window.gameObject.SetActive(true);
            LevelNotReach.gameObject.SetActive(false);
            CreateCharacterButton.gameObject.SetActive(false);
            if (PlayerDataForGame.instance.pyData.Level < MinLevel)
            {
                LevelNotReach.gameObject.SetActive(true);
                return;
            }

            var hasCharacter = PlayerDataForGame.instance.Character != null &&
                               PlayerDataForGame.instance.Character.IsValidCharacter();
            if (!hasCharacter)
            {
                CreateCharacterButton.gameObject.SetActive(true);
                return;
            }
            Window.gameObject.SetActive(false);
        }

        public void Reset()
        {
            Window.gameObject.SetActive(false);
            LevelNotReach.gameObject.SetActive(false);
            CreateCharacterButton.gameObject.SetActive(false);
        }
    }

    [Serializable]
    private class RoundUi
    {
        public GameObject Ui;
        public Text RoundText;

        public void Off() => Ui.gameObject.SetActive(false);

        public void SetRound(int round)
        {
            Ui.gameObject.SetActive(true);
            RoundText.text = $"{round}";
        }
    }

    public void PlayClickAudio() => PlayAudio(AudioFields.ClickAud);
    public void PlayChallengeAudio() => PlayAudio(AudioFields.StartChallengeAud);
    public void PlayAttackCityAudio() => PlayAudio(AudioFields.AttackCityAud);
    public void PlayChessboard() => PlayAudio(AudioFields.StartPlayChessboardAud);

    private void PlayAudio(int id) => AudioController0.instance.StackPlaying(id);

    [Serializable]
    private class AudioField
    {
        [Header("点击音效")] public int ClickAud;
        [Header("开始挑战")] public int StartChallengeAud;
        [Header("攻击城池")] public int AttackCityAud;
        [Header("开始战斗")] public int StartPlayChessboardAud;
    }
}