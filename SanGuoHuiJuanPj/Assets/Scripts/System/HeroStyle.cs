using System;
using System.Linq;
using Assets.System.WarModule;
using DG.Tweening;
using UnityEngine;
using UnityEngine.WSA;
using Random = UnityEngine.Random;

/// <summary>
/// 所有客户端棋盘UI演示的抽象层，子类实现棋子/棋盘/精灵的动画演示.
/// </summary>
public class ChessUiStyle : CombatStyle
{
    protected ChessUiStyle()
    {
        
    }
    protected ChessUiStyle(int military, int armedType, int combatStyle, int element, int strength, int level,
        int troop) : base(military, armedType, combatStyle, element, strength, level, troop)
    {

    }

    public static T Instance<T>(CombatStyle style) where T : ChessUiStyle, new() =>
        Instance<T>(style.Military, style.ArmedType, style.Type, style.Element, style.Strength, style.Level,
            style.Troop);

    public static T Instance<T>(int military, int armedType, Types type, int element, int strength, int level,
        int troop) where T : ChessUiStyle, new()
    {
        return new T
        {
            Military = military, ArmedType = armedType, Type = type, Element = element,
            Strength = strength, Level = level, Troop = troop
        };
    }

    /// <summary>
    /// 反馈行动
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="target"></param>
    /// <param name="effectId"></param>
    /// <returns></returns>
    public virtual Sequence RespondTween(Activity activity, FightCardData target, int effectId) => DOTween.Sequence();

    public virtual int GetMilitarySparkId(Activity activity) => Effect.Basic001;

}
/// <summary>
/// 棋盘执行的数据调用抽象层
/// </summary>
public class ChessmanStyle : ChessUiStyle
{
    public virtual Tween UpdateStatusTween(ChessStatus chessStatus, FightCardData card)
    {
        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                card.UpdateActivityStatus(chessStatus);
                CardAnimator.instance.UpdateStateIcon(card);
            });
    }

    /// <summary>
    /// 主行动，施展 方法
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="offense"></param>
    /// <returns></returns>
    public virtual void OffensiveEffect(Activity activity, FightCardData offense) => DOTween.Sequence();

}
public class SpriteStyle : CombatStyle
{
    public Sequence Activity(Activity activity, FightCardData target) => DOTween.Sequence()
        .Join(target.ChessmanStyle.RespondTween(activity, target, GetSpriteSpark(activity)))
        .AppendCallback(() => CardAnimator.instance.UpdateStateIcon(target));

    private int GetSpriteSpark(Activity activity)
    {
        var first = activity.Conducts.FirstOrDefault();
        if (first == null) return -1;
        return Effect.GetFloorBuffId(first.Element);
    }
}
public abstract class CardStyle : ChessmanStyle
{
    public override void OffensiveEffect(Activity activity,FightCardData offense) => OffenseVText(activity, offense);

    public override Sequence RespondTween(Activity activity, FightCardData target, int effectId) =>
        DOTween.Sequence().AppendCallback(() =>
            {
                target.UpdateActivityStatus(activity.Result.Status);
                CardAnimator.instance.UpdateStateIcon(target);
                RespondEffect(activity, target, effectId);
            })
            .Append(RespondAnimation(activity, target));

