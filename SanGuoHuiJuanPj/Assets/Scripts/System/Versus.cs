using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
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

    private static Versus instance;
#if UNITY_EDITOR
    public static string RkApi { get; } = "https://localhost:5001/api/rkwar";
    
    public const int TestCharId = -7;

    public const string GetWarsV1 = "GetWarsV1";

    public static void GetRkWars(Action<string> onRefreshWarList) =>
        Http.Get($"{RkApi}/{GetWarsV1}?charId={TestCharId}", onRefreshWarList, GetWarsV1);

    public const string GetWarInfoApi = "GetWarInfoV1";

    public static void RkWarStageInfo(int warId,int warIsd ,Action<string> onApiAction) => Http.Get(
        $"{RkApi}/{GetWarInfoApi}?warId={warId}&warIsd={warIsd}&charId={TestCharId}", onApiAction, GetWarInfoApi);

    public const string StartChallengeV1 = "StartChallengeV1";

    public static void RkStartChallenge(int warId, int warIsd, Action<string> onChallengeRespond) =>
        Http.Post($"{RkApi}/{StartChallengeV1}?charId={TestCharId}&warId={warId}&warIsd={warIsd}", string.Empty,
            onChallengeRespond, StartChallengeV1);

    public const string GetCheckpointFormationV1 = "GetCheckpointFormationV1";

    public static void RkGetCheckpointFormation(int warId, int index, Action<string> callBackAction) => 
        Http.Get(
            $"{RkApi}/{GetCheckpointFormationV1}?warId={warId}&charId={TestCharId}&index={index}",
            callBackAction, GetCheckpointFormationV1);

    public const string SubmitFormationV1 = "SubmitFormationV1";

    public static void RkPostSubmitFormation(int warId, int index, string content,
        Action<string> onCallBack) =>
        Http.Post($"{RkApi}/{SubmitFormationV1}?charId={TestCharId}&warId={warId}&index={index}",
            content, onCallBack,
            SubmitFormationV1);

    public const string CheckPointWarResultV1 = "CheckPointResultV1";
    public static void RkGetCheckPointWarResult(int warId, int index, Action<string> callbackAction) =>
        Http.Get($"{RkApi}/{CheckPointWarResultV1}?warId={warId}&index={index}&charId={TestCharId}", callbackAction,
            CheckPointWarResultV1);

    public const string CancelChallengeV1 = "CancelChallengeV1";
    public static void RkPostCancelChallenge(int warId, Action<string> callbackAction) =>
        Http.Post($"{RkApi}/{CancelChallengeV1}?warId={warId}&charId={TestCharId}", string.Empty, callbackAction,
            CancelChallengeV1);

#endif

    public static int CharId { get; private set; }

    [SerializeField] private WarBoardUi WarBoard;
    [SerializeField] private ChessboardVisualizeManager ChessboardManager;
    [SerializeField] private Image Infoboard;
    [SerializeField] private int MaxCards;
    [SerializeField] private VsWarStageController warStageController;
    [SerializeField] private VersusWindow _versusWindow;
    [SerializeField] private Image BlockingPanel;
    public VsWarListController warListController;

    public int CityLevel = 1;

    public void Init(int charId)
    {
        instance = this;
        CharId = charId;
        if (!EffectsPoolingControl.instance.IsInit)
            EffectsPoolingControl.instance.Init();
        WarBoard.Init();
        WarBoard.MaxCards = MaxCards;
        ChessboardManager.Init(WarBoard.Chessboard, WarBoard.JiBanManager);
        ControllerInit();
    }

#if UNITY_EDITOR
    void Start()
    {
        DataTable.instance.Init();
        Init(TestCharId); 
    } 
