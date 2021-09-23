using System.Collections.Generic;
using System.Linq.Expressions;
using CorrelateLib;
using Newtonsoft.Json;

public class ChessboardTester : ChessboardManager
{
    public DataTable DataTable;
    public EffectsPoolingControl EffectsPooling;
    public PlayerDataForGame PlayerData;
    private GameResources gameResources;

    void Start()
    {
        DataTable.Init();
        PlayerData.Init();
        gameResources = new GameResources();
        CardMap = new Dictionary<int, FightCardData>();
        gameResources.Init();
        EffectsPooling.Init();

        NewWar.StartButton.onClick.AddListener(InvokeCard);
        InitCardUi();
    }


    //战斗结算

    //执行一套主行动 + 附属行动的程序函数

    //private IEnumerator ProceedActivitySet(List<Activity> actSet)
    //{
    //    FightCardData major = null; //进攻点
    //    var tween = DOTween.Sequence();
    //    //每一个MajorActivity下面会有多个subActivity
    //    //整理出每一套行动的主行动内容与附属内容
    //    var isInitialMove = true;
    //    foreach (var activity in actSet)
    //    {
    //        var target = CardMap[activity.To];
    //        var intensive = activity.GetIntention(); //获取行动意图类型
    //        if (intensive == Activity.Intention.Sprite)
    //        {
    //            OnSpriteActivity(activity);
    //            continue;
    //        }
    //        target.UpdateStatus(activity.Result.Status);
    //        //ClearCardStates(target);
    //        tween.Join(CardAnimator.UpdateStatusEffect(target));
    //        //if (activity.From < 0)
    //        //{
    //        //    tween.Join(SubPartiesProcess(activity, null, target));
    //        //    continue;
    //        //}

    //        var offense = CardMap[activity.From];
    //        offense.UpdateStatus(activity.OffenderStatus);
    //        tween.Join(CardAnimator.UpdateStatusEffect(offense));
    //        yield return tween.WaitForCompletion();
    //        tween = DOTween.Sequence();
    //        //ClearCardStates(offense);
    //        if (isInitialMove || activity.GetIntention() == Activity.Intention.Major)
    //        {
    //            isInitialMove = false;
    //            yield return tween.WaitForCompletion();
    //            yield return new WaitForSeconds(0.3f);
    //            tween = DOTween.Sequence();
    //            major = offense;
    //            if (offense.Style.Type == 0) //如果近战需要预先移动到对面
    //                yield return CardAnimator.MeleeMoving(offense, target)?.WaitForCompletion();
    //            if (activity.Conducts.Any(c => c.Rouse > 0))
    //                yield return FullScreenRouse();
    //            yield return
    //                MainActionBegin(offense.combatType, offense, target)?.OnComplete().WaitForCompletion(); //执行主要活动(一般是进攻)
    //            tween.Join(OnMajorProcess(activity, offense, target));
    //        }
    //        else if (intensive == Activity.Intention.Counter)
    //        {
    //            yield return tween.WaitForCompletion();
    //            if (activity.Conducts.Any(c => c.Rouse > 0))
    //                yield return FullScreenRouse();
    //            else yield return new WaitForSeconds(CardAnimator.charge);
    //            yield return OnCounterAction(activity, offense, target)
    //                .OnComplete().WaitForCompletion(); //加入反馈活动
    //            tween = DOTween.Sequence();
    //        }
    //        else
    //        {
    //            tween.Join(SubPartiesProcess(activity, offense, target)); //加入附属活动(buff，效果类)
    //        }

    //    }

    //    yield return tween.WaitForCompletion();
    //    if (major != null)
    //        yield return Finalization(major); //结束活动并返回

    //    Tween MainActionBegin(int combatStyle, FightCardData of,
    //        FightCardData tg)
    //    {
    //        return combatStyle == 0
    //            ? CardAnimator.MeleeActivity(of, tg)
    //            : CardAnimator.RangePreAction(of);
    //    }
    //}

    //private void ClearCardStates(FightCardData card)
    //{
    //    foreach (var map in card.States)
    //    {
    //        if (card.Status.GetBuff(map.Key) > 0 || map.Value == null) continue;
    //        EffectsPoolingControl.instance.TakeBackStateIcon(map.Value);
    //        card.States[map.Key] = null;
    //    }
    //}

    //被执行主意图(主要行动)后的伤害/治疗表现

    //反馈类意图执行(主要是反击，非触发类)

    //非主棋子交互演示

    //执行后续动作(如：回到位置上)
}