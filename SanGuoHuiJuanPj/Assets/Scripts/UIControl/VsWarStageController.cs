using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VsWarStageController : MonoBehaviour
{

    [SerializeField] private CheckpointUi CheckpointUiPrefab;
    private static int[] RowSequence = new int[] { 2, 4, 0 };
    [SerializeField] private ScrollRect MapScrollRect;
    [SerializeField] private Button BackButton;
    [SerializeField] private Button ChallengeButton;
    [SerializeField] private Button CancelButton;
    [SerializeField] private UnityEvent onStartChallengeEvent;
    [SerializeField] private GameObject TimerObj;
    [SerializeField] private Text CountdownText;
    [SerializeField] private Image OppAvatar;
    [SerializeField] private Text OppMilitaryPowerText;
    [SerializeField] private Text OppNameText;
    [SerializeField] private Sprite[] genderSprites;
    [SerializeField] private CancelWindowUi CancelWindow;
    private int WarId { get; set; } = -1;
    private int WarIsd { get; set; } = -1;
    public int OppGender { get; private set; }
    public int OppMilitaryPower { get; private set; }
    private string HostName { get; set; }
    private int ChallengeRank { get; set; }
    private int PlayerRank { get; set; }
    private List<CheckpointUi> CpList { get; } = new List<CheckpointUi>();
    private List<Versus.RkCheckpoint> RkCheckPoints { get; set; }

    private ObjectPool<CheckpointUi> Pool { get; set; }
    //(warId , pointId)
    private UnityAction<(int, int, int, int), Dictionary<int, IGameCard>> OnAttackCity { get; set; }
    private Versus Vs { get; set; }

    public void Init(Versus versus, UnityAction<(int, int, int, int), Dictionary<int, IGameCard>> onAttackCityAction)
    {
        Vs = versus;
        OnAttackCity = onAttackCityAction;
        gameObject.SetActive(false);
        Pool = new ObjectPool<CheckpointUi>(() => Instantiate(CheckpointUiPrefab, MapScrollRect.content));
        CheckpointUiPrefab.gameObject.SetActive(false);
        BackButton.onClick.AddListener(() => versus.DisplayWarlistPage(false));
    }

    public void Set(int warId, int warIsd)
    {
        WarId = warId;
        WarIsd = warIsd;
        gameObject.SetActive(true);
        TimerObj.SetActive(false);
#if UNITY_EDITOR
        Versus.RkWarStageInfo(warId, warIsd, OnApiAction);
#endif

        void OnApiAction(string obj)
        {
            var bag = DataBag.DeserializeBag(obj);
            if (bag == null)
            {
                CancelWindow.SetCancel(()=>Vs.DisplayWarlistPage(true));
                return;
            }
            WarId = bag.Get<int>(0);//warId
            var host = bag.Get<List<object>>(1);
            var warIdentity = (Versus.WarIdentity)bag.Get<int>(2);
            var cpData = bag.Get<List<List<object>>>(3);
            var cps = new List<Versus.RkCheckpoint>();
            for (int i = 0; i < cpData.Count; i++)
            {
                var cp = cpData[i];

                var cpIndex = DataBag.Parse<int>(cp[0]); //0 index
                var cpTitle = cp[1].ToString(); //1 title
                var cpEventType = DataBag.Parse<int>(cp[2]); //2 cpEvent
                var cpMaxCards = DataBag.Parse<int>(cp[3]); //3 maxCard
                var cpMaxRounds = DataBag.Parse<int>(cp[4]); //4 maxRound
                var next = DataBag.Parse<int[]>(cp[5]); //5 maxRound
                var cpFormationCount = DataBag.Parse<int>(cp[6]); //6 formationCount
                cps.Add(new Versus.RkCheckpoint(index: cpIndex, title: cpTitle, eventType: cpEventType,
                    maxCards: cpMaxCards, cpMaxRounds, nextPoints: next, formationCount: cpFormationCount));
            }

            var challenge = bag.Get<Versus.ChallengeDto>(4);
            var rank = bag.Get<int>(5);
            var hostRank = bag.Get<int>(6);
            var hostName = host[0].ToString();
            var hostGender = DataBag.Parse<int>(host[1]);
            var hostMPower = DataBag.Parse<int>(host[2]);
            UpdatePage(hostName, hostGender, hostMPower, rank, hostRank, warIdentity, cps);
            if (challenge == null) return;
            UpdateCpProgress(warIdentity, challenge);
        }
    }

    private void UpdatePage(string hostName, int gender, int militaryPower, int playerRank, int hostRank,
        Versus.WarIdentity warIdentity, List<Versus.RkCheckpoint> cps)
    {
        HostName = hostName;
        PlayerRank = playerRank;
        ChallengeRank = hostRank;
        RkCheckPoints = cps;
        CancelWindow.Window.SetActive(false);
        if (CpList.Any())
        {
            CpList.ForEach(c => Pool.Recycle(c));
            CpList.Clear();
        }

        if (warIdentity == Versus.WarIdentity.Uncertain)
            CancelWindow.SetCancel(() => Versus.RkPostCancelChallenge(WarId, msg => Vs.DisplayWarlistPage(true)));

        SetOppInfo(gender, militaryPower);
        SetCancelButton(warIdentity);
        var isChallengeAvailable =
            warIdentity == Versus.WarIdentity.Anonymous //非关主或是挑战者身份
            && (playerRank < 0 || //不在排行榜上。
                playerRank > hostRank); //排位低于

        ChallengeBtnActive(isChallengeAvailable);
        UpdateCityUis(cps);
    }

    private void UpdateCityUis(List<Versus.RkCheckpoint> cps)
    {
        for (var row = cps.Count - 1; row >= 0; row--)
        {
            var sequenceIndex = row % RowSequence.Length;
            var cp = cps[row];
            for (int col = 0; col <= RowSequence.Max(); col++)
            {
                var ui = Pool.Get();
                ui.gameObject.SetActive(true);
                ui.Display(false);
                if (col == RowSequence[sequenceIndex])
                    ui.Set(cp, () => OnSelectedUi(cp.Index));
                CpList.Add(ui);
            }
        }
    }

    private void UpdateCpProgress(Versus.WarIdentity identity, Versus.ChallengeDto cha)
    {
        if (identity == Versus.WarIdentity.Challenger)
        {
            TimerObj.gameObject.SetActive(true);
            Vs.RemoveFromTimer(CountdownText);//移除公用倒计时UI。
            Vs.RegChallengeUi(cha, CountdownText);//重新注册当前挑战的时间
        }
        var progressList = cha.StageProgress.Join(CpList, p => p.Key, c => c.StageIndex, (p, c) => (c, p.Value))
            .ToList();
        for (var i = 0; i < progressList.Count; i++)
        {
            var (ui, isPass) = progressList[i];
            ui.SetProgress(isPass);
            if (isPass) ui.SetReportButton(() => OnReportAction(ui.StageIndex));
            else ui.SetAttackButton(() => OnAttackCheckpointAction(ui.StageIndex));
            var isChallengePoint = cha.CurrentPoints.Contains(i);
            ui.AttackButton.gameObject.SetActive(isChallengePoint);
            ui.SetDock(isChallengePoint);
        }
    }


    private void SetOppInfo(int gender, int militaryPower)
    {
        OppGender = gender;
        OppMilitaryPower = militaryPower;
        OppAvatar.sprite = genderSprites[gender];
        OppMilitaryPowerText.text = militaryPower.ToString();
        OppNameText.text = HostName;
    }

    private void SetCancelButton(Versus.WarIdentity warIdentity)
    {
        var isCancelAble = warIdentity == Versus.WarIdentity.Challenger;
        CancelButton.gameObject.SetActive(isCancelAble);
        if (!isCancelAble) return;
        CancelButton.onClick.RemoveAllListeners();
        CancelButton.onClick.AddListener(OnRequestCancel);
    }
    private void OnRequestCancel()
    {
#if UNITY_EDITOR
        Versus.RkPostCancelChallenge(WarId, _ => Vs.DisplayWarlistPage(true));
#endif
    }


    public void OnSelectedUi(int stageIndex) => CpList.ForEach(c => c.SetSelected(c.StageIndex == stageIndex));

    private void ChallengeBtnActive(bool isActive)
    {
        ChallengeButton.gameObject.SetActive(isActive);
        ChallengeButton.onClick.RemoveAllListeners();
        if (!isActive) return;
        ChallengeButton.onClick.AddListener(onStartChallengeEvent.Invoke);
        ChallengeButton.onClick.AddListener(RequestChallenge);
    }

    private void RequestChallenge()
    {
#if UNITY_EDITOR
        Versus.RkStartChallenge(WarId, WarIsd, OnChallengeRespond);
#endif

        void OnChallengeRespond(string databag)
        {
            var cha = DataBag.Deserialize<Versus.ChallengeDto>(databag);
            if (cha == null)
            {
                Versus.ShowHints(databag);
                return;
            }

            UpdatePage(HostName, OppGender, OppMilitaryPower, PlayerRank, ChallengeRank, Versus.WarIdentity.Challenger,
                RkCheckPoints);
            UpdateCpProgress(Versus.WarIdentity.Challenger, cha);
            Vs.warListController.GetWarList();
        }
    }

    private void OnReportAction(int index)
    {
#if UNITY_EDITOR
        Versus.RkGetCheckPointWarResult(WarId, index, OnCallBackResult);
#endif

        void OnCallBackResult(string data)
        {
            var bag = DataBag.DeserializeBag(data);
            if (bag == null)
            {
                Versus.ShowHints(data);
                return;
            }

            var isChallengerWin = bag.Get<bool>(0);
            var rounds = bag.Get<List<ChessRound>>(1);
            var ops = bag.Get<List<Versus.WarResult.Operator>>(2).Cast<IOperatorInfo>().ToList();
            Vs.ReadyWarboard(isChallengerWin, rounds, ops);
        }
    }


    private void OnAttackCheckpointAction(int checkpointId)
    {
#if UNITY_EDITOR
        Versus.RkGetCheckpointFormation(WarId, checkpointId, CallBackAction);
#endif

        void CallBackAction(string data)
        {
            var bag = DataBag.DeserializeBag(data);
            if (bag == null)
            {
                Versus.ShowHints(data);
                return;
            }
            var formation = bag.Get<Dictionary<int, Versus.Card>>(0);
            var cp = RkCheckPoints.Single(c => c.Index == checkpointId);
            OnAttackCity?.Invoke((WarId, WarIsd, cp.Index , cp.MaxCards),
                formation.ToDictionary(f => f.Key, f => (IGameCard)f.Value));
        }
    }

    public void Display(bool isShow)
    {
        gameObject.SetActive(isShow);
    }

    [Serializable]private class CancelWindowUi
    {
        public GameObject Window;
        public Button CancelButton;

        public void SetCancel(UnityAction onRequestCancel)
        {
            Window.gameObject.SetActive(true);
            CancelButton.onClick.RemoveAllListeners();
            CancelButton.onClick.AddListener(onRequestCancel);
        }
    }

}