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

    public virtual Sequence Respond(Activity activity, FightCardData target, string effectId = null) => DOTween.Sequence();
    protected virtual string GetMilitaryEffectId(Activity activity) => Effect.Basic0A;

}
/// <summary>
/// 棋盘执行的数据调用抽象层
/// </summary>
public class ChessmanStyle : ChessUiStyle
{
    /// <summary>
    /// 前摇动画，包括攻击动作
    /// </summary>
    /// <param name="card"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public virtual Tween PreActionTween(FightCardData card,FightCardData target) => DOTween.Sequence();
    /// <summary>
    /// 效果演示
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="offense"></param>
    /// <param name="target"></param>
    /// <param name="chessboard"></param>
    /// <returns></returns>
    public virtual Tween ActivityEffectTween(Activity activity, FightCardData offense, FightCardData target, Transform chessboard) => DOTween.Sequence();
    /// <summary>
    /// 后摇动作
    /// </summary>
    /// <param name="card"></param>
    /// <param name="origin"></param>
    /// <returns></returns>
    public virtual Tween FinalizeActionTween(FightCardData card, Vector3 origin) => DOTween.Sequence();

    public virtual Tween CounterTween(Activity activity, FightCardData offense, FightCardData target) => 
        DOTween.Sequence().Join(CardAnimator.Counter(offense))
        .Append(target.ChessmanStyle.Respond(activity, target, GetMilitaryEffectId(activity)));

}
public class SpriteStyle : CombatStyle
{
    public Sequence Activity(Activity activity, FightCardData target) => DOTween.Sequence().Join(target.ChessmanStyle.Respond(activity, target)).AppendCallback(() => CardAnimator.UpdateStatusEffect(target));
}
public abstract class CardStyle : ChessmanStyle
{
    public override Tween PreActionTween(FightCardData card, FightCardData target)
    {
        var tween = DOTween.Sequence();
        switch (Type)
        {
            case Types.None:
                break;
            case Types.Melee:
                tween.Join(CardAnimator.MeleeMoving(card, target));
                break;
            case Types.Range:
                tween.Join(CardAnimator.RangePreAction(card));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return tween;
    }

    public override Tween ActivityEffectTween(Activity activity,FightCardData offense,FightCardData target,Transform chessboard)
    {
        return DOTween.Sequence()
            .Join(OffenseEffects(activity, offense, target, chessboard))
            .Join(target.ChessmanStyle.Respond(activity, target, GetMilitaryEffectId(activity)));
    }

    public override Sequence Respond(Activity activity, FightCardData target, string effectId = null) => 
        base.Respond(activity, target, effectId)
            .AppendCallback(() => target.UpdateActivity(activity.Result.Status))
            .Append(RespondEffects(activity, target, effectId))
            .Join(CardAnimator.UpdateStatusEffect(target));

    protected virtual Tween RespondEffects(Activity activity, FightCardData target, string effectId)
    {
        var tween = DOTween.Sequence();
        var conducts = activity.Conducts
            .Where(c => c.Kind == CombatConduct.DamageKind ||
                        c.Kind == CombatConduct.KillingKind ||
                        c.Kind == CombatConduct.HealKind ||
                        c.Kind == CombatConduct.BuffKind
            ).ToArray();
        if (conducts.Length == 0) return tween;

        //技能效果
        tween.AppendCallback(() =>
        {
            if(effectId != null)//如果id==null，没有效果(火花)
            {
                foreach (var conduct in activity.Conducts)
                {
                    GameObject effect;
                    if (activity.Skill == 0)
                    {
                        effect = EffectsPoolingControl.instance.GetEffect(Effect.Basic0A, target.cardObj.transform,
                            0.5f);
                        effect.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360));
                    }
                    else
                        effect = EffectsPoolingControl.instance.GetEffect(effectId, target.cardObj.transform, 1f);

                    if (conduct.Critical > 0 || conduct.Rouse > 0) //如果会心或暴击 物体变大1.5
                        effect.transform.localScale = new Vector3(1.5f, 1.5f, 1);
                    else effect.transform.localScale = Vector3.one;
                }
            }
        });

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
                            tween.Join(CardAnimator.EffectIconTween(target, Effect.DropBlood, -(int)conduct.Total,
                                ColorDataStatic.name_deepRed));
                            break;
                        case CombatConduct.HealKind:
                            tween.Join(CardAnimator.EffectIconTween(target, effectId, (int)conduct.Total,
                                ColorDataStatic.huiFu_green));
                            break;
                    }

                    break;
                case ActivityResult.Types.Dodge:
                    tween.Join(CardAnimator.SideDodge(target));
                    break;
                case ActivityResult.Types.Shield:
                    tween.Join(CardAnimator.UpdateStatusEffect(target, CardState.Cons.Shield));
                    break;
                case ActivityResult.Types.Invincible:
                    tween.Join(CardAnimator.UpdateStatusEffect(target, CardState.Cons.Invincible));
                    break;
                case ActivityResult.Types.EaseShield:
                    tween.Join(CardAnimator.EffectIconTween(target, Effect.DropBlood, -(int)conduct.Total,
                            ColorDataStatic.name_gray))
                        .Join(CardAnimator.UpdateStatusEffect(target, CardState.Cons.EaseShield));
                    break;
                default:
                    break;
            }
        }
        return tween;
    }

    protected abstract Tween OffenseEffects(Activity activity, FightCardData offense, FightCardData target, Transform chessboard);

    public override Tween FinalizeActionTween(FightCardData card,Vector3 origin)
    {
        var tween = DOTween.Sequence();
        switch (Type)
        {
            case Types.None:
                break;
            case Types.Melee:
            case Types.Range:
                 tween.Join(CardAnimator.FinalizeAction(card, origin));
                 break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return tween;
    }
}

public class HeroStyle : CardStyle
{
    protected override Tween OffenseEffects(Activity activity, FightCardData offense, FightCardData target, Transform chessboard)
    {
        if (activity.Skill == 0) return DOTween.Sequence();
        //武将有自己的攻击特效
        return HeroOffenseVText(activity, offense, target);
    }

    protected virtual Tween HeroOffenseVText(Activity activity, FightCardData offense, FightCardData target) => CardAnimator.VTextEffect(MilitaryOffenseTextId(activity), offense.cardObj.transform);

    protected override Tween RespondEffects(Activity activity, FightCardData target, string effectId)
    {
        return DOTween.Sequence().Join(base.RespondEffects(activity, target, effectId));
    }

    private string MilitaryOffenseTextId(Activity activity)
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

    protected override string GetMilitaryEffectId(Activity activity)
    {
        if (activity.Skill == 0) return Effect.Basic0A;
        var value = Effect.Basic0A; // "0A";
        switch (Military)
        {
            case 3: value = Effect.FeiJia3A; break; // "3A";飞甲
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
    protected override string GetMilitaryEffectId(Activity activity)
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

    protected override Tween OffenseEffects(Activity activity, FightCardData offense, FightCardData target,
        Transform chessboard) => DOTween.Sequence();
}

public class TrapStyle : CardStyle
{
    protected override Tween OffenseEffects(Activity activity, FightCardData offense, FightCardData target,
        Transform chessboard) => DOTween.Sequence();
}