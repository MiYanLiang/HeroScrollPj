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
    private bool IsBusy { get; set; }
    private static int CardSeed;
    private GameResources gameResources;
    private Dictionary<int, FightCardData> CardData;

    void Start()
    {
        DataTable.Init();
        PlayerData.Init();
        gameResources = new GameResources();
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
        NewWar.StartButtonShow(false);
        var round = NewWar.ChessOperator.StartRound();
        StartCoroutine(AnimateRound(round));
    }

    private IEnumerator AnimateRound(ChessRound round)
    {
        IsBusy = true;
        for (int i = 0; i < round.Processes.Length; i++)
        {
            var process = round.Processes[i];

            //var list = new List<List<Activity>> {new List<Activity>()};
            //var getLast = false;
            ////这样写为了把Processes中，如果非主活动被放置在主活动的前排，依然归到主活动下面来执行
            //for (int j = 0; j < process.Activities.Count; j++)
            //{
            //    var act = process.Activities[j];
            //    if (act.GetIntensive() != Activity.Intention.Major)
            //    {
            //        list.Last().Add(act);
            //        continue;
            //    }
            //    if (getLast) list.Add(new List<Activity>());
            //    list.Last().Insert(0, act);
            //    getLast = true;
            //}
            Chessboard.OnActivityBegin(process.Pos, process.Scope == 0);
            //列表配置应该是主活动在附列表最上端，然后附活动跟着在列表后面
            yield return ProceedActivitySet(process.Activities);
            //foreach (var actSet in list)
            //   yield return ProceedActivitySet(actSet);
            foreach (var map in CardData) 
                Chessboard.ResetPos(map.Value);
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
        foreach (var activity in actSet)
        {
            var offense = CardData[activity.From];
            var target = CardData[activity.To];
            offense.UpdateStatus(activity.OffenderStatus);
            target.UpdateStatus(activity.Result.Status);
            var intensive = activity.GetIntensive(); //获取行动意图类型
            switch (intensive)
            {
                //主棋子执行演示 *注：已经包涵了附属附属演示了
                case Activity.Intention.Major:
                    yield return tween.WaitForCompletion();
                    tween = DOTween.Sequence();
                    major = offense;
                    if (offense.Style.CombatStyle == 0) //如果近战需要预先移动到对面
                        yield return CardAnimator.MeleeMoving(offense, target)?.WaitForCompletion();
                    yield return MainActionBegin(offense.combatType, offense, target)?.WaitForCompletion();//执行主要活动(一般是进攻)
                    tween.Join(OnMajorProcess(activity, offense, target));
                    break;
                //附属棋子执行演示
                case Activity.Intention.Attach:
                    tween.Join(SubPartiesProcess(activity, offense, target)); //加入附属活动(buff，效果类)
                    break;
                //如果有反馈式的活动(一般来说是反击，触发类的不属于反馈)
                case Activity.Intention.Counter:
                    yield return tween.WaitForCompletion();
                    yield return new WaitForSeconds(CardAnimator.charge);
                    yield return OnCounterAction(activity, offense, target).WaitForCompletion(); //加入反馈活动
                    tween = DOTween.Sequence();
                    break;
                //如果是棋手维度的效果这里播放
                case Activity.Intention.UnDefined:
                default:
                    break;
            }
            //击退一格
            if (!activity.IsRePos) continue;
            yield return tween.Join(CardAnimator.OnRePos(target, Chessboard.GetScope(target.IsPlayer)[activity.RePos])).WaitForCompletion();
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

    //主棋子执行演示
    private Tween OnMajorProcess(Activity activity, FightCardData offense, FightCardData target)
    {
        var tween = DOTween.Sequence();
        if (offense.Style.ArmedType >= 0 &&
            offense.Status.GetBuff(FightState.Cons.Imprisoned) < 1) //武将有自己的攻击特效
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
        if (target.Style.ArmedType >= 0)
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
                    tween.Join(CardAnimator.UpdateStateEffect(target,FightState.Cons.Shield));
                    break;
                case ActivityResult.Types.Invincible:
                    tween.Join(CardAnimator.UpdateStateEffect(target,FightState.Cons.Invincible));
                    break;
                case ActivityResult.Types.ExtendedShield:
                    tween.Join(CardAnimator.UpdateStateEffect(target,FightState.Cons.ExtendedHp));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
                tween.Join(CardAnimator.UpdateStateEffect(target, (FightState.Cons) conduct.Element));
        return tween.Join(SufferAction(offOp, target, activity));
    }

    //执行后续动作(如：回到位置上)
    private IEnumerator Finalization(FightCardData card)
    {
        yield return new WaitForSeconds(CardAnimator.charge);
        yield return CardAnimator.FinalizeAction(card,Chessboard.GetChessPos(card).transform.position)?.WaitForCompletion();
    }
}