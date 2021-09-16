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
    public virtual Sequence RespondTween(Activity activity, FightCardData target, string effectId = null) => DOTween.Sequence();

    public virtual string GetMilitarySparkId(Activity activity) => Effect.Basic0A;

}
/// <summary>
/// 棋盘执行的数据调用抽象层
/// </summary>
public class ChessmanStyle : ChessUiStyle
{
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
                switch (effectId)
                {
                    case Effect.LongSpear14A:
                    case Effect.Spear59A:
                        var rotation = target.isPlayerCard ? new Quaternion(0, 0, 180, 0) : Quaternion.identity;
                        effect.transform.localRotation = rotation;
                        break;
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
    public override string GetMilitarySparkId(Activity activity)
    {
        if (activity.Skill == 0) return Effect.Basic0A;
        var value = Effect.Basic0A; // "0A";
        switch (Military)
        {
            case 3: value = Effect.FeiJia3A; break; // "3A";飞甲
            case 1: //近战
            case 4: value = Effect.Shield4A; break; // "4A";大盾
            case 6: value = Effect.SuckBlood6A; break; // "6A";虎卫
            case 7: value = Effect.Blademail7A; break; // "7A";
            case 8: value = Effect.Elephant8A; break; // "8A";象兵
            case 9: value = Effect.Cavalry9A; break; // "9A";先锋
            case 10: value = Effect.Daredevil10A; break; // "10A";先登
            case 11: value = Effect.Knight11A; break; // "11A";白马
            case 12: value = Effect.Stimulate12A; break; // "12A";神武
            case 13: value = Effect.Guard13A; break; // "13A";禁卫
            case 15: value = Effect.Halberd15A; break; // "15A";大戟
            case 14: value = Effect.LongSpear14A; break;//"14A"长枪兵
            case 16: value = Effect.Cavalry16A; break; // "16A";骠骑
            case 17: value = Effect.Blade17A; break; // "17A";大刀
            case 18: value = Effect.Axe18A; break; // "18A";大斧
            case 19: value = Effect.CrossBow19A; break; // "19A";连弩
            case 20: value = Effect.Bow20A; break;// "20A";弓兵
            case 21: value = Effect.Warship21A; break; // "21A";战船
            case 22: value = Effect.Chariot22A; break; // "22A";战车
            case 23: value = Effect.SiegeMachine23A; break; // "23A";攻城车
            case 24: value = Effect.ThrowRocks24A; break; // "24A";投石车
            case 25: value = Effect.Assassin25A; break; // "25A";刺客
            case 26: value = Effect.Advisor26A; break;// "26A"  军师
            case 27: value = Effect.Advisor26A; break;// "27A"  大军师
            case 28: value = Effect.Warlock28A; break;// "28A"  术士
            case 29: value = Effect.Warlock29A; break;// "29A"  大术士
            case 30: value = Effect.PoisonMaster30A; break;// "30A"  毒士
            case 31: value = Effect.PoisonMaster31A; break;// "31A"  大毒士
            case 32: value = Effect.FlagBearer32A; break;// "32A"  统帅
            case 33: value = Effect.FlagBearer33A; break;// "33A"  大统帅
            case 34: value = Effect.Debate34A; break; // "34A";辩士
            case 35: value = Effect.Controversy35A; break; // "35A";大辩士
            case 36: value = Effect.Counselor36A; break;//"36"	谋士
            case 37: value = Effect.Counselor37A; break;//"37"  大谋士
            case 38: value = Effect.StateAffairs38A; break; // "38A";
            case 39: value = Effect.Support39A; break; // "39A";辅佐
            case 40: value = Effect.Mechanical40A; break; // "40A";器械
            case 42: value = Effect.Heal42A; break; // "42A";医师
            case 43: value = Effect.Heal43A; break; // "43A";大医师
            case 44: value = Effect.FemaleRider44A; break; // "44A";巾帼
            case 45: value = Effect.Lady45A; break;//"45"	美人
            case 46: value = Effect.Lady46A; break;//"46"  大美人
            case 47: value = Effect.Persuade47A; break; // "47A";说客
            case 48: value = Effect.Convince48A; break; // "48A";大说客
            case 49: value = Effect.Crossbow49A; break; // "49A";弩兵
            case 50: value = Effect.Scribe50A; break; // "50A";文士
            case 51: value = Effect.CrossBow51A; break;// "51A";强弩
            case 52: value = Effect.LongBow52A; break;// "52A";大弓
            case 53: value = Effect.Anchorite53A; break;//"53"	隐士
            case 54: value = Effect.Anchorite54A; break;//"54"  大隐士
            case 55:
                switch (activity.Skill)
                {
                    case 1:
                        value = Effect.FireShip55A;
                        break;
                    case 2:
                        value = Effect.FireShip55A0;
                        break;
                }
                break;
            case 56: value = Effect.Barbarians56A; break; // "56A";蛮族
            case 57: value = Effect.TengJia57A; break; // "57A";藤甲
            case 58: value = Effect.HeavyCavalry58A; break; // "58A";铁骑
            case 59: value = Effect.Spear59A; break;//"59A"枪兵
            case 60: value = Effect.Cavalry60A; break; // "60A";急先锋
            case 65:
                switch (activity.Skill)
                {
                    case 1:
                        value = Effect.YellowBand65A;
                        break; // "65A";黄巾
                    case 2:
                        value = Effect.YellowBand65B;
                        break; // "65B";黄巾
                }
                break;
        }
        return value;
    }
}

public class TowerStyle : CardStyle
{
    public override string GetMilitarySparkId(Activity activity)
    {
        switch (Military)
        {
            case 0: //营寨
            case 2: //奏乐台
                return Effect.Heal42A;
            case 6://轩辕台
            case 3://箭塔
                return Effect.Bow20A;
            case 1: //投石台
            default:
                return Effect.Basic0A;
        }
    }

    protected override Tween OffenseVText(Activity activity, FightCardData offense) => DOTween.Sequence();
}

public class TrapStyle : CardStyle
{
    protected override Tween OffenseVText(Activity activity, FightCardData offense) => DOTween.Sequence();
}