    private Tween RespondAnimation(Activity activity, FightCardData target)
    {
        switch (activity.Result.Type)
        {
            case ActivityResult.Types.Suffer when 
                activity.Conducts.Any(c => c.Kind == CombatConduct.DamageKind ||
                                           c.Kind == CombatConduct.KillingKind):
            case ActivityResult.Types.Friendly when 
                activity.Conducts.Any(c => c.Kind == CombatConduct.DamageKind ||
                                           c.Kind == CombatConduct.KillingKind):
            case ActivityResult.Types.EaseShield when 
                activity.Conducts.Any(c => c.Kind == CombatConduct.DamageKind ||
                                           c.Kind == CombatConduct.KillingKind):
                return CardAnimator.instance.SufferShakeAnimation(target);
            case ActivityResult.Types.Dodge:
                return CardAnimator.instance.SideDodgeAnimation(target);
        }
        return DOTween.Sequence();
    }
    protected virtual void RespondEffect(Activity activity, FightCardData target, int effectId)
    {
        var conducts = activity.Conducts
            .Where(c => c.Kind == CombatConduct.DamageKind ||
                        c.Kind == CombatConduct.KillingKind ||
                        c.Kind == CombatConduct.HealKind ||
                        c.Kind == CombatConduct.BuffKind
            ).ToArray();
        if (conducts.Length == 0) return;

        if (effectId >= 0) //如果id==null，没有特效(火花)
            SparkTween(activity, target, effectId);

        CardAnimator.instance.DisplayTextEffect(target, activity);//文字效果，描述反馈结果：伤害，闪避，格挡

        //动画演示+上状态效果
        foreach (var conduct in conducts)
        {
            switch (activity.Result.Type)
            {
                case ActivityResult.Types.Suffer:
                case ActivityResult.Types.Friendly:
                    switch (conduct.Kind)
                    {
                        case CombatConduct.DamageKind:
                        case CombatConduct.KillingKind:
                            CardAnimator.instance.NumberEffectTween(target, conduct);
                            break;
                        case CombatConduct.HealKind:
                            CardAnimator.instance.NumberEffectTween(target, conduct);
                            break;
                    }
                    break;
                case ActivityResult.Types.Dodge:
                    CardAnimator.instance.VTextEffect(Effect.VTextDodge, target.cardObj.transform);
                    break;
                case ActivityResult.Types.Shield:
                    CardAnimator.instance.UpdateStateIcon(target, CardState.Cons.Shield);
                    break;
                case ActivityResult.Types.Invincible:
                    CardAnimator.instance.VTextEffect(Effect.VTextInvincible, target.cardObj.transform);
                    break;
                case ActivityResult.Types.EaseShield:
                    CardAnimator.instance.NumberEffectTween(target, conduct, ColorDataStatic.name_red);
                    CardAnimator.instance.UpdateStateIcon(target, CardState.Cons.EaseShield);
                    break;
            }
        }
    }

    private void SparkTween(Activity activity, FightCardData target, int effectId)
    {
        foreach (var conduct in activity.Conducts)
        {
            GameObject effect;
            switch (activity.Skill)
            {
                case 0:
                    effect = EffectsPoolingControl.instance.GetSparkEffect(Effect.Basic001, target.cardObj.transform,
                        0.5f);
                    effect.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360));
                    break;
                default:
                    effect = EffectsPoolingControl.instance.GetSparkEffect(effectId, target.cardObj.transform, 1f);
                    break;
                case -1: continue; //-1 = 没有特效
            }

            //一些效果需要反向显示
            if (Effect.IsInvertControl(effectId))
            {
                var rotation = target.isPlayerCard ? new Quaternion(0, 0, 180, 0) : Quaternion.identity;
                effect.transform.localRotation = rotation;
            }

            if (conduct.Critical > 0 || conduct.Rouse > 0) //如果会心或暴击 物体变大1.5
                effect.transform.localScale = new Vector3(1.5f, 1.5f, 1);
            else effect.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// 攻击方竖文字
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="offense"></param>
    /// <returns></returns>
    protected abstract void OffenseVText(Activity activity, FightCardData offense);
}

public class HeroStyle : CardStyle
{
    protected override void OffenseVText(Activity activity, FightCardData offense)
    {
        if (activity.Skill == 0) return;
        //武将有自己的攻击特效
        CardAnimator.instance.VTextEffect(MilitaryOffenseVTextId(activity), offense.cardObj.transform);
    }

    private string MilitaryOffenseVTextId(Activity activity)
    {
        if (activity.Skill == 0)
            return null;
        switch (Military)
        {
            case 0: return null;
            case 13: return null;
            case 58: //铁骑
                if(activity.Skill == 2)
                    return "58_0";
                break;
            case 55: //火船
                if (activity.Skill == 2)
                    return "55_0"; //火船爆炸
                break;
            case 19: //弩兵
            case 51: //强弩
                switch (activity.Skill)
                {
                    default:
                        return 19.ToString();//二连
                    case 2:
                        return 51.ToString();//三连
                }
            case 65: //黄巾
                break;
        }

        return Military.ToString();
    }

    //武将特效
    public override int GetMilitarySparkId(Activity activity) => Effect.GetHeroSparkId(Military, activity.Skill);
}

public class TowerStyle : CardStyle
{
    public override int GetMilitarySparkId(Activity activity) => Effect.GetTowerSparkId(Military, activity.Skill);

    protected override void OffenseVText(Activity activity, FightCardData offense) => DOTween.Sequence();
}

public class TrapStyle : CardStyle
{
    protected override void OffenseVText(Activity activity, FightCardData offense) => DOTween.Sequence();
}