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
    public virtual Sequence RespondTween(Activity activity, FightCardData target, string effectId = null) => DOTween.Sequence();

    public virtual string GetMilitarySparkId(Activity activity) => Effect.Basic0A;

}
/// <summary>
/// 棋盘执行的数据调用抽象层
/// </summary>
public class ChessmanStyle : ChessUiStyle
{
    public virtual Tween UpdateStatusTween(ChessStatus chessStatus, FightCardData card) => DOTween.Sequence()
        .AppendCallback(() => card.UpdateActivityStatus(chessStatus)).Append(CardAnimator.UpdateStateIcon(card));
    /// <summary>
    /// 主行动，施展+反馈
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="offense"></param>
    /// <returns></returns>
    public virtual Tween OffensiveTween(Activity activity, FightCardData offense) => DOTween.Sequence();

}
public class SpriteStyle : CombatStyle
{
    public Sequence Activity(Activity activity, FightCardData target) => DOTween.Sequence()
        .Join(target.ChessmanStyle.RespondTween(activity, target))
        .AppendCallback(() => CardAnimator.UpdateStateIcon(target));
}
public abstract class CardStyle : ChessmanStyle
{
    public override Tween OffensiveTween(Activity activity,FightCardData offense)
    {
        return DOTween.Sequence()
            .Join(OffenseVText(activity, offense));
    }

    public override Sequence RespondTween(Activity activity, FightCardData target, string effectId = null) =>
        base.RespondTween(activity, target, effectId)
            .AppendCallback(() => target.UpdateActivityStatus(activity.Result.Status))
            .Append(RespondAnimation(activity, target, effectId))
            .Join(CardAnimator.UpdateStateIcon(target));
    protected virtual Tween RespondAnimation(Activity activity, FightCardData target, string effectId)
    {
        var tween = DOTween.Sequence();
        var conducts = activity.Conducts
            .Where(c => c.Kind == CombatConduct.DamageKind ||
                        c.Kind == CombatConduct.KillingKind ||
                        c.Kind == CombatConduct.HealKind ||
                        c.Kind == CombatConduct.BuffKind
            ).ToArray();
        if (conducts.Length == 0) return tween;

        if (effectId != null) //如果id==null，没有特效(火花)
            tween.Join(SparkTween(activity, target, effectId));

        tween.Join(CardAnimator.DisplayTextEffect(target, activity));//文字效果，描述反馈结果：伤害，闪避，格挡

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
                            tween.Join(CardAnimator.NumberEffectTween(target, conduct))
                                .Join(CardAnimator.SufferShakeAnimation(target));
                            break;
                        case CombatConduct.HealKind:
                            tween.Join(CardAnimator.NumberEffectTween(target, conduct));
                            break;
                    }
                    break;
                case ActivityResult.Types.Dodge:
                    tween.Join(CardAnimator.SideDodgeAnimation(target));
                    tween.Join(CardAnimator.VTextEffect(Effect.VTextDodge, target.cardObj.transform));
                    break;
                case ActivityResult.Types.Shield:
                    tween.Join(CardAnimator.UpdateStateIcon(target, CardState.Cons.Shield));
                    //tween.Join(CardAnimator.VTextEffect(Effect.VTextShield, target.cardObj.transform));
                    break;
                case ActivityResult.Types.Invincible:
                    tween.Join(CardAnimator.VTextEffect(Effect.VTextInvincible, target.cardObj.transform));
                    break;
                case ActivityResult.Types.EaseShield:
                    tween.Join(CardAnimator.NumberEffectTween(target, conduct, ColorDataStatic.name_gray))
                        .Join(CardAnimator.UpdateStateIcon(target, CardState.Cons.EaseShield))
                        .Join(CardAnimator.SufferShakeAnimation(target));
                    break;
                default:
                    break;
            }
        }
        return tween;
    }

    private Tween SparkTween(Activity activity,FightCardData target,string effectId)
    {
        return DOTween.Sequence().AppendCallback(() =>
        {
            foreach (var conduct in activity.Conducts)
            {
                GameObject effect;
                switch (activity.Skill)
                {
                    case 0:
                        effect = EffectsPoolingControl.instance.GetEffect(Effect.Basic0A, target.cardObj.transform,
                            0.5f);
                        effect.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360));
                        break;
                    default:
                        effect = EffectsPoolingControl.instance.GetEffect(effectId, target.cardObj.transform, 1f);
                        break;
                    case -1: continue;//-1 = 没有特效
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
        });
    }

    /// <summary>
    /// 攻击方竖文字
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="offense"></param>
    /// <returns></returns>
    protected abstract Tween OffenseVText(Activity activity, FightCardData offense);
}

public class HeroStyle : CardStyle
{
    protected override Tween OffenseVText(Activity activity, FightCardData offense)
    {
        if (activity.Skill == 0) return DOTween.Sequence();
        //武将有自己的攻击特效
        return HeroOffenseVText(activity, offense);
    }

    protected virtual Tween HeroOffenseVText(Activity activity, FightCardData offense) =>
        CardAnimator.VTextEffect(MilitaryOffenseVTextId(activity), offense.cardObj.transform);

    protected override Tween RespondAnimation(Activity activity, FightCardData target, string effectId)
    {
        return DOTween.Sequence().Join(base.RespondAnimation(activity, target, effectId));
    }

    private string MilitaryOffenseVTextId(Activity activity)
    {
        if (activity.Skill == 0)
            return null;
        
        switch (Military)
        {
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
    public override string GetMilitarySparkId(Activity activity) => Effect.GetHeroSpark(Military, activity.Skill);
}

public class TowerStyle : CardStyle
{
    public override string GetMilitarySparkId(Activity activity) => Effect.GetTowerSpark(Military, activity.Skill);

    protected override Tween OffenseVText(Activity activity, FightCardData offense) => DOTween.Sequence();
}

public class TrapStyle : CardStyle
{
    protected override Tween OffenseVText(Activity activity, FightCardData offense) => DOTween.Sequence();
}