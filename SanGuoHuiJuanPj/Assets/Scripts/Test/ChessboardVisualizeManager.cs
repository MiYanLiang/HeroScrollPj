using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class ChessboardVisualizeManager : MonoBehaviour
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
    [SerializeField] private Toggle autoRoundToggle;

    protected FightCardData GetCardMap(int id) => CardMap.ContainsKey(id) ? CardMap[id] : null;

    protected Dictionary<int, FightCardData> CardMap { get; set; } = new Dictionary<int, FightCardData>();

    public bool IsBusy { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsFirstRound { get; private set; }
    private static InstantEffectStyle InstantEffectStyle { get; } = new InstantEffectStyle();
    private List<SpriteObj> Sprites { get; } = new List<SpriteObj>();
    public event Func<bool> OnRoundBegin;
    public UnityEvent<bool> OnGameSet = new GameSetEvent();
    public UnityEvent<bool, int, int> OnResourceUpdate = new PlayerResourceEvent();
    public UnityEvent<FightCardData> OnCardDefeated = new CardDefeatedEvent();

    //private Stopwatch Sw = Stopwatch.StartNew();
    public void Init()
    {
        Chessboard.Init();
        NewWar.Init();
        NewWar.StartButton.onClick.AddListener(InvokeRound);
    }

    public IDictionary<int, FightCardData> CardData => NewWar.CardData;
    public void NewGame()
    {
        foreach (var card in CardMap.ToDictionary(c => c.Key, c => c.Value))
        {
            CardMap.Remove(card.Key);
            Destroy(card.Value.cardObj.gameObject);
        }

        foreach (var sp in Sprites.ToArray()) RemoveSpriteObj(sp);
        IsFirstRound = true;
        NewWar.NewGame();
        Chessboard.ResetChessboard();
        if(AutoRoundSlider) AutoRoundSlider.value = 0;
    }

    public void SetPlayerBase(FightCardData playerBase)
    {
        NewWar.RegCard(playerBase);
        NewWar.ConfirmPlayer();
    }

    public void SetPlayerChess(FightCardData card) => NewWar.RegCard(card);

    public IList<FightCardData> SetEnemyChess(FightCardData enemyBase, ChessCard[] enemyCards)
    {
        NewWar.Enemy = enemyCards;
        NewWar.RegCard(enemyBase);
        var list = NewWar.ConfirmEnemy();
        list.Add(enemyBase);
        return list;
    }
    /// <summary>
    /// 初始化卡牌UI
    /// </summary>
    /// <param name="card"></param>
    public void InstanceChessmanUi(FightCardData card)
    {
        var gc = GameCard.Instance(card.cardId, card.cardType, card.Level);
        var ui = Instantiate(card.CardType == GameCardType.Base ? HomePrefab : PrefabUi);
        ui.DragDisable();
        card.cardObj = ui;
        Chessboard.PlaceCard(card.Pos, card);
        CardMap.Add(card.InstanceId, card);
        if (card.CardType != GameCardType.Base) ui.Init(gc);
    }

    protected void InvokeRound()
    {
        if (IsBusy) return;
        if (OnRoundBegin != null && !OnRoundBegin.Invoke()) return;
        autoRoundTimer = 0;
        var chess = NewWar.ChessOperator;
        NewWar.StartButtonShow(false);
        var round = chess.StartRound();
        if (SkipAnim)
        {
            NewWar.StartButtonShow(true);
            return;
        }
        StartCoroutine(AnimateRound(round, chess));
        IsFirstRound = false;
    }

    private IEnumerator ChallengerWinAnimation()
    {
        var opponentTransform = Chessboard.GetChessPos(17, false).transform;
        boomUIObj.transform.position = opponentTransform.position;
        fireUIObj.transform.position =
            gongKeUIObj.transform.position = Chessboard.GetChessPos(7, false).transform.position;
        yield return new WaitForSeconds(0.5f);
        PlayAudio(91, 0);
        boomUIObj.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        boomUIObj.SetActive(false);
        //欢呼声
        PlayAudio(90, 0);

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
    private IEnumerator AnimateRound(ChessRound round, ChessboardOperator chess)
    {
        //Sw.Start();
        IsBusy = true;
        foreach (var stat in round.PreRoundStats)
        {
            try
            {
                var card = GetCardMap(stat.Key);
                if (card.HitPoint != stat.Value.Hp)
                {
                    Debug.LogWarning($"卡牌[{card.Info.Name}]({stat.Key})与记录的有误！客户端[{card.Status}] vs 数据[{stat}]");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        yield return OnPreRoundUpdate(round);
        //回合开始演示
        yield return OnRoundBeginAnimation(round.PreAction);
        //执行回合每一个棋子活动
        for (int i = 0; i < round.Processes.Count; i++)
        {
            var process = round.Processes[i];
            if (process.CombatMaps.Any(c => c.Value.Activities.Any() || c.Value.CounterActs.Any()))
                yield return ChessmanAnimation(process);
            //更新棋位和棋子状态(提取死亡棋子)
            foreach (var tmp in CardMap.ToDictionary(c => c.Key, c => c.Value))
            {
                Chessboard.ResetPos(tmp.Value);
                if (tmp.Value.CardType == GameCardType.Base) continue;
                if (!tmp.Value.Status.IsDeath) continue;
                var card = CardMap[tmp.Key];
                CardMap.Remove(tmp.Key);
                OnCardDefeated.Invoke(card);
                card.cardObj.gameObject.SetActive(false);
            }
        }
        //回合结束演示
        yield return OnInstantUpdate(round.FinalAction.ChessProcesses.SelectMany(p => p.CombatMaps.Values).ToArray());

        if (chess.IsGameOver)
        {
            //ClearStates();
            if (chess.IsChallengerWin)
                yield return ChallengerWinAnimation();
            RouseEffectObj.gameObject.SetActive(false);
            OnGameSet.Invoke(chess.IsChallengerWin);
            IsGameOver = true;
            IsBusy = false;
            yield break;
        }

        IsBusy = false;
        NewWar.StartButtonShow(true);
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
            switch (process.Type)
            {
                case ChessProcess.Types.JiBan:
                {
                    var jb = DataTable.JiBan[process.Major];
                    yield return DOTween.Sequence()
                        .Append(OnJiBanEffect(process.Scope == 0, jb))
                        .Append(OnJiBanOffenseAnim(process.Scope != 0, jb))
                        .Join(OnInstantUpdate(process.CombatMaps.Values.ToArray()))
                        .WaitForCompletion();
                    yield return new WaitForSeconds(1);
                    continue;
                }
                case ChessProcess.Types.Chessman:
                case ChessProcess.Types.Chessboard:
                default:
                    yield return OnInstantUpdate(process.CombatMaps.Values.ToArray()).WaitForCompletion();
                    break;
            }
        }
    }

    private Tween OnJiBanEffect(bool isChallenger, JiBanTable jb)
    {
        var jbCards = Chessboard.Data.Values
            .Where(c => c.isPlayerCard == isChallenger)
            .Join(jb.Cards, c => (c.cardId, c.cardType), j => (j.CardId, j.CardType), (c, _) => c).ToArray();
        //Debug.Log($"Jb[{jb.Id}].{nameof(OnJiBanEffect)} [{Sw.Elapsed}]");
        return DOTween.Sequence()
            .AppendCallback(() =>
            {
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
    void DisplayJiBanObj(bool isPlayer, Transform obj)
    {
        var targetTransform = isPlayer ? JiBanEffect.Player : JiBanEffect.Opposite;
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

    private Tween OnInstantUpdate(IList<CombatMapper> combats)
    {
        //Debug.Log($"{nameof(OnInstantEffects)} acts[{activities.Count}][{Sw.Elapsed}]");
        var tween = DOTween.Sequence();
        foreach (var map in combats)
        {
            UpdateActivityTarget(null, map.Activities,
                (target, act) => tween.Join(InstantEffectStyle.Activity(act, target)));
            foreach (var result in map.ResultMapper)
            {
                var target = GetCardMap(result.Key);
                target.UpdateActivityStatus(result.Value.Status);
            }
        }

        return tween;
    }

    private void UpdateActivityTarget(AudioSection audioSection,IEnumerable<Activity> activities, UnityAction<FightCardData, Activity> action)
    {
        foreach (var activity in activities)
        {
            if (audioSection != null) SetAudioSection(audioSection, activity);
            if (IsPlayerResourcesActivity(activity)) continue;
            if (IsSpriteActivity(activity)) continue;

            var target = GetCardMap(activity.To);
            if (target == null)
                throw new InvalidOperationException($"{activity}：棋盘找不到棋子[{activity.To}]，请确保活动的合法性。");
            action(target, activity);
        }
    }

    private bool IsSpriteActivity(Activity activity)
    {
        if (activity.Intent != Activity.Sprite) return false;
        OnSpriteEffect(activity);
        return true;
    }

    private IEnumerator ChessmanAnimation(ChessProcess process)
    {
        if (process.Type == ChessProcess.Types.JiBan)
            throw new InvalidOperationException("棋子行动不允许调用羁绊。");
        if (process.Type == ChessProcess.Types.Chessboard)
        {
            yield return OnInstantUpdate(process.CombatMaps.Values.ToArray()).WaitForCompletion();
            yield break;
        }

        //把棋子提到最上端
        Chessboard.OnActivityBeginTransformSibling(process.Major, process.Scope == 0);

        //var fragments = GenerateAnimFragments(process);
        var majorCard = GetCardMap(Chessboard.GetChessPos(process.Major, process.Scope == 0).Card.InstanceId);

        foreach (var map in process.CombatMaps.OrderBy(c => c.Key))
        {
            var mainTween = DOTween.Sequence().Pause();
            var counterTween = DOTween.Sequence().Pause();
            //会心一击演示
            if (map.Value.Activities.SelectMany(c => c.Conducts).Any(c => c.IsRouseDamage()))
                yield return FullScreenRouse().WaitForCompletion();

            var offensiveActivity = map.Value.Activities.FirstOrDefault(a => a.Intent == Activity.Offensive);
            if (offensiveActivity != null && majorCard.Style.Type == CombatStyle.Types.Melee)
            {
                var target = GetCardMap(offensiveActivity.To);
                yield return CardAnimator.instance.PreActionTween(majorCard, target).WaitForCompletion();
            }
            else yield return CardAnimator.instance.PreActionTween(majorCard, null).WaitForCompletion();

            //施放者活动
            FightCardData major = null;
            var audioSection = new AudioSection();
            var listTween = new List<CardTween>();
            UpdateActivityTarget(audioSection, map.Value.Activities, (target, activity) =>
            {
                var op = GetCardMap(activity.From);
                if (op == null) return;
                if (major == null)
                    major = op;
                mainTween.Join(target.ChessmanStyle.StatusEffectTween(activity, target,
                    op.ChessmanStyle.GetMilitarySparkId(activity)));
                //承受方状态演示注入
                mainTween.AppendCallback(() =>
                    //施展方演示注入
                    major.ChessmanStyle.ActivityEffect(activity, major)
                );
                var cardTween = new CardTween(target.InstanceId);
                cardTween.NumberEffectTween = DOTween.Sequence().Pause()
                    .AppendCallback(() => target.ChessmanStyle.NumberEffect(activity, target));
                listTween.Add(cardTween);
                //击退一格注入
                if (activity.IsRePos)
                    mainTween
                        .Join(CardAnimator.instance.OnRePos(target, Chessboard.GetScope(target.IsPlayer)[activity.RePos])
                        .OnComplete(() => Chessboard.PlaceCard(activity.RePos, target)));
            });

            foreach (var result in map.Value.ResultMapper)
            {
                var card = GetCardMap(result.Key);
                mainTween
                    .Join(card.ChessmanStyle.UpdateStatusTween(result.Value.Status, card)
                        .OnComplete(() => card.ChessmanStyle.ResultEffectTween(result.Value, card)))
                    .Join(card.ChessmanStyle.ResultAnimation(result.Value, card));
                SetResultAudio(audioSection, result.Value);
                var tween = listTween.FirstOrDefault(c => c.Id == card.InstanceId);
                if (tween == null) continue;
                if (!(result.Value.Type == ActivityResult.Types.Dodge ||
                      result.Value.Type == ActivityResult.Types.Shield))
                    mainTween.Join(tween.NumberEffectTween);
            }

            //***演示开始***
            //施展方如果近战，前摇(后退，前冲)
            var section = audioSection;
            var activityTween = DOTween.Sequence().OnComplete(() => PlayAudio(section));
            if (major != null && major.Style.Type == CombatStyle.Types.Melee)
                activityTween.Join(CardAnimator.instance.StepBackAndHit(major));
            yield return activityTween.WaitForCompletion();

            //如果会心，调用棋盘摇晃效果
            if (map.Value.Activities.SelectMany(c => c.Conducts).Any(c => c.IsCriticalDamage() || c.IsRouseDamage()))
                mainTween.Join(CardAnimator.instance.ChessboardConduct(Chessboard));
            //开始播放前面的注入活动
            yield return mainTween.Play().WaitForCompletion();//播放主要活动

            /*********************反击*************************/
            //如果没有反击
            if (map.Value.CounterActs == null || map.Value.CounterActs.Count == 0) continue;

            //会心一击，棋盘效果
            if (map.Value.CounterActs.SelectMany(c => c.Conducts).Any(c => c.IsRouseDamage()))
                yield return FullScreenRouse().WaitForCompletion();

            //反击前摇演示
            var counterUnit = GetCardMap(map.Value.CounterActs.First().From);
            yield return CardAnimator.instance.CounterAnimation(counterUnit).WaitForCompletion();

            audioSection = new AudioSection();
            //反击活动更新

            UpdateActivityTarget(audioSection, map.Value.CounterActs, (target, activity) =>
            {
                var op = GetCardMap(activity.From);
                op.ChessmanStyle.ActivityEffect(activity, op);
                counterTween.Join(target.ChessmanStyle.StatusEffectTween(activity, target,
                    op.ChessmanStyle.GetMilitarySparkId(activity)));
            });

            //会心演示
            if (map.Value.CounterActs.SelectMany(c => c.Conducts).Any(c => c.Rouse > 0 || c.Critical > 0))
                counterTween.Join(CardAnimator.instance.ChessboardConduct(Chessboard));

            //播放反击的注入活动
            var counterAudio = audioSection;
            yield return counterTween.Play().OnComplete(() => PlayAudio(counterAudio)).WaitForCompletion();//播放反击
        }

        var chessPos = Chessboard.GetChessPos(majorCard).transform.position;
        yield return new WaitForSeconds(0.5f);
        //后摇
        yield return CardAnimator.instance.FinalizeAnimation(majorCard, chessPos).WaitForCompletion();

    }

    private void SetAudioSection(AudioSection section, Activity activity)
    {
        //棋子活动
        int audioId = -1;
        if (activity.Intent == Activity.Sprite && activity.From < 0)//精灵buff
        {
            foreach (var conduct in activity.Conducts)
            {
                if (!(conduct.Total > 0)) continue;
                audioId = Effect.GetBuffingAudioId((CardState.Cons)conduct.Element);
                AddToSection();
            }
        }
        if (activity.Intent != Activity.Sprite && activity.From >= 0)
        {
            if (activity.Intent == Activity.Inevitable) //羁绊，Buff 类型的伤害
                audioId = Effect.GetInevitableAudioId((CardState.Cons)activity.Skill);
            else // 棋子伤害
            {
                var major = GetCardMap(activity.From);
                if (major != null) audioId = GetCardSoundEffect(activity, major.ChessmanStyle);
            }
            AddToSection();
        }

        if (activity.Intent == Activity.PlayerResource)
        {
            foreach (var conduct in activity.Conducts)
            {
                audioId = Effect.GetPlayerResourceEffectId(conduct.Element);
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
        if (section.Result == -1) section.Result = actResultId;
    }


    private bool IsPlayerResourcesActivity(Activity activity)
    {
        if (activity.Intent != Activity.PlayerResource) return false;
        foreach (var conduct in activity.Conducts)
        {
            if (conduct.Kind == CombatConduct.PlayerScopeKind)
                OnResourceUpdate.Invoke(activity.To == -1, conduct.Element, (int)conduct.Total);
        }
        return true;
    }


    private void OnSpriteEffect(Activity activity)
    {
        ChessPos chessPos;
        try
        {
            chessPos = Chessboard.GetChessPos(activity.To, activity.IsChallenger == 0);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"找不到棋格[{activity.To}]IsChallenger[{activity.IsChallenger}]: {activity}");
        }
        foreach (var conduct in activity.Conducts.Where(c => c.Kind == CombatConduct.SpriteKind))
        {
            var buffId = Effect.GetFloorBuffId(conduct.Element);
            if (buffId == -1) continue;
            var sp = Sprites.FirstOrDefault(s =>
                s.SpriteType == conduct.Element && s.Pos == chessPos.Pos && s.SpriteId == conduct.Kind);
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
            RemoveSpriteObj(sp);
        }
        //var targetPos = Chessboard.GetChessPos(activity.To, activity.IsChallenger == 0);
        //targetPos.transform
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
        public int Result = -1;
    }

    private Tween FullScreenRouse() =>
        DOTween.Sequence().AppendCallback(() =>
        {
            if (RouseEffectObj.activeSelf)
                RouseEffectObj.SetActive(false);
            RouseEffectObj.SetActive(true);
            PlayAudio(Effect.GetChessboardAudioId(Effect.ChessboardEvent.Rouse), 0f);
        }).AppendInterval(1.5f);

    private void PlayAudio(AudioSection section)
    {
        foreach (var id in section.Offensive.Where(id => id >= 0)) PlayAudio(id, 0);
        if (section.Result >= 0)
            PlayAudio(section.Result, 0f);
    }

    private void PlayAudio(int clipIndex, float delayedTime)
    {
        if (clipIndex < 0) return;
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

    private class CardTween
    {
        public int Id { get; }
        public Sequence NumberEffectTween;

        public CardTween(int id)
        {
            Id = id;
        }
    }

    private float autoRoundTimer;
    [SerializeField] private float AutoRoundSecs = 1.5f;
    [SerializeField] private Slider AutoRoundSlider;
    private void Update()
    {
        if (!IsGameOver && !IsBusy && !IsFirstRound && autoRoundToggle.isOn)
        {
            autoRoundTimer += Time.deltaTime;
            if (autoRoundTimer >= AutoRoundSecs)
            {
                InvokeRound();
            }
            if (AutoRoundSlider)
                AutoRoundSlider.value = 1 - autoRoundTimer / AutoRoundSecs;
        }
    }


    public class PlayerResourceEvent : UnityEvent<bool, int, int> { }
    public class GameSetEvent : UnityEvent<bool> { }
    public class CardDefeatedEvent : UnityEvent<FightCardData> { }
}