#endif

    private void ControllerInit()
    {
        warListController.Init(this, OnSelectedWar);
        warStageController.Init(this, OnReadyWarboard);
    }
    
    private void OnReadyWarboard((int warId,int warIsd, int pointId, int maxCards) o, Dictionary<int, IGameCard> formation)
    {
        StartNewGame();
        SetEnemyFormation(formation);
        InitCardToRack();
        MaxCards = o.maxCards;
        WarBoard.MaxCards = o.maxCards;
        WarBoard.Chessboard.StartButton.onClick.RemoveAllListeners();
        WarBoard.Chessboard.StartButton.onClick.AddListener(() =>
            OnSubmitFormation(o.warId, o.warIsd, o.pointId));
        WarboardActive(true);
    }

    private enum ChallengeResult
    {
        NotFound,
        InProgress,
        Clear
    }

    private void OnSubmitFormation(int warId, int warIsd, int pointId)
    {
        WarBoard.Chessboard.StartButton.GetComponent<Animator>().SetBool(WarBoardUi.ButtonTrigger, false);
        var challengerFormation = WarBoard.PlayerScope.ToDictionary(c => c.Pos, c => new Card(c.Card) as IGameCard);
        var json = Json.Serialize(challengerFormation);
#if UNITY_EDITOR
        RkPostSubmitFormation(warId, pointId, json, OnCallBack);
#endif

        void OnCallBack(string data)
        {
            var bag = DataBag.DeserializeBag(data);
            if (bag == null)
            {
                ShowHints(data);
                return;
            }
            var wId = bag.Get<int>(0);
            var isChallengerWin = bag.Get<bool>(1);
            var rounds = bag.Get<List<ChessRound>>(2);
            var chessmen = bag.Get<List<WarResult.Operator>>(3);
            var cr = bag.Get<int>(4);
            var result = (ChallengeResult)cr;
            PlayResult(new WarResult(isChallengerWin, chessmen.Cast<IOperatorInfo>().ToList(), rounds));
            warListController.GetWarList();
            OnSelectedWar(wId, warIsd);
        }
    }

    public void DisplayWarlistPage(bool refresh)
    {
        if (refresh)
            warListController.GetWarList();
        warListController.Display(true);
        warStageController.Display(false);
    }

    private void OnSelectedWar(int warId,int warIsd)
    {
        warListController.Display(false);
        warStageController.Set(warId, warIsd);
    }

    #region PlayerVersus

    //初始化卡牌列表
    private void InitCardToRack()
    {
        var forceId = PlayerDataForGame.instance.CurrentWarForceId;
        var hstData = PlayerDataForGame.instance.hstData;
#if UNITY_EDITOR
        if (forceId == -2) //-2为测试用不重置卡牌，直接沿用卡牌上的阵容
        {
            hstData.heroSaveData.ForEach(WarBoard.CreateCardToRack);
            hstData.towerSaveData.ForEach(WarBoard.CreateCardToRack);
            hstData.trapSaveData.ForEach(WarBoard.CreateCardToRack);
            return;
        }
#endif
        PlayerDataForGame.instance.fightHeroId.Clear();
        PlayerDataForGame.instance.fightTowerId.Clear();
        PlayerDataForGame.instance.fightTrapId.Clear();

        //临时记录武将存档信息
        hstData.heroSaveData.Enlist(forceId).ToList()
            .ForEach(WarBoard.CreateCardToRack);
        hstData.towerSaveData.Enlist(forceId).ToList()
            .ForEach(WarBoard.CreateCardToRack);
        hstData.trapSaveData.Enlist(forceId).ToList()
            .ForEach(WarBoard.CreateCardToRack);
    }

    public void PlayResult(WarResult data)
    {
        Infoboard.transform.DOLocalMoveY(1440, 2);
        WarBoard.NewGame(false);
        foreach (var op in data.Chessmen)
        {
            var card = new FightCardData(GameCard.Instance(op.Card.CardId, op.Card.Type, op.Card.Level));
            card.SetPos(op.Pos);
            card.SetInstanceId(op.InstanceId);
            card.isPlayerCard = op.IsChallenger;
            ChessboardManager.InstanceChessman(card);
        }

        StartCoroutine(ChessAnimation(data));
    }

    public void ReadyWarboard(bool isChallengerWin, List<ChessRound> rounds,
        List<IOperatorInfo> ops)
    {
        var result = new WarResult(isChallengerWin, ops, rounds);
        WarBoard.NewGame(true);
        foreach (var op in result.Chessmen)
        {
            var card = new FightCardData(GameCard.Instance(op.Card.CardId, op.Card.Type, op.Card.Level));
            card.SetPos(op.Pos);
            card.SetInstanceId(op.InstanceId);
            card.isPlayerCard = op.IsChallenger;
            ChessboardManager.InstanceChessman(card);
        }
        WarBoard.gameObject.SetActive(true);
        WarBoard.Chessboard.StartButton.onClick.RemoveAllListeners();
        WarBoard.Chessboard.StartButton.onClick.AddListener(() =>
        {
            WarBoard.Chessboard.StartButton.GetComponent<Animator>().SetBool(WarBoardUi.ButtonTrigger, false);
            PlayResult(result);
        });
    }

    private IEnumerator ChessAnimation(WarResult result)
    {
        Time.timeScale = GamePref.PrefWarSpeed;
        yield return WarBoard.AnimRounds(result.Rounds);
        Time.timeScale = 1f;
        if (result.IsChallengerWin)
            yield return WarBoard.ChallengerWinAnimation();
        _versusWindow.Open(result.IsChallengerWin, () => WarboardActive(false));
    }

    private void WarboardActive(bool isActive)
    {
        WarBoard.gameObject.SetActive(isActive);
        BlockingPanel.gameObject.SetActive(isActive);
    }

    public void StartNewGame()
    {
        WarBoard.NewGame(true);
        var card = new FightCardData(GameCard.Instance(0, (int)GameCardType.Base, CityLevel));
        card.SetPos(17);
        WarBoard.SetPlayerBase(card);
        WarBoard.Chessboard.UpdateWarSpeed();
    }

    public void SetEnemyFormation(Dictionary<int, IGameCard> formation)
    {
        var b = formation[17];
        var enemyBase = new FightCardData(GameCard.Instance(b.CardId, b.Type, b.Level));
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

        ChessCard InstanceCard(IGameCard c, int p) => ChessCard.Instance(c.CardId, c.Type, c.Level, p);
    }

    #endregion

    private float lastDelta;
    void Update()
    {
        lastDelta += Time.deltaTime;
        if (lastDelta < 1) return;
        lastDelta = Time.deltaTime;
        UpdateChallengerTimer();
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
                DisplayWarlistPage(true);
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

        public Card()
        {

        }

        public Card(IGameCard c)
        {
            CardId = c.CardId;
            Level = c.Level;
            Chips = c.Chips;
            Type = c.Type;
        }

    }

    public class WarResult : DataBag
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

    public class ChallengeDto : DataBag
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

    public static void ShowHints(string text) => PlayerDataForGame.instance.ShowStringTips(text);

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
            public string CharName { get; set; }
            public int MPower { get; set; }

            public Rank()
            {

            }
            public Rank(int warIsd, int hostId, string charName, int mPower)
            {
                WarIsd = warIsd;
                HostId = hostId;
                CharName = charName;
                MPower = mPower;
            }
        }
    }
}