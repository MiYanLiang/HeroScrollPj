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
    public enum SpWarIdentity
    {
        Anonymous = 0,//一般人
        Challenger = 1,//挑战者
        Host = 2,//关主
        PrevHost = 3,//上个关主
        PrevChallenger = 4//过期关卡的挑战者
    }

    [SerializeField] private CheckpointUi CheckpointUiPrefab;
    private static int[] RowSequence = new int[] { 2, 4, 0 };
    [SerializeField] private ScrollRect MapScrollRect;
    [SerializeField] private Button BackButton;
    [SerializeField] private Button ChallengeButton;
    [SerializeField] private UnityEvent onStartChallengeEvent;

    private int WarId { get; set; } = -1;
    private string Host { get; set; }
    private List<CheckpointUi> CpList { get; } = new List<CheckpointUi>();
    private List<SpCheckpoint> SpCheckPoints { get; set; }
    private ObjectPool<CheckpointUi> Pool { get; set; }
    //(warId , pointId)
    private UnityAction<int, int, int, Dictionary<int, IGameCard>> OnAttackCity { get; set; }

    public void Init(UnityAction backAction,UnityAction<int, int , int , Dictionary<int, IGameCard>> onAttackCityAction)
    {
        OnAttackCity = onAttackCityAction;
        gameObject.SetActive(false);
        Pool = new ObjectPool<CheckpointUi>(() => Instantiate(CheckpointUiPrefab, MapScrollRect.content));
        CheckpointUiPrefab.gameObject.SetActive(false);
        BackButton.onClick.AddListener(backAction);
    }
    
    public void Set(int warId)
    {
        WarId = warId;
        gameObject.SetActive(true);
        Http.Get($"{Versus.Api}/{Versus.WarStageInfoApi}?id={warId}&charId={Versus.TestCharId}", OnApiAction, Versus.WarStageInfoApi);


        void OnApiAction(string obj)
        {
            var bag = DataBag.DeserializeBag(obj);
            if (bag == null) throw new NotImplementedException();
            var id = bag.Get<int>(0);
            var hostName = bag.Get<string>(1);
            var warIdentity = (SpWarIdentity)bag.Get<int>(2);
            var data = bag.Get<List<List<string>>>(3); //3 warIdentity
            SpCheckPoints = new List<SpCheckpoint>();
            for (int i = 0; i < data.Count; i++)
            {
                var cp = data[i];

                var cpPointId = int.Parse(cp[0]); //0 pointId
                var cpIndex = int.Parse(cp[1]); //1 index
                var cpTitle = cp[2]; //2 title
                var cpEventType = int.Parse(cp[3]); //cpEvent
                var cpMaxCards = int.Parse(cp[4]); //4 maxCard
                var cpMaxRounds = int.Parse(cp[5]); //5 maxRound
                var cpFormationCount = int.Parse(cp[6]); //6 formationCount
                SpCheckPoints.Add(new SpCheckpoint(cpPointId, cpIndex, cpTitle, cpEventType, cpMaxCards, cpMaxRounds,
                    cpFormationCount));
            }

            UpdatePage(hostName, warIdentity, SpCheckPoints);
        }
    }

    private void UpdatePage(string hostName, SpWarIdentity warIdentity,IList<SpCheckpoint> cps)
    {
        Host = hostName;
        if (CpList.Any())
        {
            CpList.ForEach(c => Pool.Recycle(c));
            CpList.Clear();
        }

        if (warIdentity != SpWarIdentity.Anonymous) RequestChallenge();
        switch (warIdentity)
        {
            case SpWarIdentity.Anonymous:
            case SpWarIdentity.PrevHost:
                ChallengeBtnActive(true);
                break;
            case SpWarIdentity.Challenger:
            case SpWarIdentity.PrevChallenger:
            case SpWarIdentity.Host:
                ChallengeBtnActive(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(warIdentity), warIdentity, null);
        }
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
                    ui.Set(cp, () => OnSelectedUi(cp.PointId));
                CpList.Add(ui);
            }
        }
    }

    public void OnSelectedUi(int pointId) => CpList.ForEach(c => c.SetSelected(c.PointId == pointId));

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
        Http.Post($"{Versus.Api}/{Versus.StartChallengeV1}?charId={Versus.TestCharId}&warId={WarId}", string.Empty,
            OnChallengeRespond, Versus.StartChallengeV1);

        void OnChallengeRespond(string databag)
        {
            var cha = DataBag.Deserialize<ChallengeDto>(databag);
            if (cha == null) throw new NotImplementedException();
            UpdatePage(Host, SpWarIdentity.Challenger, SpCheckPoints);
            var unPassIds = cha.PointProgress.Where(c => !c.Value).Select(c => c.Key).ToList();
            var currentStage = cha.Stages.Where(c => c.Value.Any(i => unPassIds.Contains(i))).OrderBy(c => c.Key)
                .Select(c => c.Value).FirstOrDefault();
            cha.PointProgress.Join(CpList, p => p.Key, c => c.PointId, (p, c) => (c, p.Value)).ToList().ForEach(o =>
            {
                var (ui, isPass) = o;
                ui.SetProgress(isPass);
                if (isPass) ui.SetReportButton(() => OnReportAction(ui.PointId));
                else ui.SetAttackButton(() => OnAttackCheckpointAction(ui.PointId));
                var isChallengePoint = currentStage != null && currentStage.Contains(ui.PointId);
                ui.AttackButton.gameObject.SetActive(isChallengePoint);
                if (!isChallengePoint) return;
                ui.SetDock(true);
            });
        }
    }

    private void OnReportAction(int id)
    {
        throw new NotImplementedException();
    }

    private void OnAttackCheckpointAction(int checkpointId)
    {
        Http.Get($"{Versus.Api}/{Versus.GetCheckpointFormationV1}?warId={WarId}&charId={Versus.TestCharId}&pointId={checkpointId}", 
            CallBackAction, Versus.GetCheckpointFormationV1);

        void CallBackAction(string data)
        {
            var bag = DataBag.DeserializeBag(data);
            var formation = bag.Get<Dictionary<int, Versus.Card>>(0);
            var cp = SpCheckPoints.Single(c => c.PointId == checkpointId);
            OnAttackCity?.Invoke(WarId, cp.PointId, cp.MaxCards,
                formation.ToDictionary(f => f.Key, f => (IGameCard)f.Value));
        }
    }


    public void Display(bool isShow)
    {
        gameObject.SetActive(isShow);
    }

    public class SpCheckpoint
    {
        public int PointId { get; set; }
        public int Index { get; set; }
        public string Title { get; set; }
        public int EventType { get; set; }
        public int MaxCards { get; set; }
        public int MaxRounds { get; set; }
        public int FormationCount { get; set; }

        public SpCheckpoint(int pointId, int index, string title, int eventType, int maxCards, int maxRounds, int formationCount)
        {
            PointId = pointId;
            Index = index;
            Title = title;
            EventType = eventType;
            MaxCards = maxCards;
            MaxRounds = maxRounds;
            FormationCount = formationCount;
        }
    }

    private class ChallengeDto : DataBag
    {
        public int WarId { get; set; }
        public int CharacterId { get; set; }
        public string HostName { get; set; }
        public long StartTime { get; set; }
        /// <summary>
        /// key = index, value = checkpoints
        /// </summary>
        public Dictionary<int, int[]> Stages { get; set; }
        /// <summary>
        /// key = index, value = units(ex -17)
        /// </summary>
        public Dictionary<int, int> StageUnit { get; set; }
        public Dictionary<int, bool> PointProgress { get; set; }
    }

    private class ChallengeSet
    {
        public Dictionary<int, Versus.Card> Formation { get; set; }
        public int CharacterId { get; set; }
        public int TargetStage { get; set; }

    }
}