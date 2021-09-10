using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ChessboardManager : MonoBehaviour
{
    public bool SkipAnim;
    public WarGameCardUi PrefabUi;
    public WarGameCardUi HomePrefab;
    public NewWarManager NewWar;
    public Chessboard Chessboard;
    public GameObject RouseEffectObj;
    public JiBanEffectUi JiBanEffect;
    public GameObject[] JiBanOffensiveEffects;
    
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private GameObject fireUIObj;

    [SerializeField] private GameObject boomUIObj;

    [SerializeField] private GameObject gongKeUIObj;

    protected FightCardData GetCardMap(int id) => CardMap.ContainsKey(id) ? CardMap[id] : null;

    protected Dictionary<int, FightCardData> CardMap { get; set; }

    protected bool IsBusy { get; set; }
    protected Dictionary<int,EffectStateUi>Sprites { get; set; }
    private ChessOperatorManager<FightCardData> chessOp;
    private static SpriteStyle ChessboardStyle { get; } = new SpriteStyle();

    private Stopwatch Sw = Stopwatch.StartNew();
    public void Init()
    {
        NewWar.StartButton.onClick.AddListener(InvokeCard);
    }
    protected void InitCardUi()
    {
        Chessboard.Init();//确定阵容生,成operators
        NewWar.Init();//初始化计算器

        foreach (var card in NewWar.CardData.Values)
        {
            var gc = GameCard.Instance(card.cardId, card.cardType, card.Level);
            var ui = Instantiate(card.CardType == GameCardType.Base ? HomePrefab : PrefabUi);
            card.cardObj = ui;
            Chessboard.PlaceCard(card.Pos, card);
            CardMap.Add(card.InstanceId, card);
            if (card.CardType != GameCardType.Base) ui.Init(gc);
        }
    }

    protected void InvokeCard()
    {
        if (IsBusy) return;
        var chess = NewWar.ChessOperator;
        NewWar.StartButtonShow(false);
        var round = chess.StartRound();
        if (SkipAnim)
        {
            NewWar.StartButtonShow(true);
            return;
        }
        StartCoroutine(AnimateRound(round, chess));
    }

    private IEnumerator ChallengerWinFinalize()
    {
        var opponentTransform = Chessboard.GetChessPos(17, false).transform;
        boomUIObj.transform.position = opponentTransform.position;
        fireUIObj.transform.position =
            gongKeUIObj.transform.position = Chessboard.GetChessPos(7, false).transform.position;
        yield return new WaitForSeconds(0.5f);
        //PlayAudioForSecondClip(91, 0);
        boomUIObj.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        boomUIObj.SetActive(false);
        //欢呼声
        //PlayAudioForSecondClip(90, 0);

        //火焰
        fireUIObj.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        gongKeUIObj.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        fireUIObj.SetActive(false);
        gongKeUIObj.SetActive(false);
        yield return new WaitForSeconds(0.1f);

        Time.timeScale = 1;
    }

    private IEnumerator AnimateRound(ChessRound round,ChessboardOperator chess)
    {
        Sw.Start();
        IsBusy = true;
        yield return OnRoundBegin(round.PreAction.Activities);
        for (int i = 0; i < round.Processes.Length; i++)
        {
            var process = round.Processes[i];
            if (process.Pos < 0)//当执行不属于棋位 -1 = challenger, -2 = opposite
            {
                yield return OnChessboardProcess(process.Activities);
            }
            else
            {
                Chessboard.OnActivityBeginTransformSibling(process.Pos, process.Scope == 0);
                //列表配置应该是主活动在附列表最上端，然后附活动跟着在列表后面
                //yield return ProceedActivitySet(process.Activities);
                yield return ProceedChessmanActivities(process.Activities);
            }

            foreach (var card in CardMap.ToDictionary(c => c.Key, c => c.Value))
            {
                Chessboard.ResetPos(card.Value);
                if (card.Value.CardType == GameCardType.Base) continue;
                if (!card.Value.Status.IsDeath) continue;
                var ui = CardMap[card.Key];
                CardMap.Remove(card.Key);
                Destroy(ui.cardObj.gameObject);
            }
        }
        yield return OnChessboardProcess(round.FinalAction.Activities);

        if (chess.IsGameOver)
        {
            //ClearStates();
            if (chess.IsChallengerWin)
            {
                yield return ChallengerWinFinalize();
            }

            if (WarsUIManager.instance != null)
                WarsUIManager.instance.ExpeditionFinalize(false);
            yield return null;
        }

        IsBusy = false;
        NewWar.StartButtonShow(true);
        Sw.Stop();
    }

    private IEnumerator OnRoundBegin(List<Activity> activities)
    {
        var jiBanId = -1;
        var jbActivities = new List<Activity>();
        for (var i = 0; i < activities.Count; i++)
        {
            var activity = activities[i];
            if (activity.Intent == RoundAction.JiBan)
            {
                //是否跟上一个是同一个羁绊id
                if (jiBanId != activity.Skill && jiBanId != -1)
                {
                    yield return JiBanAnimTween(jbActivities)
                        .WaitForCompletion();
                    yield return OnChessboardProcess(jbActivities)
                        .WaitForCompletion();
                    jbActivities.Clear();
                    yield return new WaitForSeconds(0.5f);
                }
            }
            jbActivities.Add(activity);
            jiBanId = activity.Skill;
        }

        if (jbActivities.Count == 0) yield break;
        yield return JiBanAnimTween(jbActivities)
            .WaitForCompletion();
        yield return OnChessboardProcess(jbActivities)
            .WaitForCompletion();
        yield return new WaitForSeconds(0.5f);

        //羁绊画面演示
        Tween JiBanAnimTween(List<Activity> list)
        {
            var ac = list.First();
            var jb = DataTable.JiBan[ac.Skill];
            return DOTween.Sequence()
                .Append(OnJiBanEffect(ac.IsChallenger == 0, jb))
                .Append(OnJiBanOffenseAnim(ac.IsChallenger != 0, jb));
        }
    }

    private Tween OnJiBanEffect(bool isChallenger,JiBanTable jb)
    {
        Debug.Log($"Jb[{jb.Id}].{nameof(OnJiBanEffect)} [{Sw.Elapsed}]");
        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                JiBanEffect.Image.sprite = GameResources.Instance.JiBanBg[jb.Id];
                JiBanEffect.TitleImg.sprite = GameResources.Instance.JiBanHText[jb.Id];
                DisplayJiBanObj(isChallenger, JiBanEffect.transform);
            })
            .AppendInterval(1f)
            .OnComplete(() => JiBanEffect.gameObject.SetActive(false));
    }
    void DisplayJiBanObj(bool isPlayer,Transform obj)
    {
        var targetTransform =  isPlayer? JiBanEffect.Player : JiBanEffect.Opposite;
        obj.SetParent(targetTransform);
        obj.gameObject.SetActive(true);
        obj.localPosition = Vector3.zero;
    }

    private Tween OnJiBanOffenseAnim(bool isPlayer, JiBanTable jb)
    {
        Debug.Log($"Jb[{jb.Id}].{nameof(OnJiBanOffenseAnim)} [{Sw.Elapsed}]");
        GameObject offensiveEffect = null;
        switch ((JiBanSkillName)jb.Id)
        {
            case JiBanSkillName.WuHuShangJiang:
                offensiveEffect = JiBanOffensiveEffects[0];
                break;
            case JiBanSkillName.WuZiLiangJiang:
                offensiveEffect = JiBanOffensiveEffects[3];
                break;
            case JiBanSkillName.WeiWuMouShi:
                offensiveEffect = JiBanOffensiveEffects[2];
                break;
            case JiBanSkillName.ShuiShiDuDu:
                offensiveEffect = JiBanOffensiveEffects[1];
                break;
            case JiBanSkillName.HeBeiSiTingZhu:
                offensiveEffect = JiBanOffensiveEffects[4];
                break;
            case JiBanSkillName.TaoYuanJieYi:
            case JiBanSkillName.WoLongFengChu:
            case JiBanSkillName.HuChiELai:
            case JiBanSkillName.HuJuJiangDong:
            case JiBanSkillName.TianZuoZhiHe:
            case JiBanSkillName.JueShiWuShuang:
            case JiBanSkillName.HanMoSanXian:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (offensiveEffect == null) return DOTween.Sequence();
        return DOTween.Sequence()
            .AppendCallback(() => DisplayJiBanObj(isPlayer, offensiveEffect.transform))
            .AppendInterval(0.5f)
            .OnComplete(
                () =>
                {
                    offensiveEffect.gameObject.SetActive(false);
                    offensiveEffect.transform.SetParent(JiBanEffect.transform);
                });
    }

    private Tween OnChessboardProcess(List<Activity> activities)
    {
        Debug.Log($"{nameof(OnChessboardProcess)} acts[{activities.Count}][{Sw.Elapsed}]");
        var tween = DOTween.Sequence();
        foreach (var activity in activities)
        {
            var target = GetCardMap(activity.To);
            target.UpdateActivityStatus(activity.Result.Status);
            tween.Join(ChessboardStyle.Activity(activity, target));
        }
        return tween;
    }

    private IEnumerator ProceedChessmanActivities(List<Activity> actSet)
    {
        FightCardData offense = null;
        FightCardData target = null;
        Sequence chessboardTween = null;
        Sequence effectTween = DOTween.Sequence().Pause(); //注意DoTween必须在一个线程添加，如果没暂停，一旦yield所有tween将直接执行。

        var preActionDone = false;
        foreach (var activity in actSet)
        {
            if (activity.Intent == Activity.Sprite)//精灵活动
            {
                yield return effectTween.Play().WaitForCompletion();
                yield return OnSpriteEffect(activity).Play().WaitForCompletion();
                continue;
            }
            var innerTween = DOTween.Sequence().Pause(); //内嵌活动
            Sequence chessboardShake = null;
            FightCardData op = null;

            target = GetCardMap(activity.To);
            op = offense = GetCardMap(activity.From);
            if (op == null)
            {
                effectTween.Join(target.ChessmanStyle.Respond(activity, target));
                continue;
            }

            var tg = target;
            if (!preActionDone)
            {
                if (offense != null)
                {
                    yield return offense.ChessmanStyle.PreActionTween(offense, target).WaitForCompletion();

                    //攻击前摇
                    if (offense.ChessmanStyle.Type == CombatStyle.Types.Melee)
                        yield return CardAnimator.StepBackAndHit(offense).WaitForCompletion();
                }

                preActionDone = true;
            }

            //主要活动
            effectTween.Join(op.ChessmanStyle.ActivityEffectTween(activity, op, tg, Chessboard.transform));

            //内嵌活动
            if (activity.Inner != null && activity.Inner.Count > 0)
            {
                RecursiveInnerCount = 0;
                foreach (var inner in activity.Inner)
                {
                    //如果内嵌行动为反击
                    if (inner.Intent == Activity.Counter)
                    {
                        //直接演示上一个演示动作
                        yield return effectTween.Play().WaitForCompletion();
                        yield return innerTween.Play().WaitForCompletion();
                        //并且执行一个反击动作
                        yield return tg.ChessmanStyle.CounterTween(inner, tg, op)
                            .OnComplete(() => PlaySoundEffect(inner, tg.ChessmanStyle, op))
                            .WaitForCompletion();
                        //重新组织新活动演示链
                        innerTween = DOTween.Sequence().Pause();
                        effectTween = DOTween.Sequence().Pause();
                    }
                    else innerTween.Join(ProcessInnerActivities(inner));
                }

                effectTween.Join(innerTween);
            }

            //棋盘活动
            if (activity.Conducts.Any(c => c.Rouse > 0))
            {
                chessboardTween = DOTween.Sequence().Pause().Append(FullScreenRouse());
                effectTween.Join(CardAnimator.ChessboardConduct(activity, Chessboard));
            }

            //击退一格
            if (activity.IsRePos)
                effectTween.Join(CardAnimator.OnRePos(tg, Chessboard.GetScope(tg.IsPlayer)[activity.RePos])
                    .OnComplete(() =>
                    {
                        PlaySoundEffect(activity, op.ChessmanStyle, tg);
                        Chessboard.PlaceCard(activity.RePos, tg);
                    }));
        }

        //演示：
        if (chessboardTween != null)
            yield return chessboardTween.Play().WaitForCompletion();

        yield return effectTween.Play().WaitForCompletion();

        if (offense != null)
        {
            var chessPos = Chessboard.GetChessPos(offense).transform.position;
            yield return offense.ChessmanStyle.FinalizeActionTween(offense, chessPos).WaitForCompletion();
        }
    }

    private Dictionary<ChessPos, List<SpriteObj>> SpritePosMap { get; } = new Dictionary<ChessPos, List<SpriteObj>>();
    private class SpriteObj
    {
        public int SpriteId;
        public GameObject Obj;
        public int Value;
    }
    private Tween OnSpriteEffect(Activity activity)
    {
        var chessPos = Chessboard.GetChessPos(activity.To, activity.IsChallenger == 0);
        if (!SpritePosMap.ContainsKey(chessPos)) SpritePosMap.Add(chessPos, new List<SpriteObj>());
        var pos = SpritePosMap[chessPos];
        return DOTween.Sequence().Pause().AppendCallback(() =>
        {
            foreach (var conduct in activity.Conducts)
            {
                var sp = pos.FirstOrDefault(s => s.SpriteId == conduct.Element);
                if (sp == null)
                {
                    sp = new SpriteObj { SpriteId = conduct.Element };
                    pos.Add(sp);
                }
                sp.Value += (int)conduct.Total;
                if (sp.Value > 0)
                {
                    if (sp.Obj == null)
                        sp.Obj = CardAnimator.AddSpriteEffect(chessPos, conduct);
                    continue;
                }

                if (sp.Obj != null)
                {
                    var obj = sp.Obj;
                    EffectsPoolingControl.instance.RecycleEffect(obj);
                    sp.Obj = null;
                }
            }
        });
        //var targetPos = Chessboard.GetChessPos(activity.To, activity.IsChallenger == 0);
        //targetPos.transform
    }

    private const int RecursiveInnerActivitiesProtection = 9999;
    private int RecursiveInnerCount;

    private Sequence ProcessInnerActivities(Activity activity)
    {
        if (RecursiveInnerCount >= RecursiveInnerActivitiesProtection) throw new StackOverflowException($"{nameof(ProcessInnerActivities)} Count = {RecursiveInnerCount}!");

        var tween = DOTween.Sequence();
        var tg = GetCardMap(activity.To);
        var of = GetCardMap(activity.From);
        tween.Join(of.ChessmanStyle.ActivityEffectTween(activity, of, tg, Chessboard.transform));
        if (activity.Inner != null)
        {
            foreach (var act in activity.Inner) 
                tween.Join(ProcessInnerActivities(act));
        }
        RecursiveInnerCount++;
        return tween;
    }

    private void PlaySoundEffect(Activity activity, CombatStyle offense, FightCardData target)
    {
        var offenseSoundEffectId = GetOffenseAudioId(activity, offense);
        if (offenseSoundEffectId >= 0) PlayAudio(offenseSoundEffectId, 0);
        var defSoundEffectId = GetDefendAudioId(activity, offense, target);
        if (defSoundEffectId >= 0) PlayAudio(defSoundEffectId, 0.2f);
    }

    private int GetDefendAudioId(Activity activity,CombatStyle offense, FightCardData target)
    {
        var audioId = -1;
        var isMeleeHero = offense.Type == CombatStyle.Types.Melee &&
                          offense.ArmedType >= 0;
        var isMachine = offense.ArmedType == 7;//器械系
        if (target.Style.ArmedType == -3)//陷阱
        {
            switch (target.Style.Military)
            {
                case 0://拒马
                    if (isMeleeHero && !isMachine)
                        audioId = 89;
                    break;
                case 1://地雷
                    if (isMeleeHero)
                        audioId = 88;
                    break;
                case 2://石墙
                case 3://八阵图
                case 4://金锁阵
                case 5://鬼兵阵
                case 6://火墙
                case 7://毒泉
                case 8://刀墙
                case 9://滚石
                case 10://滚木
                    break;
                case 11://金币宝箱
                case 12://宝箱
                    if (target.Hp.Value> 0)break;
                    audioId = 98;
                    break;
            }
            return audioId;
        }
        if (target.Style.ArmedType <= 0) return audioId;
        switch (activity.Result.Type)
        {
            case ActivityResult.Types.Suffer:
            case ActivityResult.Types.Friendly:
            case ActivityResult.Types.EaseShield:
            case ActivityResult.Types.Kill:
                break;
            case ActivityResult.Types.Dodge:
                audioId = 97;
                break;
            case ActivityResult.Types.Shield:
            case ActivityResult.Types.Invincible:
                audioId = 96;
                break;
            case ActivityResult.Types.Undefined:
            default:
                throw new ArgumentOutOfRangeException();
        }

        return audioId;
    }

    private int GetOffenseAudioId(Activity activity,CombatStyle offense)
    {
        var audioId = -1;
        if (offense.ArmedType == -3) return audioId; //陷阱不会进攻
        if (offense.ArmedType == -2) //塔
        {
            switch (offense.Military)
            {
                case 0: //营寨
                case 2: //奏乐台
                    return 42;
                case 1: //投石台
                    return 24;
                case 3: //箭楼
                    return 20;
                case 6: //轩辕台
                    return 4;
            }
            return audioId;
        }

        if (offense.ArmedType < 0) return audioId;
        if (activity.Skill == 0) return 0;
        switch (offense.Military)
        {
            case 3://飞甲
                audioId = 3;
                break;
            case 4://大盾
                audioId =4;
                break;
            case 6://虎卫
                audioId =6;
                break;
            case 8://象兵
                audioId = 8;
                break;
            case 9://先锋
            case 60:
                audioId = 9;
                break;
            case 10://死士
                audioId = 10;
                break;
            case 11:
                audioId = 11;
                break;
            case 12:
                audioId = 12;
                break;
            case 14:
            case 59:
                audioId = 14;
                break;
            case 15:
                audioId = 15;
                break;
            case 16:
                audioId =16;
                break;
            case 17:
                audioId = 17;
                break;
            case 18:
                audioId = 18;
                break;
            case 19:
            case 51:
                audioId = 19;
                break;
            case 20:
            case 52:
                audioId = 20;
                break;
            case 21:
                audioId = 21;
                break;
            case 22:
                audioId = 22;
                break;
            case 23:
                audioId = 23;
                break;
            case 24:
                audioId = 24;
                break;
            case 25:
                audioId = 25;
                break;
            case 26:
                audioId = 26;
                break;
            case 27:
                audioId = 27;
                break;
            case 30:
                audioId = 30;
                break;
            case 31:
                audioId = 31;
                break;
            case 32:
                break;
            case 33:
                break;
            case 34:
                audioId = 34;
                break;
            case 35:
                audioId = 35;
                break;
            case 36:
                audioId = 36;
                break;
            case 37:
                audioId = 37;
                break;
            case 38:
                audioId = 38;
                break;
            case 39:
                audioId = 39;
                break;
            case 40:
                audioId = -1;
                break;
            case 42:
                audioId = 42;
                break;
            case 43:
                audioId = 43;
                break;
            case 44:
                audioId = 44;
                break;
            case 45:
                audioId = 45;
                break;
            case 46:
                audioId = 46;
                break;
            case 47:
                audioId = 47;
                break;
            case 48:
                audioId = 48;
                break;
            case 49:
                audioId = 49;
                break;
            case 50:
                audioId = 50;
                break;
            case 53:
                audioId = 53;
                break;
            case 54:
                audioId = 54;
                break;
            case 55:
                audioId = activity.Skill == 2 ? 84 : 55;
                break;
            case 56:
                audioId = 56;
                break;
            case 57:
                audioId = 57;
                break;
            case 58:
                audioId = 58;
                break;
            case 65:
                audioId = 65;
                break;
            case 28:
            case 29:
            default:
                audioId = 0;
                break;
        }

        return audioId;
    }

    private void OnSpriteActivity(Activity activity)
    {
        foreach (var conduct in activity.Conducts)
        {
            if (conduct.Total <= -1)//移除精灵
            {
                var sprite = Sprites[conduct.Kind];
                Sprites.Remove(conduct.Kind);
                Destroy(sprite.gameObject);
                return;
            }
            //生成精灵
            var pos = Chessboard.GetChessPos(activity.To, activity.IsChallenger == 0);
            var sp = EffectsPoolingControl.instance.GetPosState(conduct, pos);
            if (sp == null) return;
            Sprites.Add(conduct.Kind, sp);
        }

    }

    private Tween FullScreenRouse() =>
        DOTween.Sequence().AppendCallback(() =>
        {
            if (RouseEffectObj.activeSelf)
                RouseEffectObj.SetActive(false);
            RouseEffectObj.SetActive(true);
        }).AppendInterval(1.5f);

    public void PlayAudio(int clipIndex, float delayedTime)
    {
        if (!GamePref.PrefMusicPlay) return;
        if (WarsUIManager.instance == null || AudioController0.instance == null) return;
        var clip = WarsUIManager.instance.audioClipsFightEffect[clipIndex];
        var volume = WarsUIManager.instance.audioVolumeFightEffect[clipIndex];
        if (AudioController0.instance.ChangeAudioClip(clip, volume))
        {
            AudioController0.instance.PlayAudioSource(0);
            return;
        }

        AudioController0.instance.audioSource.volume *= 0.75f;
        audioSource.clip = WarsUIManager.instance.audioClipsFightEffect[clipIndex];
        audioSource.volume = WarsUIManager.instance.audioVolumeFightEffect[clipIndex];
        if (!GamePref.PrefMusicPlay)
            return;
        audioSource.PlayDelayed(delayedTime);
    }
}