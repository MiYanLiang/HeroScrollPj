using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor.XR;
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

    private Chessboard.WinningEffect WinEffect => Chessboard.winningEffect;

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
    private Tween ShadyTween(ChessboardFragment frag,int spriteId)
    {
        var darkLevel = frag.Skill == 1 ? Effect.ChessboardEvent.Shady : Effect.ChessboardEvent.Dark;
        var audioId = Effect.GetChessboardAudioId(darkLevel, PosSprite.GetKind(spriteId));
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
        RouseEffectObj.gameObject.gameObject.SetActive(false);
        Chessboard.winningEffect.Reset();
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
        yield return WinEffect.SetWin(opponentTransform, Chessboard.GetChessPos(7, false).transform);
        RouseEffectObj.gameObject.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    private Tween ChessboardPreAnimation()
    {
        var gridTween = DOTween.Sequence().Pause();
        foreach (var image in Chessboard.GridImages)
            gridTween.Join(image.DOFade(CardAnimator.instance.Misc.ChessGridFading,
                CardAnimator.instance.Misc.ChessGridFadingSec));
        return gridTween;
    }

    private IEnumerator OnUpdateSummary(ActivityRecord rec)
    {
        var tween = DOTween.Sequence().Pause();
        var section = new AudioSection();
        foreach (var fg in rec.Data)
            tween.Join(OnInstantFragmentUpdate(fg, section));
        yield return tween.Play().AppendCallback(() => PlayAudio(section)).WaitForCompletion();
    }

    private Tween OnInstantFragmentUpdate(ActivityFragment fg,AudioSection section)
    {
        var tween = DOTween.Sequence();
        switch (fg.Type)
        {
            case ActivityFragment.FragmentTypes.Chessman:
                tween.Join(UpdateChessmanFragment((CardFragment)fg, section));
                break;
            case ActivityFragment.FragmentTypes.Chessboard:
                tween.Join(UpdateChessboardFragment((ChessboardFragment)fg, section));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return tween;

        Tween UpdateChessboardFragment(ChessboardFragment cf,AudioSection aud)
        {
            var tw = DOTween.Sequence();
            SetAudioSection(aud, cf);
            var dmgColor = cf.Kind == ChessboardFragment.Kinds.Sprite
                ? ChessmanStyle.DamageColor.Red
                : ChessmanStyle.DamageColor.Gold;
            foreach (var respond in cf.Responds)
            {
                var op = TryGetCardMap(respond.ExeId); //获取执行者
                if (op != null) 
                    tw.Join(op.ChessmanStyle.ExecutionEffect(op, respond.Skill)); //加入棋子执行特效
                tw.Join(TargetRespond(respond, Damage.Types.General, dmgColor, -1, true)); //加入目标反馈
                SetAudioSection(section, respond); //设定音效
            }

            if (cf.Kind is ChessboardFragment.Kinds.Gold or ChessboardFragment.Kinds.Chest)
                tw.PrependCallback(() => OnResourceUpdate.Invoke(cf.IsChallenger, cf.TargetId, cf.Value));

            var pos = Chessboard.GetChessPos(cf.Pos, cf.IsChallenger);
            tw.PrependCallback(() => SpriteUpdate(pos, cf.TargetId, cf.Value == 1));
            return tw;
        }

        Tween UpdateChessmanFragment(CardFragment cfg,AudioSection aud)
        {
            var tween = DOTween.Sequence();
            
            foreach (var act in cfg.Executes)
            {
                foreach (var respond in act.Responds)
                {
                    var o = TryGetCardMap(respond.ExeId); //获取执行者
                    if (o != null)
                    {
                        tween.Join(o.ChessmanStyle.ExecutionEffect(o, respond.Skill)); //加入棋子执行特效
                    }

                    if (respond.Pop > 0)
                    {
                        var tg = TryGetCardMap(respond.TargetId);
                        var exe = TryGetCardMap(respond.ExeId);
                        if (tg == null) continue;
                        tween.Join(tg.ChessmanStyle.RespondAnim(tg, respond));
                        tween.PrependCallback(() => tg.ChessmanStyle.UpdateStatus(respond.Status, tg));
                        if (exe == null) continue;
                        tween.PrependCallback(() =>
                        {
                            var sparkId = exe.ChessmanStyle.GetMilitarySparkId(respond.Skill);
                            tg.ChessmanStyle.RespondUpdate(tg, respond.Pop, GetDamageColor(respond.Kind), act.DamageType, sparkId, respond.Skill);
                        });

                    }//加入目标反馈
                    SetAudioSection(aud, respond); //设定音效
                }
                tween.Join(RePosRespond(act));
            }

            var op = TryGetCardMap(cfg.InstanceId);
            if (op != null) tween.Join(CardAnimator.instance.AssistEnlargeAnimation(op));
            return tween;
        }
    }

    private ChessmanStyle.DamageColor GetDamageColor(RespondAct.Responds respondKind)
    {
        switch (respondKind)
        {
            case RespondAct.Responds.None:
            case RespondAct.Responds.Buffing:
            case RespondAct.Responds.Dodge:
            case RespondAct.Responds.Kill:
            case RespondAct.Responds.Suicide:
            case RespondAct.Responds.Suffer:
                return ChessmanStyle.DamageColor.Red;
            case RespondAct.Responds.Heal:
                return ChessmanStyle.DamageColor.Green;
            case RespondAct.Responds.Shield:
                return ChessmanStyle.DamageColor.Gold;
            case RespondAct.Responds.Ease:
                return ChessmanStyle.DamageColor.Gray;
            default:
                throw new ArgumentOutOfRangeException(nameof(respondKind), respondKind, null);
        }
    }

    private IEnumerator OnPlayJiBanBuff(ActivityRecord rec)
    {
        var jb = DataTable.JiBan[rec.InstanceId];
        PlayedList.Add((rec.IsChallenger, jb.Id));
        var section = new AudioSection();
        PlayAudio(Effect.GetChessboardAudioId(Effect.ChessboardEvent.Rouse));
        yield return JiBanManager.JiBanDisplay(jb.Id, rec.IsChallenger);
        var tween = DOTween.Sequence().Pause();
        foreach (var frag in rec.Data)
            tween.Join(OnInstantFragmentUpdate(frag, section));
        yield return tween.Play().AppendCallback(() => PlayAudio(section)).WaitForCompletion();
    }

    private List<(bool isChallenger, int jiBanId)> PlayedList = new List<(bool isChallenger, int jiBanId)>();
    private IEnumerator OnPlayJiBanAttack(ActivityRecord rec)
    {
        var jb = DataTable.JiBan[rec.InstanceId];
        var section = new AudioSection();
        if(!PlayedList.Any(p=>p.isChallenger==rec.IsChallenger &&
                             p.jiBanId == jb.Id))
        {
            PlayAudio(Effect.GetChessboardAudioId(Effect.ChessboardEvent.Rouse));
            yield return JiBanManager.JiBanDisplay(jb.Id, rec.IsChallenger);
        }
        PlayAudio(Effect.GetJiBanAudioId((JiBanSkillName)jb.Id));
        yield return JiBanManager.JiBanOffensive(jb.Id, rec.IsChallenger);
        var tween = DOTween.Sequence().Pause();
        foreach (var frag in rec.Data)
            tween.Join(OnInstantFragmentUpdate(frag, section));
        yield return tween.Play().AppendCallback(() => PlayAudio(section)).WaitForCompletion();
    }

    private void UpdateChessmenStatus(ChessRoundRecord rec)
    {
        foreach (var gc in CardMap.ToList())
        {
            if (!rec.StatusMap.TryGetValue(gc.Key, out var stat))
            {
                RemoveCard(gc.Key);
                continue;
            }
            var card = CardMap[gc.Key];
            card.ChessmanStyle.UpdateStatus(stat, card);
        }
    }
    //演示回合
    public IEnumerator AnimateRound(ChessRoundRecord round, bool playRoundStartAudio)
    {
        PlayedList.Clear();
        var preTween = DOTween.Sequence().Pause();
        yield return preTween.PrependCallback(() =>
            {
                OnRoundStart?.Invoke(round.Round);
                if (playRoundStartAudio) PlayAudio(Effect.GetChessboardAudioId(Effect.ChessboardEvent.RoundStart));
            }).Join(ChessboardPreAnimation())
            .Join(DelayStart(CardAnimator.instance.Misc.OnRoundStart)).Play()
            .WaitForCompletion();

        //yield return new WaitForSeconds(CardAnimator.instance.Misc.OnRoundStart);

        //var cards = CardMap.Values.Where(c => !c.Status.IsDeath).ToList();
        //var jbs = JiBanManager.GetAvailableJiBan(cards);
        foreach (var rec in round.ActivityRecords)
        {
            switch (rec.Type)
            {
                case ActivityRecord.Types.Summary:
                    yield return OnUpdateSummary(rec);
                    break;
                case ActivityRecord.Types.Chessman:
                    yield return OnChessmanAnimation(rec);
                    break;
                case ActivityRecord.Types.JiBanBuff:
                    yield return OnPlayJiBanBuff(rec);
                    break;
                case ActivityRecord.Types.JiBanAttack:
                    yield return OnPlayJiBanAttack(rec);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        yield return new WaitForSeconds(CardAnimator.instance.Misc.OnRoundEnd);
        UpdateChessmenStatus(round);
        var gridTween = DOTween.Sequence().Pause();
        foreach (var image in Chessboard.GridImages)
            gridTween.Join(image.DOFade(1, CardAnimator.instance.Misc.ChessGridFadingSec));
        yield return gridTween.Play().WaitForCompletion();
    }

    private Tween DelayStart(float delay) => DOTween.Sequence().AppendInterval(delay);

    //作为执行(攻击/释放技能)演示，传入已组合好的执行结果，根据不同卡牌演示结果
    private IEnumerator ExecuteTween(FightCardData op, Tween exTween)
    {
        if (op != null) //不是陷阱类都会演示行动效果
        {
            switch (op.Style.Type)
            {
                case CombatStyle.Types.None:
                    break;
                case CombatStyle.Types.Melee:
                    yield return CardAnimator.instance.StepBackAndHit(op).WaitForCompletion();
                    yield return exTween.Play().WaitForCompletion();
                    break;
                case CombatStyle.Types.Range:
                    var origin = op.cardObj.transform.position;
                    yield return CardAnimator.instance.RecoilTween(op).WaitForCompletion();
                    yield return exTween.Play().WaitForCompletion();
                    yield return CardAnimator.instance.MoveTween(op, origin).WaitForCompletion();
                    break;
                default:
                    yield break;
            }
        }
        else yield return exTween.Play().WaitForCompletion();
    }

    //每一个棋子的执行演示
    private IEnumerator OnChessmanAnimation(ActivityRecord rec)
    {
        FightCardData major = null;
        var maxActId = rec.Data.Max(f => f.ActId);
        var chessboardFrags = rec.Data
            .Where(f => f.Type == ActivityFragment.FragmentTypes.Chessboard)
            .ToList();
        var chessmanFrags = rec.Data
            .Where(f => f.Type == ActivityFragment.FragmentTypes.Chessman)
            .Cast<CardFragment>().ToList();

        major = TryGetCardMap(rec.InstanceId); //获取执行棋子
        for (var i = 0; i <= maxActId; i++)
        {
            var exTween = DOTween.Sequence().Pause();
            var preTween = DOTween.Sequence().Pause();
            var section = new AudioSection();

            var actId = i;
            //棋盘演示段(一般都是远程发射精灵)
            var boardFrags = new List<ChessboardFragment>();
            chessboardFrags.Cast<ChessboardFragment>().Where(c => c.ActId == actId).ToList().ForEach(c =>
            {
                chessboardFrags.Remove(c);
                boardFrags.Add(c);
            });
            //加入执行演示
            boardFrags.ForEach(fg => exTween.Join(OnInstantFragmentUpdate(fg, section)));

            //卡牌演示段
            var chessFrags = new List<CardFragment>();
            chessmanFrags.Where(c => c.ActId == actId).ToList().ForEach(c =>
            {
                chessmanFrags.Remove(c);
                chessFrags.Add(c);
            });

            var isRouse = false;
            var dmgType = Damage.Types.General;
            var rePosTween = DOTween.Sequence().Pause();
            FightCardData lastEx = null;
            //收集棋子执行的反馈
            foreach (var ex in chessFrags.SelectMany(frag => frag.Executes))
            {
                var op = OnSetExecuteResponds(ex, exTween, section);
                if (op != null) lastEx = op;
                rePosTween.Join(RePosRespond(ex));
                //如果会心，调用棋盘摇晃效果
                if (dmgType == Damage.Types.General)
                    dmgType = ex.DamageType;
                if (dmgType == Damage.Types.Rouse)
                    isRouse = true;
            }

            if (isRouse)
                preTween.Append(FullScreenRouse());

            if (dmgType != Damage.Types.General) //加入棋盘震动特效
                exTween.Join(CardAnimator.instance.ChessboardConduct(Chessboard));

            //把棋子提到最上端
            Chessboard.OnActivityBeginTransformSibling(major);

            if (!isShady)
            {
                var shadyFrag = boardFrags.FirstOrDefault(f =>
                    f.Kind == ChessboardFragment.Kinds.Sprite &&
                    Effect.IsShadyChessboardElement(PosSprite.GetKind(f.TargetId)));
                if (shadyFrag != null)
                {
                    isShady = true;
                    yield return ShadyTween(shadyFrag, shadyFrag.TargetId).WaitForCompletion();
                }
            }

            switch (major.ChessmanStyle.Type)
            {
                case CombatStyle.Types.Range:
                    preTween.Append(CardAnimator.instance.PreActionTween(major, null));
                    break;
                case CombatStyle.Types.Melee:
                    var tar = chessFrags.SelectMany(f => f.Executes
                            .SelectMany(e => e.Responds.Where(r => r.ExeId == major.InstanceId)))
                        .FirstOrDefault(e => e.TargetId != rec.InstanceId);
                    if (tar != null)
                    {
                        var target = TryGetCardMap(tar.TargetId);
                        preTween.Append(CardAnimator.instance.PreActionTween(major, target));
                    }
                    break;
                case CombatStyle.Types.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            yield return preTween.Play().WaitForCompletion();

            lastEx ??= major;
            yield return ExecuteTween(lastEx, exTween.Join(rePosTween).PrependCallback(() => PlayAudio(section)));

            var counter = chessFrags.Select(c => c.Counter).FirstOrDefault(c => c != null);
            if (counter == null) continue;
            //如果有反击
            var counterAudio = new AudioSection();
            var counterTween = DOTween.Sequence().Pause();
            switch (counter.DamageType)
            {
                case Damage.Types.General:
                    break;
                case Damage.Types.Critical:
                    counterTween.Join(CardAnimator.instance.ChessboardConduct(Chessboard));
                    break;
                case Damage.Types.Rouse:
                    counterTween.Join(CardAnimator.instance.ChessboardConduct(Chessboard));
                    yield return FullScreenRouse().WaitForCompletion();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var counterRes = counter.Responds.FirstOrDefault()?.ExeId ?? -1;
            var counterEx = TryGetCardMap(counterRes);
            if (counterEx == null)
                throw new NullReferenceException($"找不到反击单位id={counterRes}");

            OnSetExecuteResponds(counter, counterTween, counterAudio);
            counterTween.PrependCallback(() => PlayAudio(counterAudio));
            yield return CardAnimator.instance.CounterAnimation(counterEx).WaitForCompletion();
            yield return counterTween.Play().WaitForCompletion();
        }

        if (isShady)
        {
            yield return ShadyTween(0, -1).WaitForCompletion();
            isShady = false;
        }

        if (major != null && !major.Status.IsDeath)
            //后摇
        {
            yield return CardAnimator.instance.FinalizeAnimation(major, Chessboard.GetChessPos(major).transform.position)
                .WaitForCompletion();
            Chessboard.PlaceCard(major.PosIndex, major);
        }

    }

    private FightCardData OnSetExecuteResponds(ExecuteAct ex, Sequence exTween, AudioSection section)
    {
        var tween = DOTween.Sequence();
        FightCardData exer = null;
        foreach (var respond in ex.Responds)
        {
            var op = TryGetCardMap(respond.ExeId); //获取执行者
            if (op != null)
            {
                tween.Join(op.ChessmanStyle.ExecutionEffect(op, respond.Skill)); //加入棋子执行特效
                exer = op;
            }

            if (respond.Pop > 0)
                tween.Join(TargetRespond(respond, ex.DamageType, GetDamageColor(respond.Kind), respond.Skill, false)); //加入目标反馈
            SetAudioSection(section, respond); //设定音效
        }

        exTween.Append(tween);
        return exer;
    }

    private Tween TargetRespond(RespondAct respond, Damage.Types dmgType, ChessmanStyle.DamageColor color, int skill,bool isSummary)
    {
        var tween = DOTween.Sequence();
        var tg = TryGetCardMap(respond.TargetId);
        var op = TryGetCardMap(respond.ExeId);
        if (tg == null) return tween;
        tween.Join(tg.ChessmanStyle.RespondAnim(tg, respond));
        tween.PrependCallback(() => tg.ChessmanStyle.UpdateStatus(respond.Status, tg));
        if (isSummary || op == null) return tween;
        tween.PrependCallback(() =>
        {
            var sparkId = op.ChessmanStyle.GetMilitarySparkId(respond.Skill);
            tg.ChessmanStyle.RespondUpdate(tg, respond.Pop, color, dmgType, sparkId, skill);
        });
        return tween;
    }

    private Tween RePosRespond(ExecuteAct ex)
    {
        var tween = DOTween.Sequence();
        foreach (var respond in ex.Responds)
        {
            var tg = TryGetCardMap(respond.TargetId);
            if (respond.FinalPos != tg.Pos)
                tween.Join(OnRePosTween(tg, respond.FinalPos));
        }
        return tween;
    }

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
        yield return OnFilterDeathChessman();
        //执行回合每一个棋子活动
        for (int i = 0; i < round.Processes.Count; i++)
        {
            var process = round.Processes[i];
            if (process.CombatMaps.Any(c => c.Value.Activities.Any() || c.Value.CounterActs.Any()))
                yield return ChessmanAnimation(process);
            yield return OnFilterDeathChessman();
        }

        //回合结束演示
        if (round.FinalAction.ChessProcesses.Count > 0)
        {
            foreach (var process in round.FinalAction.ChessProcesses)
                yield return OnBasicChessProcess(process).Play().WaitForCompletion();
            yield return OnFilterDeathChessman();
        }

        var gridTween = DOTween.Sequence().Pause();
        foreach (var image in Chessboard.GridImages)
            gridTween.Join(image.DOFade(1, CardAnimator.instance.Misc.ChessGridFadingSec));
        gridTween.Play();

    }

    private IEnumerator OnFilterDeathChessman()
    {
        yield return new WaitForSeconds(CardAnimator.instance.Misc.OnFilterChessmen);
        FilterDeathChessman();
    }

    public void FilterDeathChessman()
    {
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
            var target = TryGetCardMap(activity.To);
            if (target == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{activity}：棋盘找不到棋子[{activity.To}]，请确保活动的合法性。");
#endif
                continue;
            }
            if(!CombatTargets.Contains(activity.To))
            {
                CombatTargets.Add(activity.To);
                UpdateTargetStatus(activity);
            }
            if (activity.Intention == Activity.Intentions.ChessboardBuffing) continue;//附buff活动不演示，直接在CombatMap结果更新状态
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

        var pos = Chessboard.GetChessPos(process.Major, process.Scope == 0);

        //为了预防卡牌换位或是找不到了，尝试搜索
        int opInstance;
        if (pos.Card == null)
        {
            if (pos.Operator == null) //如果op没了直接忽略
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{nameof(ChessmanAnimation)}:忽略 Pos = {pos}, card = {pos.Card}");
#endif
                yield break;
            }

            opInstance = pos.Operator.InstanceId;
        }
        else opInstance = pos.Card.InstanceId;
        var majorCard = TryGetCardMap(opInstance);
        if (majorCard == null) yield break;
        //把棋子提到最上端
        Chessboard.OnActivityBeginTransformSibling(majorCard);

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
            tween.Join(OnRePosTween(target, activity.RePos));
        }
        return tween;
    }

    private Tween OnRePosTween(FightCardData target, int pos)
    {
        return CardAnimator.instance
            .OnRePos(target, Chessboard.GetScope(target.IsPlayer)[pos])
            .OnComplete(() =>
            {
                if (CardMap.Any(c =>
                        c.Value.Pos == pos && c.Value.isPlayerCard == target.isPlayerCard &&
                        c.Value != target))
                    XDebug.LogError<ChessboardVisualizeManager>(
                        $"目标单位棋格[{target.Pos}]移位[{pos}]异常！请检查是否该棋格上有其它单位。");
                Chessboard.PlaceCard(pos, target);
            });
    }

    private void SetAudioSection(AudioSection section, RespondAct respond)
    {
        //棋子活动
        int audioId = -1;
        var op = TryGetCardMap(respond.ExeId);
        if (op == null) return;
        // 棋子伤害
        var style = op.ChessmanStyle;
        switch (style.ArmedType)
        {
            case -2:
                audioId = Effect.GetTowerAudioId(style.Military, respond.Skill);
                break;
            case -3:
                audioId = Effect.GetTrapAudioId(style.Military);
                break;
            case >= 0:
                audioId = Effect.GetHeroAudioId(style.Military, respond.Skill);
                break;
        }

        AddToSection();

        void AddToSection()
        {
            if (audioId >= 0 && !section.Offensive.Contains(audioId))
                section.Offensive.Add(audioId);
        }
    }
    private void SetAudioSection(AudioSection section, ChessboardFragment board)
    {
        //棋子活动
        var audioId = -1;
        switch (board.Kind)
        {
            case ChessboardFragment.Kinds.Sprite when (board.Value > 0)://仅在添加的时候会有声效
                audioId = Effect.GetSpriteAudioId(PosSprite.GetKind(board.TargetId));
                    AddToSection();
                break;
            case ChessboardFragment.Kinds.Gold:
            case ChessboardFragment.Kinds.Chest:
                    audioId = Effect.GetPlayerResourceEffectId(board.TargetId);
                    AddToSection();
                break;
            default:break;
        }
        //if (conduct.Kind == CombatConduct.ElementDamageKind)
        //{
        //    audioId = Effect.GetBuffDamageAudioId((CardState.Cons)conduct.ReferenceId);
        //    AddToSection();
        //}
        void AddToSection()
        {
            if (audioId >= 0 && !section.Offensive.Contains(audioId))
                section.Offensive.Add(audioId);
        }
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

public record CardRespondAnim
{
    public bool Shake { get; set; }
    public int RePos { get; set; } = -1;
    public int Pop { get; set; }

}