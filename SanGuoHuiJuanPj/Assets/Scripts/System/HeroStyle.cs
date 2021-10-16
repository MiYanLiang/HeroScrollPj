using System;
using System.Linq;
using Assets.System.WarModule;
using DG.Tweening;
using UnityEngine;
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
    public virtual void RespondStatusEffect(Activity activity, FightCardData target, int effectId) {}

    public virtual int GetMilitarySparkId(Activity activity) => Effect.Basic001;

    public virtual void NumberEffect(ActivityResult result, FightCardData target, Damage.Types type)
    {
    }

}
/// <summary>
/// 棋盘执行的数据调用抽象层
/// </summary>
public class ChessmanStyle : ChessUiStyle
{
    public virtual void UpdateStatus(ChessStatus chessStatus, FightCardData card)
    {
        card.UpdateActivityStatus(chessStatus);
        CardAnimator.instance.UpdateStateIcon(card);
    }

    /// <summary>
    /// 主行动，施展 方法
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="offense"></param>
    /// <returns></returns>
    public virtual void ActivityEffect(Activity activity, FightCardData offense) => DOTween.Sequence();

    public virtual Tween ResultAnimation(ActivityResult result, FightCardData target) => DOTween.Sequence();

    public virtual void ResultEffectTween(ActivityResult result, FightCardData card){}
}
public class InstantEffectStyle : CombatStyle
{
    public void Activity(Activity activity, FightCardData target)
    {
        target.ChessmanStyle.RespondStatusEffect(activity, target, GetSpriteSpark(activity));
        CardAnimator.instance.UpdateStateIcon(target);
    }

    private int GetSpriteSpark(Activity activity)
    {
        var first = activity.Conducts.FirstOrDefault();
        if (first == null) return -1;
        return Effect.GetFloorBuffId(first.Element);
    }
}
public abstract class CardStyle : ChessmanStyle
{
    public override void ActivityEffect(Activity activity,FightCardData offense) => ActivityVText(activity, offense);

    public override void RespondStatusEffect(Activity activity, FightCardData target, int effectId)
    {
        CardAnimator.instance.UpdateStateIcon(target);
        RespondEffect(activity, target, effectId);
    }

    public override Tween ResultAnimation(ActivityResult result, FightCardData target)
    {
        switch (result.Type)
        {
            case ActivityResult.Types.Dodge:
                return CardAnimator.instance.SideDodgeAnimation(target);
            case ActivityResult.Types.Suffer:
            case ActivityResult.Types.EaseShield:
                return CardAnimator.instance.SufferShakeAnimation(target);
            case ActivityResult.Types.Assist:
            case ActivityResult.Types.Heal:
                return CardAnimator.instance.AssistEnlargeAnimation(target);
            case ActivityResult.Types.ChessPos:
            case ActivityResult.Types.Shield:
            case ActivityResult.Types.Invincible:
            case ActivityResult.Types.Kill:
            case ActivityResult.Types.Suicide:
                return DOTween.Sequence();
            default:
                throw new ArgumentOutOfRangeException(nameof(result), result.ToString());
        }
    }

    public override void NumberEffect(ActivityResult result,FightCardData target,Damage.Types type)
    {
        var value = 0;
        var color = ColorDataStatic.name_deepRed;
            
        switch (result.Type)
        {
            case ActivityResult.Types.Suffer:
                value = result.Status.LastSuffers.LastOrDefault();
                break;
            case ActivityResult.Types.EaseShield:
                value = result.Status.LastEaseShieldDamage;
                break;
            case ActivityResult.Types.Heal:
                value = result.Status.LastHeal;
                color = ColorDataStatic.huiFu_green;
                break;
            case ActivityResult.Types.ChessPos:
            case ActivityResult.Types.Assist:
            case ActivityResult.Types.Shield:
            case ActivityResult.Types.Invincible:
            case ActivityResult.Types.Dodge:
            case ActivityResult.Types.Kill:
            case ActivityResult.Types.Suicide:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        CardAnimator.instance.NumberEffectTween(target, value, color, type);
    }

    protected virtual void RespondEffect(Activity activity, FightCardData target, int effectId)
    {
        //如果要精灵闪花或文字反馈得加 c.Kind == CombatConduct.SpriteKind ||
        if (!activity.Conducts.Any(c => c.Kind == CombatConduct.DamageKind ||
                                        c.Kind == CombatConduct.ElementDamageKind ||
                                        c.Kind == CombatConduct.KillingKind ||
                                        c.Kind == CombatConduct.HealKind ||
                                        c.Kind == CombatConduct.BuffKind)) return;
        
        if (effectId >= 0) //如果id==null，没有特效(火花)
            SparkTween(activity, target, effectId);

        CardAnimator.instance.DisplayTextEffect(target, activity); //文字效果，描述反馈结果：流血，治疗，禁锢...
    }

    public override void ResultEffectTween(ActivityResult result, FightCardData card)
    {
        var vTextId = Effect.ActivityResultVText(result);
        if (vTextId > -1) CardAnimator.instance.VTextEffect(vTextId, card.cardObj.transform);

        switch (result.Type)
        {
            case ActivityResult.Types.Suffer:
            case ActivityResult.Types.Assist:
            case ActivityResult.Types.Dodge:
            case ActivityResult.Types.Invincible:
            case ActivityResult.Types.ChessPos:
            case ActivityResult.Types.Heal:
            case ActivityResult.Types.Kill:
            case ActivityResult.Types.Suicide:
                break;
            case ActivityResult.Types.Shield:
                CardAnimator.instance.UpdateStateIcon(card, CardState.Cons.Shield);
                break;
            case ActivityResult.Types.EaseShield:
                CardAnimator.instance.UpdateStateIcon(card, CardState.Cons.EaseShield);
                break;
            default:
                throw new ArgumentOutOfRangeException();
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
    protected abstract void ActivityVText(Activity activity, FightCardData offense);
}

public class HeroStyle : CardStyle
{
    protected override void ActivityVText(Activity activity, FightCardData offense)
    {
        if (activity.Skill <= 0) return;
        //武将有自己的攻击特效
        CardAnimator.instance.VTextEffect(Effect.HeroActivityVText(Military,activity.Skill), offense.cardObj.transform);
    }

    //武将特效
    public override int GetMilitarySparkId(Activity activity) => Effect.GetHeroSparkId(Military, activity.Skill);
}

public class TowerStyle : CardStyle
{
    public override int GetMilitarySparkId(Activity activity) => Effect.GetTowerSparkId(Military, activity.Skill);

    protected override void ActivityVText(Activity activity, FightCardData offense) => CardAnimator.instance.VTextEffect(Effect.TowerActivityVText(Military, activity.Skill), offense.cardObj.transform);
}

public class TrapStyle : CardStyle
{
    protected override void ActivityVText(Activity activity, FightCardData offense) => CardAnimator.instance.VTextEffect(Effect.TrapActivityVText(Military, activity.Skill), offense.cardObj.transform);
}