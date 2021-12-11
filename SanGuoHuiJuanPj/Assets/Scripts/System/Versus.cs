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
    public static string Api = "https://localhost:5001/api/spwar";

    public const int TestCharId = 55;
    public const string SubmitFormationV1 = "SubmitFormationV1";
    public const string GetCheckpointFormationV1 = "GetCheckpointFormationV1";
    public const string StartChallengeV1 = "StartChallengeV1";
    public const string WarStageInfoApi = "GetWarInfoV1";
    public const string GetWarsV1 = "GetWarsV1";

    [SerializeField] private WarBoardUi WarBoard;
    [SerializeField] private ChessboardVisualizeManager ChessboardManager;
    [SerializeField] private Image Infoboard;
    [SerializeField] private int MaxCards;
    [SerializeField] private VsWarListController warListController;
    [SerializeField] private VsWarStageController warStageController;
    [SerializeField] private VersusWindow _versusWindow;
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

    void Start() => Init();

    public void ControllerInit()
    {
        warListController.Init(OnSelectedWar);
        warStageController.Init(OnWarListDisplay, OnReadyWarboard);
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
        Http.Post($"{Api}/{SubmitFormationV1}?charId={TestCharId}&warId={warId}&pointId={pointId}", json, OnCallBack,
            SubmitFormationV1);

        void OnCallBack(string data)
        {
            var bag = DataBag.DeserializeBag(data);
            if (bag == null)
                throw new NotImplementedException();
            var isChallengerWin = bag.Get<bool>(0);
            var rounds = bag.Get<List<ChessRound>>(1);
            var chessmen = bag.Get<List<WarResult.Operator>>(2);
            PlayResult(new WarResult(isChallengerWin, chessmen.Cast<IOperatorInfo>().ToList(), rounds));
            OnSelectedWar(warId);
        }
    }

    private void OnWarListDisplay()
    {
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
#if UNITY_EDITOR
        if (forceId == -2) //-2为测试用不重置卡牌，直接沿用卡牌上的阵容
        {
            PlayerDataForGame.instance.fightHeroId.Select(id => new GameCard().Instance(GameCardType.Hero, id, 1))
                .Concat(PlayerDataForGame.instance.fightTowerId.Select(id =>
                    new GameCard().Instance(GameCardType.Tower, id, 1)))
                .Concat(PlayerDataForGame.instance.fightTrapId.Select(id =>
                    new GameCard().Instance(GameCardType.Trap, id, 1)))
                .ToList().ForEach(WarBoard.CreateCardToRack);
            return;
        }
#endif
        PlayerDataForGame.instance.fightHeroId.Clear();
        PlayerDataForGame.instance.fightTowerId.Clear();
        PlayerDataForGame.instance.fightTrapId.Clear();

        var hstData = PlayerDataForGame.instance.hstData;
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

    private IEnumerator ChessAnimation(WarResult result)
    {
        yield return WarBoard.AnimRounds(result.Rounds);
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
}