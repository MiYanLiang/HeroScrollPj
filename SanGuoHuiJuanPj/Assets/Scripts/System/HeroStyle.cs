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

    protected ChessUiStyle(int military, int armedType, int combat, int element,
        int strength, int level, int hitPoint, int speed, int troop, int intelligent, int recovery,int rare) : base(military,
        armedType, combat, element, strength, level, hitPoint, speed, troop, intelligent, recovery,rare)
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
    public virtual int GetMilitarySparkId(int skill) => Effect.Basic001;

    public virtual void NumberEffect(ActivityResult result, FightCardData target, Damage.Types type)
    {
    }

}
/// <summary>
/// 棋盘执行的数据调用抽象层
/// </summary>
public class ChessmanStyle : ChessUiStyle
{
    public enum DamageColor
    {
        Red,
        Green,
        Gray,
        Gold
    }
    public void UpdateStatus(ChessStatus chessStatus, FightCardData card)
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

    public virtual Tween RespondAnim(FightCardData target, RespondAct respond) => DOTween.Sequence();

    public virtual Tween ExecutionEffect(ExecuteAct act, FightCardData op, int skill = 0) =>
        DOTween.Sequence();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="respond"></param>
    /// <param name="dmgColor"></param>
    /// <param name="dmgType"></param>
    /// <param name="sparkId">-1 = 无闪花,0=普通闪花，正数等于兵种闪花</param>
    /// <param name="skill"></param>
    public virtual void RespondUpdate(FightCardData target, RespondAct respond, DamageColor dmgColor, Damage.Types dmgType, int sparkId, int skill) {}
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
        return Effect.GetFloorBuffId(PosSprite.GetKind(first.Element));
    }
}
public abstract class CardStyle : ChessmanStyle
{
    #region OldActivities
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
        if (target.ChessmanStyle.ArmedType == -3 &&
            target.ChessmanStyle.Military == 11 && 
            result.IsDeath) //金币宝箱
        {
            CardAnimator.instance.NumberEffectTween(target, $"金币+{target.Level}",
                CardAnimator.instance.Misc.TreasureChestTextColor,
                Damage.Types.Rouse, false);
            return;
        }
        switch (result.Type)
        {
            case ActivityResult.Types.Suffer:
                value = result.Status.LastSuffers.LastOrDefault();
                break;
            case ActivityResult.Types.EaseShield:
                value = result.Status.LastEaseShieldDamage;
                color = ColorDataStatic.name_gray;
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
        CardAnimator.instance.UpdateStateIcon(card);
    }

    private void SparkTween(Activity activity, FightCardData target, int effectId) => Spark(activity.Skill, target,
        effectId, activity.Conducts.Any(c => c.IsCriticalDamage() || c.IsRouseDamage()));

    /// <summary>
    /// 攻击方竖文字
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="offense"></param>
    /// <returns></returns>
    protected abstract void ActivityVText(Activity activity, FightCardData offense);
    #endregion

