using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CardAnimator
{
    static float RangeHeroPreActionBegin = 0.5f;
    static float RangeHeroPreActionFinalize = 0.5f;
    static float yuanChengShakeTime = 0.1f;

    //攻击行动方式0-适用于-主动塔,远程兵
    public static Tween RangeActivity(FightCardData card)
    {
        var obj = card.cardObj;
        return obj.transform.DOScale(new Vector3(1.15f, 1.15f, 1), RangeHeroPreActionBegin).SetAutoKill(false).OnComplete(() => obj.transform.DOPlayBackwards());
    }

    public static float Move = 0.534f;
    public static float Back = 0.167f;
    public static float forward = 0.2f; //去
    public static float stepBack = 0.3f; //回
    public static float charge = 0.4f; //等
    public static float hit = 0.1f; //去
    public static float posFloat = 0;
    public static float oneDisY = 1;

    /// <summary>
    /// 近战移动方式
    /// </summary>
    public static Tween MeleeMoving(FightCardData card, FightCardData target)
    {
        var ui = card.cardObj;
        var size = GetWorldSize(target.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? 1 : -1) * size.y;
        var targetPos = target.cardObj.transform.position;
        var facingPos = new Vector3(targetPos.x, targetPos.y - oneYOffset, targetPos.z);
        return DOTween.Sequence().Append(ui.transform.DOMove(facingPos, Move * forward)).AppendInterval(charge);
    }
    /// <summary>
    /// 近战行动
    /// </summary>
    public static Tween MeleeActivity(FightCardData card, FightCardData target) => StepBackAndHit(card, target);
    /// <summary>
    /// 反击动作
    /// </summary>
    public static Tween Counter(FightCardData card, FightCardData target)
    {
        //这里反击所有兵种都显示文字效果。并不仅限于禁卫
        return DOTween.Sequence()
            .OnComplete(() => GetVTextEffect(13.ToString(), card.cardObj.transform))
            .Join(StepBackAndHit(card, target, 0.3f, 0.3f, 0.1f));
    }

    //退后向前进攻模式
    private static Tween StepBackAndHit(FightCardData card, FightCardData target,float backStepDistance = 0.5f,float time = 1f,float chargeRate = 1f)
    {
        var ui = card.cardObj;
        var size = GetWorldSize(target.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? -1 : 1) * size.y;
        var origin = ui.transform.position;
        return DOTween.Sequence().Append(ui.transform
            .DOMove(origin + new Vector3(0, oneYOffset * backStepDistance), stepBack * time))
            .AppendInterval(charge * chargeRate)
            .Append(ui.transform.DOMove(origin, hit * time));
    }

    /// <summary>
    /// 闪避动作
    /// </summary>
    public static Tween SideDodge(FightCardData target,float distance = 0.2f,float time = 0.2f)
    {
        var ui = target.cardObj;
        var size = GetWorldSize(target.cardObj.transform);
        var origin = ui.transform.position;
        return DOTween.Sequence().Append(ui.transform
                .DOMove(origin + new Vector3(size.x * distance, 0, 0), time))
            .Append(ui.transform.DOMove(origin, time * 0.5f));
    }

    /// <summary>
    /// 结束动作(卡牌大小归位)
    /// </summary>
    public static Tween FinalizeAction(FightCardData card,Vector3 origin)
    {
        var sBack = card.cardObj.transform.DOMove(origin, Back);
        var scaleBack = card.cardObj.transform.DOScale(Vector3.one, Back);
        return DOTween.Sequence().Join(sBack).Join(scaleBack);
    }

    /// <summary>
    /// 承受动作
    /// </summary>
    public static Tween SufferingAttack(FightCardData target, Activity activity, CombatConduct conduct,
        Chessboard chessboard)
    {
        var tween = DOTween.Sequence();
        var effect =
            EffectsPoolingControl.instance.GetEffectToFight(Effect.DropBlood, 1.5f, target.cardObj.transform);
        effect.GetComponentInChildren<Text>().text = "-" + Mathf.Abs(conduct.Total);
        effect.GetComponentInChildren<Text>().color = Color.red;
        if (!activity.IsRePos)
            return tween.Join(target.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10)))
                .Join(ChessboardConduct(conduct, chessboard));

        return ChessboardConduct(conduct, chessboard);
    }

    /// <summary>
    /// 被击退一格
    /// </summary>
    public static Tween OnRePos(FightCardData target, ChessPos pos) =>
        target.cardObj.transform.DOMove(pos.transform.position,
            0.2f);

    /// <summary>
    /// 补血效果
    /// </summary>
    public static Tween GenerateHealEffect(FightCardData target, CombatConduct conduct)
    {
        var effect =
            EffectsPoolingControl.instance.GetEffectToFight(Effect.DropBlood, 1.5f, target.cardObj.transform);
        effect.GetComponentInChildren<Text>().text = "+" + Mathf.Abs(conduct.Total);
        effect.GetComponentInChildren<Text>().color = Color.green;
        return target.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10));
    }

    static float chessboardShakeIntensity = 30f;
    /// <summary>
    /// 棋盘震动
    /// </summary>
    public static Tween ChessboardConduct(CombatConduct conduct,Chessboard chessboard)
    {
        var transform = chessboard.transform;
        var origin = transform.position;
        if (conduct.Kind == CombatConduct.DamageKind)
        {
            if (conduct.Rouse > 0 || conduct.Critical > 0)
                return transform.DOShakePosition(0.25f, chessboardShakeIntensity);
        }
        return transform.DOMove(origin, 0);
    }

    private static Vector2 GetWorldSize(Transform transform) =>
        ((RectTransform) transform).sizeDelta * transform.lossyScale;

    /// <summary>
    /// 打击效果
    /// </summary>
    public static Tween GetCombatStrikeEffect(Activity activity, IChessman op, FightCardData target)
    {
        return DOTween.Sequence().AppendInterval(0.01f).OnComplete(() =>
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
                    effect = EffectsPoolingControl.instance.GetEffect(GetEffectByStyle(op.Style, conduct), target.cardObj.transform, 1f);

                if (conduct.Critical > 0 || conduct.Rouse > 0) //如果会心或暴击 物体变大1.5
                    effect.transform.localScale = new Vector3(1.5f, 1.5f, 1);
                else effect.transform.localScale = Vector3.one;
            }
        });
    }

    /// <summary>
    /// 更新状态效果
    /// </summary>
    public static Tween UpdateStateEffect(FightCardData target,FightState.Cons con)
    {
        return DOTween.Sequence().AppendInterval(0.01f).OnComplete(() =>
        {
            var key = (int) con;
            var status = target.fightState.Data;
            var stateValue = status.ContainsKey(key) ? status[key] : 0;
            if (stateValue < 0)
            {
                if (target.States.ContainsKey(key))
                {
                    var e = target.States[key];
                    target.States.Remove(key);
                    EffectsPoolingControl.instance.TakeBackStateIcon(e);
                }

                return;
            }

            EffectStateUi effect;
            if (!target.States.ContainsKey(key))
            {
                effect = EffectsPoolingControl.instance.GetStateIconToFight(FightState.StateIconName(con),
                    target.cardObj.transform);
                target.States.Add(key, effect);
            }
            else
                effect = target.States[key];

            if (con == FightState.Cons.ExtendedHp)
            {
                var fade = Math.Max(0.3f, 1f * stateValue / DataTable.GetGameValue(119));
                effect.Image.color = new Color(1, 1, 1, fade);
            }
        });
    }

    private static string GetEffectByStyle(AttackStyle style,CombatConduct conduct)
    {
        if (style.ArmedType >= 0)
            return GetHeroEffectByStyle(style);
        if (style.ArmedType == -1)
            return GetTowerEffectByStyle(style);
        if (style.ArmedType == -2)
            return GetTrapEffectByStyle(style);
        throw new ArgumentOutOfRangeException($"Arg = {style.ArmedType}");
    }

    private static string GetHeroEffectByStyle(AttackStyle style)
    {
        var value = Effect.Basic0A; // "0A";
        switch (style.Military)
        {
            case 3: value = Effect.FeiJia3A;break; // "3A";飞甲
            case 4: value = Effect.Shield4A;break; // "4A";大盾
            case 6: value = Effect.SuckBlood6A;break; // "6A";虎卫
            case 7: value = Effect.Blademail7A;break; // "7A";
            case 8: value = Effect.Elephant8A;break; // "8A";象兵
            case 9: value = Effect.Cavalry9A;break; // "9A";先锋
            case 10: value = Effect.Daredevil10A;break; // "10A";先登
            case 11: value = Effect.Knight11A;break; // "11A";白马
            case 12: value = Effect.Stimulate12A;break; // "12A";神武
            case 13: value = Effect.Guard13A;break; // "13A";禁卫
            case 15: value = Effect.Halberd15A;break; // "15A";大戟
            case 14: value = Effect.LongSpear14A;break;//"14A"长枪兵
            case 16: value = Effect.Cavalry16A;break; // "16A";骠骑
            case 17: value = Effect.Blade17A;break; // "17A";大刀
            case 18: value = Effect.Axe18A;break; // "18A";大斧
            case 19: value = Effect.CrossBow19A;break; // "19A";连弩
            case 20: value = Effect.Bow20A;break;// "20A";弓兵
            case 21: value = Effect.Warship21A;break; // "21A";战船
            case 22: value = Effect.Chariot22A;break; // "22A";战车
            case 23: value = Effect.SiegeMachine23A;break; // "23A";攻城车
            case 24: value = Effect.ThrowRocks24A;break; // "24A";投石车
            case 25: value = Effect.Assassin25A;break; // "25A";刺客
            case 26: value = Effect.Advisor26A;break;// "26A"  军师
            case 27: value = Effect.Advisor26A;break;// "27A"  大军师
            case 28: value = Effect.Warlock28A;break;// "28A"  术士
            case 29: value = Effect.Warlock29A;break;// "29A"  大术士
            case 30: value = Effect.PoisonMaster30A;break;// "30A"  毒士
            case 31: value = Effect.PoisonMaster31A;break;// "31A"  大毒士
            case 32: value = Effect.FlagBearer32A;break;// "32A"  统帅
            case 33: value = Effect.FlagBearer33A;break;// "33A"  大统帅
            case 34: value = Effect.Debate34A;break; // "34A";辩士
            case 35: value = Effect.Controversy35A;break; // "35A";大辩士
            case 36: value = Effect.Counselor36A;break;//"36"	谋士
            case 37: value = Effect.Counselor37A;break;//"37"  大谋士
            case 38: value = Effect.StateAffairs38A;break; // "38A";
            case 39: value = Effect.Support39A;break; // "39A";辅佐
            case 40: value = Effect.Mechanical40A;break; // "40A";器械
            case 42: value = Effect.Doctor42A;break; // "42A";医师
            case 43: value = Effect.Doctor43A;break; // "43A";大医师
            case 44: value = Effect.FemaleRider44A;break; // "44A";巾帼
            case 45: value = Effect.Lady45A;break;//"45"	美人
            case 46: value = Effect.Lady46A;break;//"46"  大美人
            case 47: value = Effect.Persuade47A;break; // "47A";说客
            case 48: value = Effect.Convince48A;break; // "48A";大说客
            case 49: value = Effect.Crossbow49A;break; // "49A";弩兵
            case 50: value = Effect.Scribe50A;break; // "50A";文士
            case 51: value = Effect.CrossBow51A;break;// "51A";强弩
            case 52: value = Effect.LongBow52A;break;// "52A";大弓
            case 53: value = Effect.Anchorite53A;break;//"53"	隐士
            case 54: value = Effect.Anchorite54A;break;//"54"  大隐士
            case 55: value = Effect.FireShip55A0;break; // "55A0";火船
            case 551: value = Effect.FireShip55A;break; // "55A";火船
            case 56: value = Effect.Barbarians56A;break; // "56A";蛮族
            case 57: value = Effect.TengJia57A;break; // "57A";藤甲
            case 58: value = Effect.HeavyCavalry58A;break; // "58A";铁骑
            case 59: value = Effect.Spear59A;break;//"59A"枪兵
            case 60: value = Effect.Cavalry60A;break; // "60A";急先锋
            case 65: value = Effect.YellowBand65A;break; // "65A";黄巾
            case 651: value = Effect.YellowBand65B;break; // "65B";黄巾
        }

        return value;
    }

    private static string GetTrapEffectByStyle(AttackStyle style)
    {
        switch (style.Military)
        {
                default : return Effect.Basic0A ; // "0A";
                case 0: return Effect.Blademail7A;break; // "7A";拒马
                case 1: return Effect.Explode; // "201A";地雷
                case 9: return Effect.Dropping; // "209A";滚石
                case 10: return Effect.Dropping ; // "209A";滚木
        }
    }
    private static string GetTowerEffectByStyle(AttackStyle style)
    {
        switch (style.Military)
        {
            default: return Effect.Basic0A;break; // "0A";
            case 3: return Effect.Bow20A;break; // "20A";箭楼
            case 6: return Effect.Shield4A;break; // "4A";轩辕台
            case 0: //营寨
            case 2: return Effect.Doctor42A;break; // "42A";奏乐台
        }
    }

    public static Tween DisplayTextEffect(FightCardData target, Activity activity)
    {
        var tween = DOTween.Sequence();
        if (activity.IsRePos)
            tween.OnComplete(() => GetHTextEffect(17, target.cardObj.transform, Color.red));
        switch (activity.Result.Type)
        {
            case ActivityResult.Types.Suffer:
                break;
            case ActivityResult.Types.Friendly:
                break;
            case ActivityResult.Types.Dodge:
            case ActivityResult.Types.Shield:
            case ActivityResult.Types.Invincible:
            case ActivityResult.Types.ExtendedShield:
                tween.OnComplete(()=>TextEffectByResult(activity.Result.Type,target));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        foreach (var conduct in activity.Conducts)
        {
            var color = Color.red;
            var tableId = -1;
            switch (conduct.Kind)
            {
                case CombatConduct.BuffKind:
                    if (conduct.Total > 0)
                    {
                        switch ((FightState.Cons) conduct.Element)
                        {
                            case FightState.Cons.Bleed:
                                tableId = 16;
                                break;
                            case FightState.Cons.Poison:
                                tableId = 12;
                                break;
                            case FightState.Cons.Burn:
                                tableId = 20;
                                break;
                            case FightState.Cons.Imprisoned:
                                tableId = 11;
                                break;
                            case FightState.Cons.Cowardly:
                                tableId = 21;
                                break;
                            case FightState.Cons.ExtendedHp:
                            case FightState.Cons.Disarmed:
                            case FightState.Cons.Shield:
                            case FightState.Cons.Invincible:
                            case FightState.Cons.Stimulate:
                            case FightState.Cons.ZhanGuTaiAddOn:
                            case FightState.Cons.FengShenTaiAddOn:
                            case FightState.Cons.PiLiTaiAddOn:
                            case FightState.Cons.LangYaTaiAddOn:
                            case FightState.Cons.FengHuoTaiAddOn:
                            case FightState.Cons.DeathFight:
                            case FightState.Cons.Stunned:
                            case FightState.Cons.ShenZhu:
                            case FightState.Cons.Neizhu:
                            case FightState.Cons.MiWuZhenAddOn:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else tableId = 14;//如果buff值是负数，判定为镇定效果(移除buff)
                    break;
                case CombatConduct.KillingKind:
                    tableId = 13;
                    break;
                case CombatConduct.HealKind:
                    color = ColorDataStatic.huiFu_green;
                    tableId = 15;
                    break;
            }

            if (tableId >= 0)
                tween.OnComplete(() => GetHTextEffect(tableId, target.cardObj.transform, color));
            if (conduct.Critical > 0)
                tween.OnComplete(() => GetHTextEffect(23, target.cardObj.transform, color));
            if (conduct.Rouse > 0)
                tween.OnComplete(() => GetHTextEffect(22, target.cardObj.transform, color));
        }

        return tween;
    }

    private static void TextEffectByResult(ActivityResult.Types type, FightCardData target)
    {
        var effectName = string.Empty;
        switch (type)
        {
            case ActivityResult.Types.Suffer:
            case ActivityResult.Types.Friendly:
                break;
            case ActivityResult.Types.Dodge:
                effectName = DataTable.GetStringText(19);
                break;
            case ActivityResult.Types.Shield:
            case ActivityResult.Types.Invincible:
                case ActivityResult.Types.ExtendedShield:
                effectName = DataTable.GetStringText(18);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        GetVTextEffect(effectName, target.cardObj.transform);
    }

    private static void GetVTextEffect(string effectName,Transform transform)
    {
        var effectObj = EffectsPoolingControl.instance.GetEffectToFight(Effect.SpellTextV, 1.5f, transform);
        effectObj.GetComponentsInChildren<Image>()[1].sprite =
            Resources.Load("Image/battle/" + effectName, typeof(Sprite)) as Sprite;
    }
    private static void GetHTextEffect(int id,Transform transform, Color color)
    {
        var effectObj = EffectsPoolingControl.instance.GetEffectToFight(Effect.SpellTextH, 1.5f, transform);
        effectObj.GetComponentInChildren<Text>().text = DataTable.GetStringText(id);
        effectObj.GetComponentInChildren<Text>().color = color;
    }

    public static Tween SkillEffect(Activity activity,FightCardData op, bool isOffense, IChessman target)
    {
        var tween = DOTween.Sequence();
        if (activity.Skill == 0) return tween;
        var value = string.Empty;
        var validEffect = isOffense;
        var style = op.Style;
        var targetStyle = target.Style;
        switch (style.Military)
        {
            case 17: //刀兵
                if (!target.Status.IsDeath)//只有死亡才会有连斩效果
                    return tween;
                break;
            case 13: //禁卫
                //validEffect = !isOffense;//禁卫只有防守才有效果
                return tween;//暂时只要是反击，在Counter代码里都会打出效果，不仅于禁卫
            case 12: //神武
                if (targetStyle.ArmedType>=0) //非英雄没技能表现
                    return tween;
                validEffect = true;//无论进攻或防守都有效果
                break;
            case 58: //铁骑
                validEffect = true;
                //攻击与防守显示文字不同
                value = isOffense ? style.Military.ToString() : "58_0";
                break;
            case 55: //火船
                value = op.Status.HpRate * 100 < DataTable.GetGameValue(54) ? 
                    "55_0" : //火船爆炸
                    style.Military.ToString();
                break;
            case 41: //敢死
                validEffect = !isOffense;
                if (op.Status.GetBuff(FightState.Cons.DeathFight) <= 0 &&
                    op.Status.HpRate * 100 < DataTable.GetGameValue(103))
                    break;
                return tween;
            case 10: //死士
                if (op.Status.HpRate * 100 > DataTable.GetGameValue(100))
                    return tween;
                break;
            case 7: //刺甲
                validEffect = !isOffense;//防守才有效果
                if (targetStyle.ArmedType < 0 || targetStyle.CombatStyle == AttackStyle.CombatStyles.Range)
                    return tween;
                break;
            case 19: //弩兵
            case 51: //强弩
                switch (activity.Skill)
                {
                    default:
                        value = 19.ToString();
                        break;
                    case 2:
                        value = 51.ToString();
                        break;
                }
                break;
            case 56: //蛮族
            case 44: //卸甲
            case 34: //辩士
            case 35: //辩士
            case 47: //说客
            case 48: //说客
            case 53: //隐士
            case 54: //隐士
            case 39: //辅佐
            case 42: //医生
            case 43: //医生
            case 45: //美人
            case 46: //美人
            case 36: //谋士
            case 37: //谋士
            case 30: //毒士
            case 31: //毒士
            case 25: //刺客
            case 21: //战船
            case 22: //战车
                if (targetStyle.ArmedType < 0) //非英雄没技能表现
                    return tween;
                value = style.Military.ToString();
                break;
            case 23: //攻城车
            case 40: //器械
                if (targetStyle.ArmedType >= 0)
                    return tween;
                break;
            case 65: //黄巾
            case 4: //大盾
            case 32: //统帅
            case 33: //统帅
            case 9: //先锋
            case 16: //骑兵
            case 26: //军师
            case 27: //军师
            case 24: //投石车
            case 20: //弓兵
            case 52: //弓兵
            case 38: //内政
            case 18: //斧兵
            case 15: //戟兵
            case 14: //枪兵
            case 59: //枪兵
            case 11: //白马
            case 8: //象兵
            case 6: //虎卫
                value = style.Military.ToString();
                break;
        }

        if (value == string.Empty || !validEffect) return tween;
        tween.OnComplete(() => GetVTextEffect(value, op.cardObj.transform));
        return tween;
    }
}