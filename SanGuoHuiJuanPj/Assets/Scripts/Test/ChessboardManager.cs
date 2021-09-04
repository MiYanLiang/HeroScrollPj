﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
using UnityEngine;

public class ChessboardManager : MonoBehaviour
{
    public WarGameCardUi PrefabUi;
    public WarGameCardUi HomePrefab;
    public NewWarManager NewWar;
    public Chessboard Chessboard;
    public GameObject RouseEffectObj;

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private GameObject fireUIObj;

    [SerializeField] private GameObject boomUIObj;

    [SerializeField] private GameObject gongKeUIObj;
    protected Dictionary<int, FightCardData> CardMap;
    protected bool IsBusy { get; set; }
    protected Dictionary<int,EffectStateUi>Sprites { get; set; }
    private ChessOperatorManager<FightCardData> chessOp;

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

    private IEnumerator AnimateRound(ChessRound round,ChessboardOperator chess)
    {
        IsBusy = true;
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

            foreach (var card in CardMap.ToDictionary(c=>c.Key,c=>c.Value))
            {
                Chessboard.ResetPos(card.Value);
                if (card.Value.CardType == GameCardType.Base) continue;
                if (!card.Value.Status.IsDeath) continue;
                var ui = CardMap[card.Key];
                CardMap.Remove(card.Key);
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

    private IEnumerator OnChessboardProcess(List<Activity> activities)
    {
        var tween = DOTween.Sequence();
        foreach (var activity in activities)
        {
            var target = CardMap[activity.To];
            target.UpdateActivity(activity.Result.Status);
            tween.Join(CardAnimator.UpdateStatusEffect(target));
            //.Join(SubPartiesProcess(activity, null, target));
        }

        yield return tween.WaitForCompletion();
    }

    private IEnumerator ProceedChessmanActivities(List<Activity> actSet)
    {
        FightCardData offense = null;
        Sequence effectTween = DOTween.Sequence().Pause(); //注意DoTween必须在一个线程添加，如果没暂停，一旦yield所有tween将直接执行。
        Sequence innerTween = null;
        foreach (var activity in actSet)
        {
            Sequence chessboardTween = null;
            Sequence chessboardShake = null;
            var target = CardMap[activity.To];
            var op = offense = CardMap[activity.From];
            var tg = target;
            if (activity.Intent == Activity.Counter)
            {
                yield return effectTween.Play().WaitForCompletion();
                yield return op.ChessmanStyle.CounterTween(activity, op, tg)
                    .OnComplete(() => PlaySoundEffect(activity, op.Style, tg))
                    .WaitForCompletion();
                yield return op.ChessmanStyle.ActivityEffectTween(activity, op, tg, Chessboard.transform).WaitForCompletion();
                effectTween = DOTween.Sequence().Pause();
                continue;
            }

            effectTween.Join(op.ChessmanStyle.ActivityEffectTween(activity, op, tg, Chessboard.transform));
            if (activity.Inner != null && activity.Inner.Length > 0)
            {
                RecursiveInnerCount = 0;
                innerTween = ProcessInnerActivities(activity.Inner);
            }

            if (innerTween != null)
            {
                effectTween.Join(innerTween);
                innerTween = null;
            }

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
            if (chessboardTween != null)
                yield return chessboardTween.Play().WaitForCompletion();
            yield return offense.ChessmanStyle.PreActionTween(offense, target).WaitForCompletion();
            if (offense.ChessmanStyle.Type == CombatStyle.Types.Melee)
                yield return CardAnimator.StepBackAndHit(offense).WaitForCompletion();
        }

        yield return effectTween.Play().WaitForCompletion();

        if (offense != null)
        {
            var chessPos = Chessboard.GetChessPos(offense).transform.position;
            yield return offense.ChessmanStyle.FinalizeActionTween(offense, chessPos).WaitForCompletion();
        }
    }

    private const int RecursiveInnerActivitiesProtection = 9999;
    private int RecursiveInnerCount;
    private Sequence ProcessInnerActivities(Activity[] activities)
    {
        var tween = DOTween.Sequence();
        foreach (var activity in activities)
        {
            if (RecursiveInnerCount >= RecursiveInnerActivitiesProtection)
                throw new StackOverflowException($"{nameof(ProcessInnerActivities)} Count = {RecursiveInnerCount}!");
            var tg = CardMap[activity.To];
            var of = CardMap[activity.From];
            tween.Join(of.ChessmanStyle.ActivityEffectTween(activity, of, tg, Chessboard.transform));
            if (activity.Inner != null)
                tween.Join(ProcessInnerActivities(activity.Inner));
            RecursiveInnerCount++;
        }
        return tween;
    }

    private class ProcessPack
    {
        public List<Activity> Activities = new List<Activity>();
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
            var pos = Chessboard.GetChessPos(activity.To, activity.Initiator == 0);
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