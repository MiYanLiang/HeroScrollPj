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

    private GameResources gameResources;

    void Start()
    {
        DataTable.Init();
        PlayerData.Init();
        gameResources = new GameResources();
        gameResources.Init();
        EffectsPooling.Init();
        InitCard(NewWar.Player, true);
        InitCard(NewWar.Enemy, false);
        NewWar.StartButton.onClick.AddListener(InvokeCard);
    }

    private void InitCard(NewWarManager.ChessCard[] cards, bool isPlayer)
    {
        foreach (var card in cards)
        {
            var gc = GameCard.Instance(card.Id, (int) card.Type, card.Level);
            if (card.Type == GameCardType.Base)
            {
                var home = Instantiate(HomePrefab);
                var h = new FightCardData(DataTable.BaseLevel[card.Level].BaseHp +
                                          DataTable.PlayerLevelConfig[card.Level].BaseHpAddOn);
                h.isPlayerCard = isPlayer;
                h.cardType = gc.Type;
                h.cardObj = home;
                var hop = NewWar.ChessOperator.RegOperator(h);
                hop.SetPos(card.Pos);
                Chessboard.PlaceCard(card.Pos,h);
                continue;
            }

            var ui = Instantiate(PrefabUi);
            var fc = new FightCardData(gc);
            fc.isPlayerCard = isPlayer;
            fc.cardObj = ui;
            ui.Init(gc);
            var op = NewWar.ChessOperator.RegOperator(fc);
            op.SetPos(card.Pos);
            Chessboard.PlaceCard(card.Pos, fc);
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
            var list = new List<List<Activity>> {new List<Activity>()};
            var getLast = false;
            //这样写为了把Processes中，如果非主活动被放置在主活动的前排，依然归到主活动下面来执行
            for (int j = 0; j < process.Activities.Count; j++)
            {
                var act = process.Activities[j];
                if (act.GetIntensive() != Activity.Intention.Major)
                {
                    list.Last().Add(act);
                    continue;
                }
                if (getLast) list.Add(new List<Activity>());
                list.Last().Insert(0, act);
                getLast = true;
            }
            Chessboard.OnActivityBegin(process.Pos, process.Scope == 0);
            //列表配置应该是主活动在附列表最上端，然后附活动跟着在列表后面
            foreach (var actSet in list)
               yield return ProceedActivitySet(actSet);
            foreach (var map in NewWar.ChessOperator.Data)
            {
                Chessboard.ResetPos(map.Value, map.Key);
            }
        }

        IsBusy = false;
        NewWar.StartButtonShow(true);
    }

    //执行一套主行动 + 附属行动的程序函数
    private IEnumerator ProceedActivitySet(List<Activity> actSet)
    {
        FightCardData major = null; //进攻点
        var response =
            new List<(Activity activity, AttackStyle style, FightCardData offender, FightCardData target)>(); //反馈行动
        //每一个MajorActivity下面会有多个subActivity
        //整理出每一套行动的主行动内容与附属内容
        foreach (var activity in actSet)
        {
            var minors = new List<IEnumerator>(); //附属行动效果
            //执行方必须用最新的位置
            var offOp = NewWar.Grid.GetChessPos(activity.From, activity.IsChallenger == 0).Operator;
            var offPos = Chessboard.GetData().FirstOrDefault(p => p.Operator == offOp);
            if (offOp == null)
                throw new NullReferenceException(
                    $"{nameof(ProceedActivitySet)}(): Unable to find {nameof(offOp)} in grid[{activity.From}] player ={activity.IsChallenger}.");
            if (offPos == null)
                throw new NullReferenceException(
                    $"{nameof(ProceedActivitySet)}(): Unable to find {nameof(offPos)} in chessboard where Op = {offOp.GetType().Name} ({offOp.Pos} player ={offOp.IsChallenger}).");
            var offense = offPos.Card;
            if (offPos == null)
                throw new NullReferenceException(
                    $"{nameof(ProceedActivitySet)}(): Unable to find Card in chessboard where pos = ({offPos.Pos} player ={offPos.IsChallenger}).");
            //承受方却有可能还在旧位置
            var target = Chessboard.GetChessPos(activity.To, activity.TargetIsChallenger).Card;
            var intensive = activity.GetIntensive(); //获取行动意图类型
            switch (intensive)
            {
                //执行主意图，一般来说都是棋子攻击
                case Activity.Intention.Major:
                    major = offense;
                    yield return OnMajorIntention(offOp, offense, target, activity);
                    break;
                //如果有反馈式的活动(一般来说是反击，触发类的不属于反馈)
                case Activity.Intention.Responsive:
                    yield return OnResponseAction(activity, offOp.Style, offense, target); //加入反馈活动
                    break;
                //其余附属类的活动
                case Activity.Intention.Attach:
                    yield return UpdateActionEffectProcess(activity, offOp, target); //加入附属活动(buff，效果类)
                    break;
                //如果是棋手维度的效果这里播放
                case Activity.Intention.UnDefined:
                default:
                    break;
            }
            
            //如果移位必须即刻换位，否则下一步会找不到对象
            if (activity.IsRePos) Chessboard.PlaceCard(activity.RePos, target);
        }
        if (major != null)
            yield return Finalization(major); //结束活动并返回
    }

    //主要意图执行
    private IEnumerator OnMajorIntention(IChessOperator offOp, FightCardData offense,
        FightCardData target, Activity activity)
    {
        if (offOp.Style.CombatStyle == 0) //如果近战需要预先移动到对面
            yield return CardAnimator.MeleeMoving(offense, target)?.WaitForCompletion();
        yield return MainActionBegin(offOp, offense, target)?.WaitForCompletion();//执行主要活动(一般是进攻)
        var tween = DOTween.Sequence()
            .Join(UpdateActionEffectProcess(activity,offOp,target))
            .Join(OnSufferAction(target, activity));
        yield return tween.WaitForCompletion();

        Tween MainActionBegin(IChessOperator op, FightCardData of,
            FightCardData tg)
        {
            return op.Style.CombatStyle == 0
                ? CardAnimator.MeleeActivity(of, tg)
                : CardAnimator.RangeActivity(of);
        }
    }

    //被执行主意图(主要行动)后的伤害/治疗表现
    private Tween OnSufferAction(FightCardData target, Activity activity)
    {
        var conduct = activity.Conducts
            .FirstOrDefault(c => c.Kind == CombatConduct.DamageKind ||
                                 c.Kind == CombatConduct.KillingKind ||
                                 c.Kind == CombatConduct.HealKind ||
                                 c.Kind == CombatConduct.BuffKind
            );
        if (conduct.Equals(default)) return null;

        target.UpdateActivity(activity.Result.Status); //更新战斗状态(血量)

        //击退一格
        if (activity.IsRePos)
        {
            return DOTween.Sequence()
                .Join(CardAnimator.SufferAction(target, activity, conduct, Chessboard))
                .Join(CardAnimator.OnRePos(target,
                    Chessboard.GetScope(target.IsPlayer)[activity.RePos]));
        }

        return CardAnimator.SufferAction(target, activity, conduct, Chessboard);
    }

    //反馈类意图执行(主要是反击，非触发类)
    private IEnumerator OnResponseAction(Activity activity, AttackStyle style,FightCardData offender ,FightCardData target)
    {
        var tween = DOTween.Sequence().Join(CardAnimator.Counter(offender, target));
        foreach (var conduct in activity.Conducts)
            tween.Join(CardAnimator.GetCombatStrikeEffect(style, conduct, target));
        yield return tween.Join(OnSufferAction(target, activity)).WaitForCompletion();
    }

    //附属效果
    private Tween UpdateActionEffectProcess(Activity activity, IChessOperator op, FightCardData target)
    {
        var tween = DOTween.Sequence();
        foreach (var conduct in activity.Conducts)
            tween.Join(CardAnimator.GetCombatStrikeEffect(op.Style, conduct, target));
        return tween.Join(OnSufferAction(target, activity));
    }

    //执行后续动作(如：回到位置上)
    private IEnumerator Finalization(FightCardData card)
    {
        yield return new WaitForSeconds(CardAnimator.charge);
        yield return CardAnimator.FinalizeAction(card,Chessboard.GetChessPos(card).transform.position)?.WaitForCompletion();
    }


    #region Helper
    //同步执行器
    private IEnumerator ParallelInvocation(params IEnumerator[] parallel)
    {
        var list = new List<Coroutine>();
        foreach (var ie in parallel)
        {
            var co = StartCoroutine(ie);
            list.Add(co);
        }

        yield return new WaitUntil(() => list.All(o => o == null));
    }
    #endregion
}