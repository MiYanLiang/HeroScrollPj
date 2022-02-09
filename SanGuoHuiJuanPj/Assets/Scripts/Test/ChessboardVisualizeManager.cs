using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class ChessboardVisualizeManager : MonoBehaviour
{
    public WarGameCardUi PrefabUi;
    public WarGameCardUi HomePrefab;
    //public NewWarManager NewWar;
    private Chessboard Chessboard { get; set; }
    private Animator RouseEffectObj => Chessboard.RouseAnim;
    private Image ShadyImage => Chessboard.ShadyImage;
    private JiBanAnimationManager JiBanManager { get; set; }

    [SerializeField] private AudioSource audioSource;

    private Animator fireUIObj => Chessboard.WinFire;

    private Animator boomUIObj => Chessboard.WinExplode;
    private Animator gongKeUIObj => Chessboard.WinText;
    protected FightCardData TryGetCardMap(int id) => CardMap.ContainsKey(id) ? CardMap[id] : null;

    protected Dictionary<int, FightCardData> CardMap { get; set; } = new Dictionary<int, FightCardData>();

    private static InstantEffectStyle InstantEffectStyle { get; } = new InstantEffectStyle();
    private List<SpriteObj> Sprites { get; } = new List<SpriteObj>();
    public UnityEvent<bool, int, int> OnResourceUpdate = new PlayerResourceEvent();
    public UnityEvent<FightCardData> OnCardRemove = new CardDefeatedEvent();
    public event UnityAction<int> OnRoundStart;

    private bool isShady = false;

    private List<WarGameCardUi> GarbageUi = new List<WarGameCardUi>();//UI垃圾收集器，千万别重用UI，如果没分开重写放置棋格上的方法，会造成演示UI引用混乱

    private Tween ShadyTween(Activity activity)
    {
        var conduct = activity.Conducts.FirstOrDefault(c => Effect.IsShadyChessboardElement(PosSprite.GetKind(c)));
        var darkLevel = activity.Skill == 1 ? Effect.ChessboardEvent.Shady : Effect.ChessboardEvent.Dark;
        var audioId = Effect.GetChessboardAudioId(darkLevel, PosSprite.GetKind(conduct));
        return ShadyTween(
            darkLevel == Effect.ChessboardEvent.Shady
                ? CardAnimator.instance.Misc.Shady
                : CardAnimator.instance.Misc.Dark, audioId);
    }

    private Tween ShadyTween(float alpha, int audioId)
    {
        if (alpha > 0 && audioId != -1)
        {
            //var audioId =
            //    Effect.GetChessboardAudioId(alpha >= 0.8
            //        ? Effect.ChessboardEvent.Dark
            //        : Effect.ChessboardEvent.Shady);
            PlayAudio(audioId);
        }
        return ShadyImage.DOFade(alpha, 1f);
    }

    public void Init(Chessboard chessboard,JiBanAnimationManager jiBanAnimationManager)
    {
        Chessboard = chessboard;
        JiBanManager = jiBanAnimationManager;
        Chessboard.Init();
        JiBanManager.Init();
    }

    public IReadOnlyDictionary<int, FightCardData> CardData => _cardData;
    public Dictionary<int, FightCardData> _cardData = new Dictionary<int, FightCardData>();

    public void NewGame()
    {
        foreach (var ui in GarbageUi.ToArray())
        {
            GarbageUi.Remove(ui);
            Destroy(ui.gameObject);
        }
        foreach (var sp in Sprites.ToArray()) RemoveSpriteObj(sp);
        foreach (var card in CardMap.ToDictionary(c => c.Key, c => c.Value))
        {
            CardMap.Remove(card.Key);
            var uiObj = card.Value.cardObj;
            if (uiObj) Destroy(card.Value.cardObj.gameObject);
        }
        _cardData.Clear();
        Chessboard.ResetChessboard();
        ResetWinAnimation();
    }

    public void SetPlayerCard(FightCardData playerBase)
    {
        RegCardData(playerBase);
        InstanceChessman(playerBase);
    }

    private void RegCardData(FightCardData card) => _cardData.Add(card.InstanceId, card);

    /// <summary>
    /// 初始化卡牌UI
    /// </summary>
    /// <param name="card"></param>
    public void InstanceChessman(FightCardData card)
    {
        var gc = GameCard.Instance(card.cardId, card.cardType, card.Level);
        var ui = card.CardType == GameCardType.Base 
            ? Instantiate(HomePrefab) 
            : Instantiate(PrefabUi);

        card.cardObj = ui;
        card.UpdateHpUi();
        Chessboard.PlaceCard(card.Pos, card);
        CardMap.Add(card.InstanceId, card);
        if (card.CardType != GameCardType.Base) ui.Init(gc);
    }

    private void RemoveUi(WarGameCardUi obj)
    {
        obj.gameObject.gameObject.SetActive(false);
        obj.transform.SetParent(Chessboard.transform);
        GarbageUi.Add(obj);
    }

    public IEnumerator ChallengerWinAnimation()
    {
        var opponentTransform = Chessboard.GetChessPos(17, false).transform;
        boomUIObj.transform.position = opponentTransform.position;
        fireUIObj.transform.position =
            gongKeUIObj.transform.position = Chessboard.GetChessPos(7, false).transform.position;
        yield return new WaitForSeconds(0.1f);
        PlayAudio(91);
        boomUIObj.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
        //欢呼声
        PlayAudio(90);

        //火焰
        fireUIObj.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.3f);
        gongKeUIObj.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        boomUIObj.gameObject.SetActive(false);
        fireUIObj.gameObject.SetActive(false);
        gongKeUIObj.gameObject.SetActive(false);
        RouseEffectObj.gameObject.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    private void ResetWinAnimation()
    {
        boomUIObj.gameObject.SetActive(false);
        fireUIObj.gameObject.SetActive(false);
        gongKeUIObj.gameObject.SetActive(false);
        RouseEffectObj.gameObject.gameObject.SetActive(false);
    }

    private Tween ChessboardPreAnimation()
    {
        var gridTween = DOTween.Sequence().Pause();
        foreach (var image in Chessboard.GridImages)
            gridTween.Join(image.DOFade(CardAnimator.instance.Misc.ChessGridFading,
                CardAnimator.instance.Misc.ChessGridFadingSec));
        return gridTween;
    }
    //演示回合

    public IEnumerator AnimateRound(ChessRound round, bool playRoundStartAudio)
    {
        OnRoundStart?.Invoke(round.InstanceId);
        if (playRoundStartAudio) PlayAudio(Effect.GetChessboardAudioId(Effect.ChessboardEvent.RoundStart));
        ChessboardPreAnimation().Play();
        yield return new WaitForSeconds(CardAnimator.instance.Misc.OnRoundStart);

#if UNITY_EDITOR
        foreach (var stat in round.PreRoundStats)
        {
            var card = TryGetCardMap(stat.Key);
            if (card == null) continue;
            if (card.HitPoint != stat.Value.Hp)
                Debug.LogWarning($"卡牌[{card.GetInfo().Name}]({stat.Key})与记录的有误！客户端[{card.Status}] vs 数据[{stat}]");
        }
#endif
        var cards = OnPreRoundUpdate(round);
        var jbs = JiBanManager.GetAvailableJiBan(cards);

        //回合开始演示
        yield return OnRoundBeginAnimation(round.PreAction, jbs);
        yield return FilterDeathChessman();
        //执行回合每一个棋子活动
        for (int i = 0; i < round.Processes.Count; i++)
        {
            var process = round.Processes[i];
            if (process.CombatMaps.Any(c => c.Value.Activities.Any() || c.Value.CounterActs.Any()))
                yield return ChessmanAnimation(process);
            yield return FilterDeathChessman();
        }

        //回合结束演示
        if (round.FinalAction.ChessProcesses.Count > 0)
        {
            foreach (var process in round.FinalAction.ChessProcesses)
                yield return OnBasicChessProcess(process).Play().WaitForCompletion();
            yield return FilterDeathChessman();
        }

        var gridTween = DOTween.Sequence().Pause();
        foreach (var image in Chessboard.GridImages)
            gridTween.Join(image.DOFade(1, CardAnimator.instance.Misc.ChessGridFadingSec));
        gridTween.Play();

    }

    private IEnumerator FilterDeathChessman()
    {
        yield return new WaitForSeconds(CardAnimator.instance.Misc.OnFilterChessmen);
        //更新棋位和棋子状态(提取死亡棋子)
        foreach (var tmp in CardMap.ToDictionary(c => c.Key, c => c.Value))
        {
            Chessboard.ResetPos(tmp.Value);
            if (tmp.Value.CardType == GameCardType.Base) continue;
            if (!tmp.Value.Status.IsDeath) continue;
            RemoveCard(tmp.Key);
        }
    }

    private void RemoveCard(int key, bool trigger = true)
    {
        var card = CardMap[key];
        CardMap.Remove(key);
        RemoveUi(card.cardObj);
        if (trigger) OnCardRemove.Invoke(card);
    }

    private List<FightCardData> OnPreRoundUpdate(ChessRound round)
    {
        var list = new List<FightCardData>();
        foreach (var gc in CardMap.ToList())
        {
            if (!round.PreRoundStats.TryGetValue(gc.Key, out var stat))
            {
                RemoveCard(gc.Key);
                continue;
            }
            var card = CardMap[gc.Key];
            card.ChessmanStyle.UpdateStatus(stat, card);
            list.Add(card);
        }
        return list;
    }

    private IEnumerator OnRoundBeginAnimation(RoundAction action, IEnumerable<(int JiBanId, bool IsChallenger)> jbs)
    {
        var jbProcesses = action.ChessProcesses.Where(p => p.Type == ChessProcess.Types.JiBan).ToArray();
        //无活动羁绊演示
        foreach (var (jiBanId, isChallenger) in jbs)
        {
            //检查羁绊是否有活动
            var process = jbProcesses.FirstOrDefault(p => p.Major == jiBanId && p.IsChallenger == isChallenger);
            if (process != null)continue;//如果有活动等下演示
            PlayAudio(Effect.GetChessboardAudioId(Effect.ChessboardEvent.Rouse));
            yield return JiBanManager.JiBanDisplay(jiBanId, isChallenger);
            if (!JiBanManager.IsOffensiveJiBan(jiBanId)) continue;
            PlayAudio(Effect.GetJiBanAudioId((JiBanSkillName)jiBanId));
            yield return JiBanManager.JiBanOffensive(jiBanId, isChallenger);
        }
        //根据活动演示羁绊
        foreach (var process in action.ChessProcesses)
        {
            Coroutine attackCoroutine = null;
            var rePosActs = new List<Activity>();
            var callBackUpdate = DOTween.Sequence();
            if (process.Type == ChessProcess.Types.JiBan)
            {
                var jb = DataTable.JiBan[process.Major];
                foreach (var map in process.CombatMaps)
                foreach (var activity in map.Value.Activities)
                {
                    callBackUpdate.AppendCallback(() => UpdateTargetStatus(activity));
                    if (activity.IsRePos)
                        rePosActs.Add(activity);
                }
                var isChallenger = process.Scope == 0;
                PlayAudio(Effect.GetChessboardAudioId(Effect.ChessboardEvent.Rouse));
                yield return JiBanManager.JiBanDisplay(jb.Id, isChallenger);
                if (JiBanManager.IsOffensiveJiBan(jb.Id))
                {
                    attackCoroutine = StartCoroutine(JiBanManager.JiBanOffensive(jb.Id, isChallenger));
                    PlayAudio(Effect.GetJiBanAudioId((JiBanSkillName)jb.Id));
                }
            }

            callBackUpdate.Append(RePosTween(rePosActs));
            yield return OnBasicChessProcess(process).Append(callBackUpdate).Play().WaitForCompletion();
            if (attackCoroutine != null)
                //播放
                yield return attackCoroutine;
        }
    }

    private void UpdateTargetStatus(Activity activity)
    {
        if (activity.Intention == Activity.Intentions.Sprite) return;
        var card = TryGetCardMap(activity.To);
        if (card == null ||
            card.Status.IsDeath) return;
        card.ChessmanStyle.UpdateStatus(activity.TargetStatus, card);
    }

    private Sequence OnBasicChessProcess(ChessProcess process)
    {
        var tween = DOTween.Sequence().Pause();
        foreach (var map in process.CombatMaps)
        {
            var updateResult = UpdateBasicActivityUpdate(map.Value.InstanceId,map.Value.Activities);
            var audioSection = updateResult.Item1;
            var tweenCards = updateResult.Item2;
            var ue = new UnityEvent();
            foreach (var card in tweenCards) ue.AddListener(card.MajorAction.Invoke);
            foreach (var result in map.Value.ResultMapper)
            {
                var card = TryGetCardMap(result.Key);
                if (card == null) continue;
                var tw = tweenCards.FirstOrDefault(c => c.Id == card.InstanceId);
                ue.AddListener(() =>
                {
                    card.ChessmanStyle.ResultEffectTween(result.Value, card);
                    if (tw != null)
                        card.ChessmanStyle.NumberEffect(result.Value, card, tw.DmgType);
                    card.ChessmanStyle.UpdateStatus(result.Value.Status, card);
                });
                tween.Join(card.ChessmanStyle.ResultAnimation(result.Value, card));
                SetResultAudio(audioSection, result.Value);
            }

            tween.AppendCallback(() =>
            {
                ue.Invoke();
                PlayAudio(audioSection);
            });
        }
        return tween;
    }

    private void OnInstantUpdate(IList<CombatSet> combats)
    {
        foreach (var map in combats)
        {
            var section = UpdateActivityTarget(map.InstanceId,map.Activities,
                (target, act, _) => InstantEffectStyle.Activity(act, target));
            foreach (var result in map.ResultMapper)
            {
                var target = TryGetCardMap(result.Key);
                target?.UpdateActivityStatus(result.Value.Status);
            }
            PlayAudio(section);
        }
    }

    private List<int> CombatTargets { get; set; } = new List<int>();
    private int CombatSetId { get; set; }
    private AudioSection UpdateActivityTarget(int combatSetId,IEnumerable<Activity> activities, UnityAction<FightCardData, Activity, AudioSection> action)
    {
        var audioSection = new AudioSection();
        if(combatSetId == CombatSetId) CombatTargets.Clear();
        foreach (var activity in activities)
        {
            SetAudioSection(audioSection, activity);
            if (IsPlayerResourcesActivity(activity)) continue;
            if (IsSpriteActivity(activity)) continue;
            if(!CombatTargets.Contains(activity.To))
            {
                CombatTargets.Add(activity.To);
                UpdateTargetStatus(activity);
            }
            if (activity.Intention == Activity.Intentions.ChessboardBuffing) continue;//附buff活动不演示，直接在CombatMap结果更新状态
            var target = TryGetCardMap(activity.To);
            if (target == null)
                Debug.LogError($"{activity}：棋盘找不到棋子[{activity.To}]，请确保活动的合法性。");
            action(target, activity, audioSection);
        }
        return audioSection;
    }

    private (AudioSection, List<TweenCard>) UpdateBasicActivityUpdate(int combatSetId, IEnumerable<Activity> activities)
    {
        var list = new List<TweenCard>();
        var section = UpdateActivityTarget(combatSetId, activities, (target, activity, aud) =>
        {
            var tc = new TweenCard(activity.To);
            tc.DmgType = Damage.GetType(activity);
            if (activity.From >= 0)
            {
                var op = TryGetCardMap(activity.From);
                if (op == null) return;
                tc.MajorAction.AddListener(() =>
                {
                    target.ChessmanStyle.RespondStatusEffect(activity, target,
                        op.ChessmanStyle.GetMilitarySparkId(activity));
                    op.ChessmanStyle.ActivityEffect(activity, op);
                });
            }
            else tc.MajorAction.AddListener(() => OnChessboardActivity(activity, target));

            list.Add(tc);
            SetAudioSection(aud, activity);
        });
        return (section, list);
    }

    private void OnChessboardActivity(Activity activity, FightCardData target)
    {
        var first = activity.Conducts.FirstOrDefault(c =>
            c.Kind == CombatConduct.ElementDamageKind && Effect.GetBuffDamageSparkId(c.Element) >= 0);
        if (first == null) return;

        target.ChessmanStyle.RespondStatusEffect(activity, target,
            Effect.GetBuffDamageSparkId(first.Element)); //闪花
    }

    private bool IsSpriteActivity(Activity activity)
    {
        if (activity.Intention != Activity.Intentions.Sprite) return false;
        OnSpriteEffect(activity);
        return true;
    }

    private IEnumerator ChessmanAnimation(ChessProcess process)
    {
        if (process.Type == ChessProcess.Types.JiBan)
            throw new InvalidOperationException("棋子行动不允许调用羁绊。");
        if (process.Type == ChessProcess.Types.Chessboard)
        {
            OnInstantUpdate(process.CombatMaps.Values.ToArray());
            yield break;
        }

        //把棋子提到最上端
        Chessboard.OnActivityBeginTransformSibling(process.Major, process.Scope == 0);

        var majorCard = TryGetCardMap(Chessboard.GetChessPos(process.Major, process.Scope == 0).Card.InstanceId);

        foreach (var map in process.CombatMaps.OrderBy(c => c.Key))
        {
            var mainTween = DOTween.Sequence().Pause();
            //会心一击演示
            if (map.Value.Activities.SelectMany(c => c.Conducts).Any(c => c.IsRouseDamage()))
                yield return FullScreenRouse().WaitForCompletion();

            var offensiveActivity =
                map.Value.Activities.FirstOrDefault(a =>
                    a.Intention == Activity.Intentions.Offensive || 
                    a.Intention == Activity.Intentions.Inevitable);
            if (offensiveActivity != null && 
                majorCard != null &&
                offensiveActivity.From == majorCard.InstanceId &&
                majorCard.Style.Type == CombatStyle.Types.Melee)
            {
                var target = TryGetCardMap(offensiveActivity.To);
                yield return CardAnimator.instance.PreActionTween(majorCard, target).WaitForCompletion();
            }
            else yield return CardAnimator.instance.PreActionTween(majorCard, null).WaitForCompletion();

            //施放者活动
            var major = map.Value.Activities.Where(a => a.From >= 0).Select(a => TryGetCardMap(a.From))
                .FirstOrDefault();

            var shady = map.Value.Activities
                .FirstOrDefault(a => a.Intention == Activity.Intentions.Sprite
                                     && a.Conducts.Any(c =>
                                         Effect.IsShadyChessboardElement(PosSprite.GetKind(c)) && c.Total > 0));

            if (shady != null && shady.Skill > 0 && !isShady)
            {
                isShady = true;
                yield return ShadyTween(shady).WaitForCompletion();
            }

            var listTween = new List<TweenCard>();
            var rePosActs = new List<Activity>();
            var mainEvent = new UnityEvent();
            AudioSection section = null;

            yield return new WaitUntil(() =>
            {
                section = UpdateActivityTarget(map.Value.InstanceId, map.Value.Activities, (target, activity, _) =>
                {
                    if(activity.From >= 0)
                    {
                        var op = TryGetCardMap(activity.From);
                        if (op == null) return;
                        //承受方状态演示注入
                        mainEvent.AddListener(() =>
                        {
                            target.ChessmanStyle.RespondStatusEffect(activity, target,
                                op.ChessmanStyle.GetMilitarySparkId(activity));
                            //施展方演示注入
                            op.ChessmanStyle.ActivityEffect(activity, op);
                        });
                        var cardTween = new TweenCard(target.InstanceId);
                        cardTween.DmgType = Damage.GetType(activity);
                        listTween.Add(cardTween);
                    }
                    else mainEvent.AddListener(() => OnChessboardActivity(activity, target));

                    //击退一格注入
                    if (activity.IsRePos)
                        rePosActs.Add(activity);
                });
                return true;
            });

            //***演示开始***
            var rePosTween = RePosTween(rePosActs).Pause();

            //施展方如果近战，前摇(后退，前冲)
            //如果有侵略行为才有动画
            if (major != null &&
                major.Style.ArmedType >= 0)
            {
                switch (major.Style.Type)
                {
                    case CombatStyle.Types.None:
                        break;
                    case CombatStyle.Types.Melee:
                        //为了避免被反伤把卡牌引走
                        if (map.Value.Activities.Any(a => a.From == major.InstanceId &&
                                                          (a.Intention == Activity.Intentions.Offensive || 
                                                           a.Intention == Activity.Intentions.Inevitable)))
                            yield return CardAnimator.instance.StepBackAndHit(major)
                                .WaitForCompletion();
                        break;
                    case CombatStyle.Types.Range:
                        var origin = major.cardObj.transform.position;
                        yield return CardAnimator.instance.RecoilTween(major)
                            .WaitForCompletion();
                        mainTween.Join(CardAnimator.instance.MoveTween(major, origin));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            //如果会心，调用棋盘摇晃效果
            if (map.Value.Activities.SelectMany(c => c.Conducts).Any(c => c.IsCriticalDamage() || c.IsRouseDamage()))
                mainTween.Join(CardAnimator.instance.ChessboardConduct(Chessboard)).WaitForCompletion();

            foreach (var result in map.Value.ResultMapper)
            {
                var card = TryGetCardMap(result.Key);
                if (card == null) continue;//todo 会因为精灵销毁而找不到物件
                
                var tweenCard = listTween.FirstOrDefault();
                mainEvent.AddListener(() =>
                {
                    card.ChessmanStyle.ResultEffectTween(result.Value, card);
                    if (tweenCard != null)
                        card.ChessmanStyle.NumberEffect(result.Value, card, tweenCard.DmgType);
                    card.ChessmanStyle.UpdateStatus(result.Value.Status, card);
                    //var cPos = Chessboard.GetChessPos(card);
                    //if (cPos == null) return;
                    //SpriteUpdateByResult(cPos, result.Value);
                });
                
                mainTween.Join(card.ChessmanStyle.ResultAnimation(result.Value, card));
                SetResultAudio(section, result.Value);
            }

            mainTween.Join(rePosTween);
            mainEvent.AddListener(() => PlayAudio(section));
            mainEvent.Invoke();
            //开始播放前面的注入活动
            yield return mainTween.Play().WaitForCompletion(); //播放主要活动

            /*********************反击*************************/
            //如果没有反击
            if (map.Value.CounterActs == null || map.Value.CounterActs.Count == 0) continue;

            var counterTween = DOTween.Sequence().Pause();

            //会心一击，棋盘效果
            if (map.Value.CounterActs.SelectMany(c => c.Conducts).Any(c => c.IsRouseDamage()))
                yield return FullScreenRouse().WaitForCompletion();

            //反击前摇演示
            var counterUnit = TryGetCardMap(map.Value.CounterActs.First().From);
            yield return CardAnimator.instance.CounterAnimation(counterUnit).WaitForCompletion();

            //会心演示
            if (map.Value.CounterActs.SelectMany(c => c.Conducts).Any(c => c.Rouse > 0 || c.Critical > 0))
                counterTween.Join(CardAnimator.instance.ChessboardConduct(Chessboard));
            var (counterAudio, tweenCards) = UpdateBasicActivityUpdate(map.Value.InstanceId,map.Value.CounterActs);

            var counterE = new UnityEvent();
            foreach (var tweenCard in tweenCards) counterE.AddListener(tweenCard.MajorAction.Invoke);
            foreach (var result in map.Value.CounterResultMapper)
            {
                var card = TryGetCardMap(result.Key);
                if (card == null) continue;
                var tc = tweenCards.FirstOrDefault();
                counterE.AddListener(() =>
                {
                    card.ChessmanStyle.ResultEffectTween(result.Value, card);
                    if (tc != null)
                        card.ChessmanStyle.NumberEffect(result.Value, card, tc.DmgType);
                    card.ChessmanStyle.UpdateStatus(result.Value.Status, card);
                    //var cPos = Chessboard.GetChessPos(card);
                    //if (cPos == null) return;
                    //SpriteUpdateByResult(cPos, result.Value);
                });
                counterTween.Join(card.ChessmanStyle.ResultAnimation(result.Value, card));
                SetResultAudio(counterAudio, result.Value);
            }

            counterE.Invoke();
            PlayAudio(counterAudio);
            //播放反击的注入活动
            yield return counterTween.Play().WaitForCompletion(); //播放反击
        }

        if (isShady)
        {
            yield return ShadyTween(0, -1).WaitForCompletion();
            isShady = false;
        }


        var chessPos = Chessboard.GetChessPos(majorCard);
        yield return new WaitForSeconds(CardAnimator.instance.Misc.OnActivity);
        if (!majorCard.Status.IsDeath)
            //后摇
        {
            yield return CardAnimator.instance.FinalizeAnimation(majorCard, chessPos.transform.position)
                .WaitForCompletion();
            Chessboard.PlaceCard(majorCard.PosIndex, majorCard);
        }
    }

    private Tween RePosTween(List<Activity> list)
    {
        var tween = DOTween.Sequence();
        foreach (var activity in list)
        {
            var target = TryGetCardMap(activity.To);
            if (target == null) continue;
            //todo:注意这里的退一格并没有等待结果。所以理想状态是退一格的时间别设太长
            tween.Join(CardAnimator.instance
                .OnRePos(target, Chessboard.GetScope(target.IsPlayer)[activity.RePos])
                .OnComplete(() =>
                {
                    if (CardMap.Any(c =>
                            c.Value.Pos == activity.RePos && c.Value.isPlayerCard == target.isPlayerCard &&
                            c.Value != target))
                        XDebug.LogError<ChessboardVisualizeManager>(
                            $"目标单位棋格[{target.Pos}]移位[{activity.RePos}]异常！请检查是否该棋格上有其它单位。");
                    Chessboard.PlaceCard(activity.RePos, target);
                }));
        }
        return tween;
    }

    private void SetAudioSection(AudioSection section, Activity activity)
    {
        //棋子活动
        int audioId = -1;
        switch (activity.Intention)
        {
            //精灵buff
            case Activity.Intentions.Sprite when activity.From < 0:
            {
                foreach (var conduct in activity.Conducts)
                {
                    if (!(conduct.Total > 0)) continue;
                    audioId = Effect.GetSpriteAudioId(PosSprite.GetKind(conduct.Element));
                    AddToSection();
                }

                break;
            }
            case Activity.Intentions.PlayerResource:
            {
                foreach (var conduct in activity.Conducts)
                {
                    audioId = Effect.GetPlayerResourceEffectId(conduct.Element);
                    AddToSection();
                }
                break;
            }
            default:
            {
                if (activity.From >= 0)
                {
                    // 棋子伤害
                    var major = TryGetCardMap(activity.From);
                    if (major != null) audioId = GetCardSoundEffect(activity, major.ChessmanStyle);
                    AddToSection();
                }
                break;
            }
        }

        foreach (var conduct in activity.Conducts)
        {
            if (conduct.Kind == CombatConduct.ElementDamageKind)
            {
                audioId = Effect.GetBuffDamageAudioId((CardState.Cons)conduct.ReferenceId);
                AddToSection();
            }
        }

        void AddToSection()
        {
            if (audioId >= 0 && !section.Offensive.Contains(audioId))
                section.Offensive.Add(audioId);
        }
    }

    private static void SetResultAudio(AudioSection section, ActivityResult result)
    {
        var actResultId = Effect.ResultAudioId(result.Type);
        //只获取第一棋子结果音效
        if (!section.Result.Contains(actResultId)) section.Result.Add(actResultId);
    }


    private bool IsPlayerResourcesActivity(Activity activity)
    {
        if (activity.Intention != Activity.Intentions.PlayerResource) return false;
        foreach (var conduct in activity.Conducts)
        {
            if (conduct.Kind == CombatConduct.PlayerScopeKind)
                OnResourceUpdate.Invoke(activity.To == -1, conduct.Element, (int)conduct.Total);
        }
        return true;
    }


    private void OnSpriteEffect(Activity activity)
    {
        ChessPos chessPos = null;
        try
        {
            chessPos = Chessboard.GetChessPos(activity.To, activity.IsChallenger > 0);
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogError($"找不到棋格[{activity.To}]IsChallenger[{activity.IsChallenger > 0}]: {activity}");
#endif
        }

        foreach (var conduct in activity.Conducts.Where(c => c.Kind == CombatConduct.SpriteKind))
            SpriteUpdate(chessPos, conduct.Element, conduct.Total >= 0);
    }

    private void SpriteUpdate(ChessPos chessPos, int spriteType, bool isAdd)
    {
        var buffId = Effect.GetFloorBuffId(PosSprite.GetKind(spriteType));
        if (buffId == -1) return;

        var sp = Sprites.FirstOrDefault(s =>
            s.SpriteType == spriteType && s.Pos == chessPos.Pos);
        if (isAdd)
        {
            if (sp == null)
            {
                sp = new SpriteObj
                {
                    SpriteType = spriteType,
                    Pos = chessPos.Pos
                };
                Sprites.Add(sp);
            }

            //统帅业火特别重新演示爆炸
            if (sp.SpriteType == (int)PosSprite.Kinds.YeHuo)
            {
                if (sp.IsSpriteDisplay)
                    EffectsPoolingControl.instance.RecycleEffect(sp.Obj);
                sp.Obj = EffectsPoolingControl.instance.GetFloorBuff(buffId, chessPos.transform);
            }
            else
            {
                if (!sp.IsSpriteDisplay)
                    sp.Obj = EffectsPoolingControl.instance.GetFloorBuff(buffId, chessPos.transform);
            }

            return;
        }

        if (sp == null) return;
        RemoveSpriteObj(sp);
    }

    private void RemoveSpriteObj(SpriteObj sp)
    {
        if (sp.Obj != null)
        {
            var obj = sp.Obj;
            EffectsPoolingControl.instance.RecycleEffect(obj);
            sp.Obj = null;
        }
        Sprites.Remove(sp);
    }

    private int GetCardSoundEffect(Activity activity, CombatStyle offense)
    {
        var offensiveAudio = -1;
        if (offense.ArmedType == -2) offensiveAudio = Effect.GetTowerAudioId(offense.Military, activity.Skill);
        else if (offense.ArmedType == -3) offensiveAudio = Effect.GetTrapAudioId(offense.Military);
        else if (offense.ArmedType >= 0) offensiveAudio = Effect.GetHeroAudioId(offense.Military, activity.Skill);
        return offensiveAudio;
    }

    private class AudioSection
    {
        public List<int> Offensive = new List<int>();
        public List<int> Result = new List<int>();
    }

    private Tween FullScreenRouse() =>
        DOTween.Sequence().AppendCallback(() =>
        {
            if (RouseEffectObj.gameObject.activeSelf)
                RouseEffectObj.gameObject.SetActive(false);
            RouseEffectObj.gameObject.SetActive(true);
            PlayAudio(Effect.GetChessboardAudioId(Effect.ChessboardEvent.Rouse));
        }).AppendInterval(1.5f);

    private void PlayAudio(AudioSection section)
    {
        foreach (var id in section.Offensive.Concat(section.Result).Where(id => id >= 0)) PlayAudio(id);
    }

    private void PlayAudio(int clipIndex)
    {
        if (clipIndex < 0) return;
        if (WarMusicController.Instance == null || AudioController0.instance == null) return;
        WarMusicController.Instance.PlayWarEffect(clipIndex);
    }

    private class SpriteObj
    {
        public int SpriteType;
        public int Pos;
        public EffectStateUi Obj;
        public bool IsSpriteDisplay => Obj != null;
    }

    private class TweenCard
    {
        public int Id { get; }
        public UnityEvent MajorAction = new UnityEvent();
        public Damage.Types DmgType;

        public TweenCard(int id)
        {
            Id = id;
        }
    }

    public class PlayerResourceEvent : UnityEvent<bool, int, int> { }
    
    public class CardDefeatedEvent : UnityEvent<FightCardData> { }
}