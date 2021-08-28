using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;

public class ChessmanTester : MonoBehaviour
{
    public EffectsPoolingControl EffectsPooling;
    public PlayerDataForGame PlayerData;
    public DataTable DataTable;
    public WarGameCardUi PrefabUi;
    public WarGameCardUi HomePrefab;
    public NewWarManager NewWar;
    public Chessboard Chessboard;
    public GameObject RouseEffectObj;
    [SerializeField] 
    AudioSource audioSource;
    [SerializeField]
    GameObject fireUIObj;
    [SerializeField]
    GameObject boomUIObj;
    [SerializeField]
    GameObject gongKeUIObj;

    private bool IsBusy { get; set; }
    private static int CardSeed;
    private GameResources gameResources;
    private Dictionary<int, FightCardData> CardData;
    private Dictionary<int,EffectStateUi>Sprites { get; set; }

    void Start()
    {
        DataTable.Init();
        PlayerData.Init();
        gameResources = new GameResources();
        Sprites = new Dictionary<int, EffectStateUi>();
        CardData = new Dictionary<int, FightCardData>();
        gameResources.Init();
        EffectsPooling.Init();
        Chessboard.Init();
        InitCard(NewWar.Player, true);
        InitCard(NewWar.Enemy, false);
        NewWar.StartButton.onClick.AddListener(InvokeCard);
    }

    private void InitCard(NewWarManager.ChessCard[] cards, bool isPlayer)
    {
        foreach (var card in cards)
        {
            CardSeed++;
            var gc = GameCard.Instance(card.Id, (int) card.Type, card.Level);
            if (card.Type == GameCardType.Base)
            {
                var home = Instantiate(HomePrefab);
                var h = new FightCardData(CardSeed, gc);
                h.isPlayerCard = isPlayer;
                h.cardType = gc.Type;
                h.cardObj = home;
                var hop = NewWar.ChessOperator.RegOperator(h);
                hop.SetPos(card.Pos);
                Chessboard.PlaceCard(card.Pos,h);
                CardData.Add(h.InstanceId, h);
                continue;
            }

            var ui = Instantiate(PrefabUi);
            var fc = new FightCardData(CardSeed, gc);
            fc.isPlayerCard = isPlayer;
            fc.cardObj = ui;
            ui.Init(gc);
            var op = NewWar.ChessOperator.RegOperator(fc);
            op.SetPos(card.Pos);
            Chessboard.PlaceCard(card.Pos, fc);
            CardData.Add(fc.InstanceId, fc);
        }
    }

    private void InvokeCard()
    {
        if(IsBusy)return;
        var chess = NewWar.ChessOperator;
        NewWar.StartButtonShow(false);
        var round = chess.StartRound();
        StartCoroutine(AnimateRound(round,chess));
    }

    //战斗结算
    IEnumerator ChallengerWinFinalize()
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
        //var enemyCards = FightForManager.instance.GetCardList(false);
        //for (int i = 0; i < enemyCards.Count; i++)
        //{
        //    FightCardData cardData = enemyCards[i];
        //    if (i != 17 && cardData != null && cardData.Hp <= 0)
        //    {
        //        totalGold += DataTable.EnemyUnit[cardData.unitId].GoldReward;
        //        var chests = DataTable.EnemyUnit[cardData.unitId].WarChest;
        //        //暂时关闭打死单位获得的战役宝箱
        //        if (chests != null && chests.Length > 0)
        //        {
        //            for (int j = 0; j < chests.Length; j++)
        //            {
        //                this.chests.Add(chests[j]);
        //            }
        //        }
        //    }
        //}
        //totalGold += DataTable.BattleEvent[FightForManager.instance.battleIdIndex].GoldReward;
        //var warChests = DataTable.BattleEvent[FightForManager.instance.battleIdIndex].WarChestTableIds;
        //for (int k = 0; k < warChests.Length; k++)
        //{
        //    chests.Add(warChests[k]);
        //}

