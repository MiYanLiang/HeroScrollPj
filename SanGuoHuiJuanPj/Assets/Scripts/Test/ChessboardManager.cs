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
    
    private static SpriteStyle SpriteStyle { get; } = new SpriteStyle();
    private List<SpriteObj> Sprites { get; } = new List<SpriteObj>();

    //private Stopwatch Sw = Stopwatch.StartNew();
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

    //演示回合
    private IEnumerator AnimateRound(ChessRound round,ChessboardOperator chess)
    {
        //Sw.Start();
        IsBusy = true;
        foreach (var stat in round.PreRoundStats)
        {
            var card = GetCardMap(stat.Key);
            if (card.HitPoint != stat.Value.Hp)
            {
                Debug.LogWarning($"卡牌[{card.Info.Name}]({stat.Key})与记录的有误！客户端[{card.Status}] vs 数据[{stat}]");
            }
        }
        yield return OnPreRoundUpdate(round);
        //回合开始演示
        yield return OnRoundBeginAnimation(round.PreAction);
        //执行回合每一个棋子活动
        for (int i = 0; i < round.Processes.Count; i++)
        {
            var process = round.Processes[i];

            yield return ChessmanAnimation(process);
            //更新棋位和棋子状态(提取死亡棋子)
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
        //回合结束演示
        yield return OnInstantEffects(round.FinalAction.ChessProcesses
            .SelectMany(p => p.CombatMaps.Values.SelectMany(m => m.Activities)).ToList());

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
        NewWar.ChessOperator.RoundConfirm();
        //Sw.Stop();
    }

    private IEnumerator OnPreRoundUpdate(ChessRound round)
    {
        var tween = DOTween.Sequence().Pause();
        foreach (var stat in round.PreRoundStats)
        {
            if (!CardMap.ContainsKey(stat.Key)) continue;
            var card = CardMap[stat.Key];
            tween.Append(card.ChessmanStyle.UpdateStatusTween(stat.Value, card));
        }
        yield return tween.Play().WaitForCompletion();
    }

    private IEnumerator OnRoundBeginAnimation(RoundAction action)
    {
        foreach (var process in action.ChessProcesses)
        {
            if (process.Type == ChessProcess.Types.JiBan)
            {
                var jb = DataTable.JiBan[process.Major];
                yield return DOTween.Sequence()
                    .Append(OnJiBanEffect(process.Scope == 0, jb))
                    .Append(OnJiBanOffenseAnim(process.Scope != 0, jb))
                    .Join(OnInstantEffects(process.CombatMaps.Values.SelectMany(a => a.Activities).ToList()))
                    .WaitForCompletion();
                yield return new WaitForSeconds(1);
                continue;
            }

            yield return OnInstantEffects(process.CombatMaps.Values.SelectMany(a => a.Activities).ToList()).WaitForCompletion();
        }
    }

    private Tween OnJiBanEffect(bool isChallenger,JiBanTable jb)
    {
        var jbCards = Chessboard.Data.Values
            .Where(c=>c.isPlayerCard == isChallenger)
            .Join(jb.Cards, c => (c.cardId, c.cardType), j => (j.CardId, j.CardType), (c, _) => c).ToArray();
        //Debug.Log($"Jb[{jb.Id}].{nameof(OnJiBanEffect)} [{Sw.Elapsed}]");
        return DOTween.Sequence()
            .AppendCallback(() => {
                foreach (var card in jbCards) card.cardObj.SetHighLight(true);
            }).AppendInterval(0.5f)
            .AppendCallback(() =>
            {
                foreach (var card in jbCards) card.cardObj.SetHighLight(false);
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
        //Debug.Log($"Jb[{jb.Id}].{nameof(OnJiBanOffenseAnim)} [{Sw.Elapsed}]");
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
            .OnComplete(() => DisplayJiBanObj(isPlayer, offensiveEffect.transform))
            .AppendInterval(0.5f)
            .OnComplete(
                () =>
                {
                    offensiveEffect.gameObject.SetActive(false);
                    offensiveEffect.transform.SetParent(JiBanEffect.transform);
                });
    }

    private Tween OnInstantEffects(IList<Activity> activities)
    {
        //Debug.Log($"{nameof(OnInstantEffects)} acts[{activities.Count}][{Sw.Elapsed}]");
        var tween = DOTween.Sequence();
        foreach (var activity in activities)
        {
            if (activity.Intent == Activity.Sprite)
            {
                tween.Join(OnSpriteEffect(activity));
                continue;
            }
            var target = GetCardMap(activity.To);
            target.UpdateActivityStatus(activity.Result.Status);
            tween.Join(SpriteStyle.Activity(activity, target));
        }

        return tween;
    }

    private IEnumerator ChessmanAnimation(ChessProcess process)
    {
        if (process.Type == ChessProcess.Types.JiBan)
            throw new InvalidOperationException("棋子行动不允许调用羁绊。");
        if (process.Type == ChessProcess.Types.Chessboard)
        {
            yield return OnInstantEffects(process.CombatMaps.Values.SelectMany(m => m.Activities.Concat(m.CounterActs)).ToArray()).WaitForCompletion();
            yield break;
        }

        //把棋子提到最上端
        Chessboard.OnActivityBeginTransformSibling(process.Major, process.Scope == 0);

        //var fragments = GenerateAnimFragments(process);
        var majorCard = GetCardMap(Chessboard.GetChessPos(process.Major, process.Scope == 0).Card.InstanceId);

        foreach (var map in process.CombatMaps)
        {
            var mainTween = DOTween.Sequence().Pause();
            var counterTween = DOTween.Sequence().Pause();

            //会心一击演示
            if (map.Value.Activities.SelectMany(c => c.Conducts).Any(c => c.Rouse > 0))
                yield return FullScreenRouse().WaitForCompletion();

            var offensiveActivity = map.Value.Activities.FirstOrDefault(a => a.Intent == Activity.Offensive);
            if (offensiveActivity != null && majorCard.Style.Type == CombatStyle.Types.Melee)
            {
                var target = GetCardMap(offensiveActivity.To);
                yield return CardAnimator.PreActionTween(majorCard, target).WaitForCompletion();
            }
            else yield return CardAnimator.PreActionTween(majorCard, null).WaitForCompletion();

            //施放者活动
            FightCardData major = null;
            foreach (var activity in map.Value.Activities)
            {
                var target = GetCardMap(activity.To);
                var op = GetCardMap(activity.From);
                if (major == null)
                    major = op;
                
                if (activity.Intent == Activity.Sprite) //精灵活动最高优先，否则target会译为棋子而非棋位
                {
                    mainTween.Join(OnSpriteEffect(activity));
                    continue;
                }

                if (op == null) //没有施放者多数是buff活动
                {
                    mainTween.Join(target.ChessmanStyle.RespondTween(activity, target, -1));
                    continue;
                }

                //主要活动
                mainTween.Join(op.ChessmanStyle.OffensiveTween(activity, op))
                    .Join(target.ChessmanStyle.RespondTween(activity, target,
                        op.ChessmanStyle.GetMilitarySparkId(activity)));

                //击退一格
                if (activity.IsRePos)
                    mainTween.Join(CardAnimator.OnRePos(target, Chessboard.GetScope(target.IsPlayer)[activity.RePos])
                        .OnComplete(() =>
                        {
                            PlaySoundEffect(activity, op.ChessmanStyle, target);
                            Chessboard.PlaceCard(activity.RePos, target);
                        }));
            }

            if (major!=null && major.Style.Type == CombatStyle.Types.Melee)
                yield return CardAnimator.StepBackAndHit(major).WaitForCompletion();

            if (map.Value.Activities.SelectMany(c => c.Conducts).Any(c => c.Rouse > 0 || c.Critical > 0))
                mainTween.Join(CardAnimator.ChessboardConduct(Chessboard));

            yield return mainTween.Play().WaitForCompletion();//播放主要活动

            /*********************反击*************************/
            //如果没有反击
            if (map.Value.CounterActs == null || map.Value.CounterActs.Count == 0) continue;

            //会心一击演示
            if (map.Value.Activities.SelectMany(c => c.Conducts).Any(c => c.Rouse > 0))
                yield return FullScreenRouse().WaitForCompletion();

            var counterUnit = GetCardMap(map.Value.CounterActs.First().From);
            yield return CardAnimator.CounterAnimation(counterUnit).WaitForCompletion();

            //反击活动
            foreach (var activity in map.Value.CounterActs)
            {
                var target = GetCardMap(activity.To);
                var op = GetCardMap(activity.From);
                counterTween.Join(op.ChessmanStyle.OffensiveTween(activity, op))
                    .Join(target.ChessmanStyle.RespondTween(activity, target,
                        op.ChessmanStyle.GetMilitarySparkId(activity)));
            }

            if (map.Value.CounterActs.SelectMany(c => c.Conducts).Any(c => c.Rouse > 0 || c.Critical > 0))
                counterTween.Join(CardAnimator.ChessboardConduct(Chessboard));


            yield return counterTween.Play().WaitForCompletion();//播放反击
        }

        var chessPos = Chessboard.GetChessPos(majorCard).transform.position;
        yield return new WaitForSeconds(0.5f);
        yield return CardAnimator.FinalizeAnimation(majorCard, chessPos).WaitForCompletion();
    }

    private Tween OnSpriteEffect(Activity activity)
    {
        var chessPos = Chessboard.GetChessPos(activity.To, activity.IsChallenger == 0);
        return DOTween.Sequence().AppendCallback(() =>
        {
            foreach (var conduct in activity.Conducts.Where(c=>c.Kind == CombatConduct.SpriteKind))
            {
                var buffId = Effect.GetFloorBuffId(conduct.Element);
                if (buffId == -1) continue;
                var sp = Sprites.FirstOrDefault(s => s.SpriteType == conduct.Element && s.Pos == chessPos.Pos && s.SpriteId == conduct.Kind);
                if (conduct.Total >= 0)
                {
                    if (sp == null)
                    {
                        sp = new SpriteObj
                        {
                            SpriteType = conduct.Element,
                            SpriteId = conduct.Kind,
                            Pos = chessPos.Pos
                        };
                        Sprites.Add(sp);
                    }

                    if (sp.Obj == null)
                        sp.Obj = EffectsPoolingControl.instance.GetFloorBuff(buffId, chessPos.transform);
                    continue;
                }

                if (sp == null) continue;
                if (sp.Obj != null)
                {
                    var obj = sp.Obj;
                    EffectsPoolingControl.instance.RecycleEffect(obj);
                    sp.Obj = null;
                }
                Sprites.Remove(sp);
            }
        });
        //var targetPos = Chessboard.GetChessPos(activity.To, activity.IsChallenger == 0);
        //targetPos.transform
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
            case ActivityResult.Types.ChessPos:
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

    private class SpriteObj
    {
        public int SpriteId;
        public int SpriteType;
        public int Pos;
        public EffectStateUi Obj;
    }
}