    public override Tween ExecutionEffect(ExecuteAct act, FightCardData op, int skill)
    {
        var tween = DOTween.Sequence();
        
        if(skill > 0)
        {
            //VText
            tween.AppendCallback(()=>ActivityVText(skill, op));
        }
        return tween;
    }
    public override Tween RespondAnim(FightCardData target, RespondAct respond)
    {
        var tween = DOTween.Sequence();
        switch (respond.Kind)
        {
            case RespondAct.Responds.Ease:
            case RespondAct.Responds.Suffer:
                tween.Join(CardAnimator.instance.SufferShakeAnimation(target));
                break;
            case RespondAct.Responds.Buffing:
            case RespondAct.Responds.Heal:
                tween.Join(CardAnimator.instance.AssistEnlargeAnimation(target));
                break;
            case RespondAct.Responds.Dodge:
                tween.Join(CardAnimator.instance.SideDodgeAnimation(target));
                break;
            case RespondAct.Responds.None:
            case RespondAct.Responds.Shield:
            case RespondAct.Responds.Kill:
            case RespondAct.Responds.Suicide:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        tween.AppendCallback(()=>
        {
            UpdateStatus(respond.Status, target);
            CardAnimator.instance.UpdateStateIcon(target); //更新状态(buff)
        }); //更新血量
        return tween;
    }

    public override void RespondUpdate(FightCardData target,RespondAct respond,DamageColor dmgColor,Damage.Types dmgType,int sparkId,int skill)
    {
        if (respond.Pop > 0)
        {
            var color = GetColor(dmgColor);
            CardAnimator.instance.NumberEffectTween(target, respond.Pop, color, dmgType);
        }
        Spark(skill, target, sparkId, dmgType != Damage.Types.General);
    }

    private Color GetColor(DamageColor color)
    {
        switch (color)
        {
            case DamageColor.Red:
                return ColorDataStatic.name_deepRed;
            case DamageColor.Green:
                return ColorDataStatic.huiFu_green;
            case DamageColor.Gray:
                return ColorDataStatic.name_gray;
            case DamageColor.Gold:
                return Color.yellow;
            default:
                throw new ArgumentOutOfRangeException(nameof(color), color, null);
        }
    }

    private void Spark(int skill, FightCardData target, int sparkId, bool enlarge)
    {
        if (sparkId < 0) return;
        GameObject effect;
        switch (skill)
        {
            case 0 :
                effect = EffectsPoolingControl.instance.GetSparkEffect(Effect.Basic001, target.cardObj.transform);
                effect.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360));
                break;
            default:
                effect = EffectsPoolingControl.instance.GetSparkEffect(sparkId, target.cardObj.transform);
                break;
            case -1: return;
        }

        //一些效果需要反向显示
        if (Effect.IsInvertControl(sparkId))
        {
            var rotation = target.isPlayerCard ? new Quaternion(0, 0, 180, 0) : Quaternion.identity;
            effect.transform.localRotation = rotation;
        }

        effect.transform.localScale = enlarge ? new Vector3(1.5f, 1.5f, 1) : Vector3.one;
    }

    protected abstract void ActivityVText(int skill, FightCardData offense);
}

public class HeroStyle : CardStyle
{
    protected override void ActivityVText(Activity activity, FightCardData offense)
    {
        if (activity.Skill <= 0) return;
        //武将有自己的攻击特效
        CardAnimator.instance.VTextEffect(Effect.HeroActivityVText(Military,activity.Skill), offense.cardObj.transform);
    }

    protected override void ActivityVText(int skill, FightCardData offense)
    {
        if (skill <= 0) return;
        //武将有自己的攻击特效
        CardAnimator.instance.VTextEffect(Effect.HeroActivityVText(Military, skill), offense.cardObj.transform);
    }

    //武将特效
    public override int GetMilitarySparkId(Activity activity) => Effect.GetHeroSparkId(Military, activity.Skill);
    public override int GetMilitarySparkId(int skill) => Effect.GetHeroSparkId(Military, skill);
}

public class TowerStyle : CardStyle
{
    public override int GetMilitarySparkId(Activity activity) => Effect.GetTowerSparkId(Military, activity.Skill);
    public override int GetMilitarySparkId(int skill) => Effect.GetTowerSparkId(Military, skill);
    protected override void ActivityVText(Activity activity, FightCardData offense) => CardAnimator.instance.VTextEffect(Effect.TowerActivityVText(Military, activity.Skill), offense.cardObj.transform);
    protected override void ActivityVText(int skill, FightCardData offense) => CardAnimator.instance.VTextEffect(Effect.TowerActivityVText(Military, skill), offense.cardObj.transform);
}

public class TrapStyle : CardStyle
{
    public override int GetMilitarySparkId(Activity activity) => Effect.GetTrapSparkId(Military, activity.Skill);
    public override int GetMilitarySparkId(int skill) => Effect.GetTrapSparkId(Military, skill);
    protected override void ActivityVText(Activity activity, FightCardData offense) => CardAnimator.instance.VTextEffect(Effect.TrapActivityVText(Military, activity.Skill), offense.cardObj.transform);
    protected override void ActivityVText(int skill, FightCardData offense) => CardAnimator.instance.VTextEffect(Effect.TrapActivityVText(Military, skill), offense.cardObj.transform);
}