        //WarsUIManager.instance.FinalizeWar(totalGold, chests);
        //totalGold = 0;
        //chests.Clear();
    }


    private IEnumerator AnimateRound(ChessRound round,ChessOperatorManager<FightCardData> chess)
    {
        IsBusy = true;
        for (int i = 0; i < round.Processes.Length; i++)
        {
            var process = round.Processes[i];
            if (process.Pos < 0)//当执行不属于棋位 -1 = challenger, -2 = opposite
            {
                throw new NotImplementedException()
            }
            Chessboard.OnActivityBeginTransformSibling(process.Pos, process.Scope == 0);
            //列表配置应该是主活动在附列表最上端，然后附活动跟着在列表后面
            yield return ProceedActivitySet(process.Activities);
            foreach (var card in CardData.ToDictionary(c=>c.Key,c=>c.Value))
            {
                Chessboard.ResetPos(card.Value);
                if (card.Value.CardType == GameCardType.Base) continue;
                if (card.Value.Hp.Value > 0) continue;
                var ui = CardData[card.Key];
                CardData.Remove(card.Key);
                Destroy(ui.cardObj.gameObject);
            }
        }

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
    }

    //执行一套主行动 + 附属行动的程序函数
    private IEnumerator ProceedActivitySet(List<Activity> actSet)
    {
        FightCardData major = null; //进攻点
        var tween = DOTween.Sequence();
        //每一个MajorActivity下面会有多个subActivity
        //整理出每一套行动的主行动内容与附属内容
        var isInitialMove = true;
        foreach (var activity in actSet)
        {
            var target = CardData[activity.To];
            var intensive = activity.GetIntention(); //获取行动意图类型
            if (intensive == Activity.Intention.Sprite)
            {
                OnSpriteActivity(activity);
                continue;
            }
            target.UpdateStatus(activity.Result.Status);
            //ClearCardStates(target);
            tween.Join(CardAnimator.UpdateStateEffect(target));
            if (activity.From < 0)
            {
                tween.Join(SubPartiesProcess(activity, null, target));
                continue;
            }

            var offense = CardData[activity.From];
            offense.UpdateStatus(activity.OffenderStatus);
            tween.Join(CardAnimator.UpdateStateEffect(offense));
            yield return tween.WaitForCompletion();
            tween = DOTween.Sequence();
            //ClearCardStates(offense);
            if (isInitialMove || activity.GetIntention() == Activity.Intention.Major)
            {
                isInitialMove = false;
                yield return tween.WaitForCompletion();
                yield return new WaitForSeconds(0.3f);
                tween = DOTween.Sequence();
                major = offense;
                if (offense.Style.CombatStyle == 0) //如果近战需要预先移动到对面
                    yield return CardAnimator.MeleeMoving(offense, target)?.WaitForCompletion();
                if (activity.Conducts.Any(c => c.Rouse > 0))
                    yield return FullScreenRouse();
                yield return
                    MainActionBegin(offense.combatType, offense, target)?.OnComplete(() => PlaySoundEffect(activity, offense.Style, target)).WaitForCompletion(); //执行主要活动(一般是进攻)
                tween.Join(OnMajorProcess(activity, offense, target));
            }
            else if (intensive == Activity.Intention.Counter)
            {
                yield return tween.WaitForCompletion();
                if (activity.Conducts.Any(c => c.Rouse > 0))
                    yield return FullScreenRouse();
                else yield return new WaitForSeconds(CardAnimator.charge);
                yield return OnCounterAction(activity, offense, target)
                    .OnComplete(() => PlaySoundEffect(activity, offense.Style, target)).WaitForCompletion(); //加入反馈活动
                tween = DOTween.Sequence();
            }
            else
            {
                tween.Join(SubPartiesProcess(activity, offense, target)); //加入附属活动(buff，效果类)
            }

            //击退一格
            if (!activity.IsRePos) continue;
            yield return tween.Join(CardAnimator.OnRePos(target, Chessboard.GetScope(target.IsPlayer)[activity.RePos]))
                .OnComplete(() => PlaySoundEffect(activity, offense.Style, target)).WaitForCompletion();
            tween = DOTween.Sequence();
            Chessboard.PlaceCard(activity.RePos, target);
        }

        yield return tween.WaitForCompletion();
        if (major != null)
            yield return Finalization(major); //结束活动并返回

        Tween MainActionBegin(int combatStyle, FightCardData of,
            FightCardData tg)
        {
            return combatStyle == 0
                ? CardAnimator.MeleeActivity(of, tg)
                : CardAnimator.RangeActivity(of);
        }
    }

    private void PlaySoundEffect(Activity activity, AttackStyle offense, FightCardData target)
    {
        var offenseSoundEffectId = GetOffenseAudioId(activity, offense);
        if (offenseSoundEffectId >= 0) PlayAudio(offenseSoundEffectId, 0);
        var defSoundEffectId = GetDefendAudioId(activity, offense, target);
        if (defSoundEffectId >= 0) PlayAudio(defSoundEffectId, 0.2f);
    }

    private int GetDefendAudioId(Activity activity,AttackStyle offense, FightCardData target)
    {
        var audioId = -1;
        var isMeleeHero = offense.CombatStyle == AttackStyle.CombatStyles.Melee &&
                          offense.ArmedType >= 0;
        var isMachine = offense.ArmedType == 7;//器械系
        if (target.Style.ArmedType == -2)//陷阱
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

    private int GetOffenseAudioId(Activity activity,AttackStyle offense)
    {
        var audioId = -1;
        if (offense.ArmedType == -2) return audioId; //陷阱不会进攻
        if (offense.ArmedType == -1) //塔
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

    //private void ClearCardStates(FightCardData card)
    //{
    //    foreach (var map in card.States)
    //    {
    //        if (card.Status.GetBuff(map.Key) > 0 || map.Value == null) continue;
    //        EffectsPoolingControl.instance.TakeBackStateIcon(map.Value);
    //        card.States[map.Key] = null;
    //    }
    //}

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
            var pos = Chessboard.GetChessPos(activity.To, activity.Initiator == 0);
            var sp = EffectsPoolingControl.instance.GetPosState(conduct, pos);
            if (sp == null) return;
            Sprites.Add(conduct.Kind, sp);
        }

    }

    private IEnumerator FullScreenRouse()
    {
        if(RouseEffectObj.activeSelf)
            RouseEffectObj.SetActive(false);
        RouseEffectObj.SetActive(true);
        yield return new WaitForSeconds(1.5f);
    }

    //主棋子执行演示
    private Tween OnMajorProcess(Activity activity, FightCardData offense, FightCardData target)
    {
        var tween = DOTween.Sequence();
        if (offense.Style.ArmedType >= 0 &&
            offense.Status.GetBuff(CardState.Cons.Imprisoned) < 1) //武将有自己的攻击特效
            tween.Join(CardAnimator.SkillEffect(activity, offense, true, target));

        //添加附属方的演示
        tween.Join(SubPartiesProcess(activity, offense, target));
        return tween;
    }

    //被执行主意图(主要行动)后的伤害/治疗表现
    private Tween SufferAction(FightCardData offOp,FightCardData target, Activity activity)
    {
        var tween = DOTween.Sequence();
        var conducts = activity.Conducts
            .Where(c => c.Kind == CombatConduct.DamageKind ||
                        c.Kind == CombatConduct.KillingKind ||
                        c.Kind == CombatConduct.HealKind ||
                        c.Kind == CombatConduct.BuffKind
            ).ToArray();
        if (conducts.Length == 0) return tween;
        target.UpdateActivity(activity.Result.Status); //更新战斗状态(血量)

        tween.Join(CardAnimator.DisplayTextEffect(target, activity));
        //如果是武将类兵种，将会执行兵种文字效果
        if (offOp != null && target.Style.ArmedType >= 0)
            tween.Join(CardAnimator.SkillEffect(activity, target, false, offOp));
        foreach (var conduct in conducts)
        {
            switch (activity.Result.Type)
            {
                case ActivityResult.Types.Suffer:
                case ActivityResult.Types.Friendly:
                {
                    switch (conduct.Kind)
                    {
                        case CombatConduct.DamageKind:
                        case CombatConduct.KillingKind:
                            tween.Join(CardAnimator.SufferingAttack(target, activity, conduct, Chessboard));
                            break;
                        case CombatConduct.HealKind:
                            tween.Join(CardAnimator.GenerateHealEffect(target, conduct));
                            break;
                    }

                    break;
                }
                case ActivityResult.Types.Dodge:
                    tween.Join(CardAnimator.SideDodge(target));
                    break;
                case ActivityResult.Types.Shield:
                    tween.Join(CardAnimator.UpdateStateEffect(target, CardState.Cons.Shield));
                    break;
                case ActivityResult.Types.Invincible:
                    tween.Join(CardAnimator.UpdateStateEffect(target, CardState.Cons.Invincible));
                    break;
                case ActivityResult.Types.EaseShield:
                    tween.Join(CardAnimator.UpdateStateEffect(target, CardState.Cons.EaseShield));
                    break;
                default:
                    break;
            }
        }

        return tween;
    }

    //反馈类意图执行(主要是反击，非触发类)
    private Tween OnCounterAction(Activity activity, FightCardData offender, FightCardData target)
    {
        return DOTween.Sequence()
            .Join(CardAnimator.Counter(offender, target))
            .Join(CardAnimator.GetCombatStrikeEffect(activity,offender, target))
            .Join(SufferAction(offender, target, activity));
    }

    //非主棋子交互演示
    private Tween SubPartiesProcess(Activity activity, FightCardData offOp, FightCardData target)
    {
        var tween = DOTween.Sequence().Join(CardAnimator.GetCombatStrikeEffect(activity,offOp, target));
        foreach (var conduct in activity.Conducts)
            if (conduct.Kind == CombatConduct.BuffKind)
                tween.Join(CardAnimator.UpdateStateEffect(target, (CardState.Cons) conduct.Element));
        return tween.Join(SufferAction(offOp, target, activity));
    }

    //执行后续动作(如：回到位置上)
    private IEnumerator Finalization(FightCardData card)
    {
        yield return new WaitForSeconds(CardAnimator.charge);
        yield return CardAnimator.FinalizeAction(card,Chessboard.GetChessPos(card).transform.position)?.WaitForCompletion();
    }

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