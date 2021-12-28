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
    public enum SpWarIdentity
    {
        Anonymous = 0,//一般人
        Challenger = 1,//挑战者
        Host = 2,//关主
        PrevHost = 3,//上个关主
        PrevChallenger = 4//过期关卡的挑战者
    }

#if UNITY_EDITOR
    public static string SpApi { get; } = "https://localhost:5001/api/spwar";
    public static string RkApi { get; } = "https://localhost:5001/api/rkwar";
    
    public const int TestCharId = -2;

    public const string GetWarsV1 = "GetWarsV1";

    public static void GetRkWars(Action<string> onRefreshWarList) =>
        Http.Get($"{RkApi}/{GetWarsV1}?charId={TestCharId}", onRefreshWarList, GetWarsV1);
    public static void GetSpWars(Action<string> onRefreshWarList) =>
        Http.Get($"{SpApi}/{GetWarsV1}?charId={TestCharId}", onRefreshWarList, GetWarsV1);
    
    public const string GetWarInfoApi = "GetWarInfoV1";

    public static void RkWarStageInfo(int warId, Action<string> onApiAction, int? hostId = null) => Http.Get(
        $"{RkApi}/{GetWarInfoApi}?warId={warId}&charId={TestCharId}" +
        (hostId.HasValue ? $"&hostId={hostId.Value}" : string.Empty), onApiAction, GetWarInfoApi);
    public static void SpWarStageInfo(int warId, Action<string> onApiAction) => Http.Get($"{SpApi}/{GetWarInfoApi}?warId={warId}&charId={TestCharId}", onApiAction, GetWarInfoApi);

    public const string StartChallengeV1 = "StartChallengeV1";
    public static void StartChallenge(int warId, Action<string> onChallengeRespond) =>
        Http.Post($"{SpApi}/{StartChallengeV1}?charId={TestCharId}&warId={warId}", string.Empty,
            onChallengeRespond, StartChallengeV1);

    public const string GetCheckpointFormationV1 = "GetCheckpointFormationV1";
    public static void GetCheckpointFormation(int warId, int checkpointId, Action<string> callBackAction) =>
        Http.Get($"{SpApi}/{GetCheckpointFormationV1}?warId={warId}&charId={TestCharId}&pointId={checkpointId}",
            callBackAction, GetCheckpointFormationV1);

    public const string SubmitFormationV1 = "SubmitFormationV1";
    public static void PostSubmitFormation(int warId,int pointId,string content,Action<string> onCallBack) =>
        Http.Post($"{SpApi}/{SubmitFormationV1}?charId={TestCharId}&warId={warId}&pointId={pointId}", content, onCallBack,
            SubmitFormationV1);

    public const string CheckPointWarResultV1 = "CheckPointResultV1";
    public static void GetCheckPointWarResult(int warId, int pointId, Action<string> callbackAction) =>
        Http.Get($"{SpApi}/{CheckPointWarResultV1}?warId={warId}&pointId={pointId}&charId={TestCharId}", callbackAction,
            CheckPointWarResultV1);

    public const string CancelChallengeV1 = "CancelChallengeV1";
    public static void PostCancelChallenge(int warId, Action<string> callbackAction) =>
        Http.Post($"{SpApi}/{CancelChallengeV1}?warId={warId}&charId={TestCharId}", string.Empty, callbackAction,
            CancelChallengeV1);

#endif
    [SerializeField] private WarBoardUi WarBoard;
    [SerializeField] private ChessboardVisualizeManager ChessboardManager;
    [SerializeField] private Image Infoboard;
    [SerializeField] private int MaxCards;
    [SerializeField] private VsWarStageController warStageController;
    [SerializeField] private VersusWindow _versusWindow;
    public VsWarListController warListController;
    /// <summary>
    /// key = warId
    /// </summary>
    private Dictionary<int,UnityAction<long>> challengeTimer = new Dictionary<int, UnityAction<long>>();
    public int CityLevel = 1;

    public void Init()
    {
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
        Init(); 
    } 
#endif

    private void ControllerInit()
    {
        warListController.Init(OnSelectedWar);
        warStageController.Init(this, OnReadyWarboard);
    }

    private void OnReadyWarboard(int warId, int pointId, int maxCards, Dictionary<int, IGameCard> formation)
    {
        StartNewGame();
        SetEnemyFormation(formation);
        InitCardToRack();
        MaxCards = maxCards;
        WarBoard.MaxCards = maxCards;
        WarBoard.gameObject.SetActive(true);
        WarBoard.Chessboard.StartButton.onClick.RemoveAllListeners();
        WarBoard.Chessboard.StartButton.onClick.AddListener(() => OnSubmitFormation(warId, pointId));

    }

    private void OnSubmitFormation(int warId, int pointId)
    {
        WarBoard.Chessboard.StartButton.GetComponent<Animator>().SetBool(WarBoardUi.ButtonTrigger, false);
        var challengerFormation = WarBoard.PlayerScope.ToDictionary(c => c.Pos, c => new Card(c.Card) as IGameCard);
        var json = Json.Serialize(challengerFormation);
#if UNITY_EDITOR
        PostSubmitFormation(warId, pointId, json, OnCallBack);
#endif

        void OnCallBack(string data)
        {
            var bag = DataBag.DeserializeBag(data);
            if (bag == null)
            {
                ShowHints(data);
                return;
            }
            var id = bag.Get<int>(0);
            var isChallengerWin = bag.Get<bool>(1);
            var rounds = bag.Get<List<ChessRound>>(2);
            var chessmen = bag.Get<List<WarResult.Operator>>(3);
            PlayResult(new WarResult(isChallengerWin, chessmen.Cast<IOperatorInfo>().ToList(), rounds));
            warListController.GetWarList();
            OnSelectedWar(id);
        }
    }

    public void DisplayWarlistPage(bool refresh)
    {
        if (refresh)
            warListController.GetWarList();
        warListController.Display(true);
        warStageController.Display(false);
    }

    private void OnSelectedWar(int warId)
    {
        warListController.Display(false);
        warStageController.Set(warId);
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

    public void ReadyWarboard(SpWarIdentity spWarIdentity, bool isChallengerWin, List<ChessRound> rounds,
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
        _versusWindow.Open(result.IsChallengerWin, () => WarBoard.gameObject.SetActive(false));
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
        foreach (var action in challengeTimer) action.Value.Invoke(SysTime.UnixNow);
    }

    public void RemoveChallengeTimer(int warId)
    {
        if (!challengeTimer.ContainsKey(warId))return;
        challengeTimer.Remove(warId);
    }
    public void RegChallengeTimer(int warId,long expiredTime ,UnityAction<TimeSpan> updateAction)
    {
        if (challengeTimer.ContainsKey(warId))
            challengeTimer.Remove(warId);
        challengeTimer.Add(warId, now => ExpiredUpdate(warId, now, expiredTime, updateAction));
    }
    private void ExpiredUpdate(int warId, long now, long expired, UnityAction<TimeSpan> updateAction)
    {
        var timeSpan = TimeSpan.FromMilliseconds(expired - now);
        updateAction(timeSpan);
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
}