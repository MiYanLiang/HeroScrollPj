using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
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

    //近战移动方式
    public static Tween MeleeMoving(FightCardData card, FightCardData target)
    {
        var ui = card.cardObj;
        var size = GetWorldSize(target.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? 1 : -1) * size.y;
        var targetPos = target.cardObj.transform.position;
        var facingPos = new Vector3(targetPos.x, targetPos.y - oneYOffset, targetPos.z);
        return DOTween.Sequence().Append(ui.transform.DOMove(facingPos, Move * forward)).AppendInterval(charge);
    }

    public static Tween MeleeActivity(FightCardData card, FightCardData target) => StepBackAndHit(card, target);

    public static Tween Counter(FightCardData card, FightCardData target) =>
        StepBackAndHit(card, target, 0.3f, 0.3f, 0.1f);

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

    public static Tween FinalizeAction(FightCardData card,Vector3 origin)
    {
        var sBack = card.cardObj.transform.DOMove(origin, Back);
        var scaleBack = card.cardObj.transform.DOScale(Vector3.one, Back);
        return DOTween.Sequence().Join(sBack).Join(scaleBack);
    }

    public static Tween SufferAction(FightCardData target,Activity activity,CombatConduct conduct, Chessboard chessboard)
    {
        //闪避
        //伤害 - 无敌，盾，防护盾，扣血
        if (conduct.Kind == CombatConduct.DamageKind)
        {
            if (activity.Result.Type == ActivityResult.Types.Suffer)
            {
                var effect =
                    EffectsPoolingControl.instance.GetEffectToFight(Effect.DropBlood, 1.5f, target.cardObj.transform);
                effect.GetComponentInChildren<Text>().text = "-" + Mathf.Abs(conduct.Total);
                effect.GetComponentInChildren<Text>().color = Color.red;
                if (!activity.IsRePos)
                    return DOTween.Sequence().Join(target.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10))).Join(ChessboardConduct(conduct, chessboard));

                return ChessboardConduct(conduct, chessboard);
            }

            //if (activity.Result.Result == ActivityResult.Shield)
            //{
            //    yield return GenerateShieldEffect(target);
            //}
        }
        if (conduct.Kind == CombatConduct.HealKind)
            return GenerateHealEffect(target, conduct);
        return null;
    }

    public static Tween OnRePos(FightCardData target, ChessPos pos) =>
        target.cardObj.transform.DOMove(pos.transform.position,
            0.2f);

    private static Tween GenerateHealEffect(FightCardData target, CombatConduct conduct)
    {
        var effect =
            EffectsPoolingControl.instance.GetEffectToFight(Effect.DropBlood, 1.5f, target.cardObj.transform);
        effect.GetComponentInChildren<Text>().text = "+" + Mathf.Abs(conduct.Total);
        effect.GetComponentInChildren<Text>().color = Color.green;
        return target.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10));

    }


    static float chessboardShakeIntensity = 30f;
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

    public static Tween GetCombatStrikeEffect(AttackStyle style, CombatConduct conduct, FightCardData target)
    {
        var effectName = GetEffectByStyle(style);
        GameObject effectObj;
        if (effectName == null)
        {
            effectObj = EffectsPoolingControl.instance.GetEffectToFight(Effect.BasicAttack, 0.5f, target.cardObj);
            effectObj.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360));
        }
        else
            effectObj = EffectsPoolingControl.instance.GetEffectToFight(effectName, 1f, target.cardObj);

        if (conduct.Critical > 0 || conduct.Rouse > 0) //如果会心或暴击 物体变大1.5
            return effectObj.transform.DOScale(new Vector3(1.5f,1.5f,1),0.1f);
        return effectObj.transform.DOScale(Vector3.one, 0.1f);
    }

    private static string GetEffectByStyle(AttackStyle style)
    {
        if (style.ArmedType >= 0)
            return GetHeroEffectByStyle(style);
        else if (style.ArmedType == -1)
            return GetTowerEffectByStyle(style);
        else if (style.ArmedType == -2)
            return GetTrapEffectByStyle(style);
        throw new ArgumentOutOfRangeException($"Arg = {style.ArmedType}");
    }

    private static string GetHeroEffectByStyle(AttackStyle style)
    {
        switch (style.Military)
        {
            default: return Effect.BasicAttack; // "0A";
            case 3: return Effect.FeiJia; // "3A";飞甲
            case 4: return Effect.Shield; // "4A";大盾
            case 7: return Effect.ReflectDamage; // "7A";
            case 6: return Effect.SuckBlood; // "6A";虎卫
            case 8: return Effect.ElephantAttack; // "8A";象兵
            case 9: return Effect.CavalryCharge; // "9A";先锋
            case 10: return Effect.Daredevil; // "10A";先登
            case 11: return Effect.KnightAttack; // "11A";白马
            case 12: return Effect.Stimulate; // "12A";神武
            case 13: return Effect.GuardCounter; // "13A";禁卫
            case 15: return Effect.HalberdSweep; // "15A";大戟
            case 16: return Effect.CavalryGallop; // "16A";骠骑
            case 17: return Effect.BladeCombo; // "17A";大刀
            case 18: return Effect.AxeStrike; // "18A";大斧
            case 19:  // "19A";连弩
            case 51: return Effect.CrossBowCombo; // "19A";强弩
            case 20: // "20A";弓兵
            case 52: return Effect.LongBow; // "20A";大弓
            case 21: return Effect.WarshipAttack; // "21A";战船
            case 22: return Effect.ChariotAttack; // "22A";战车
            case 23: return Effect.SiegeMachine; // "23A";攻城车
            case 24: return Effect.ThrowRocks; // "24A";投石车
            case 25: return Effect.AssassinStrike; // "25A";刺客
            case 34: return Effect.Debate; // "34A";辩士
            case 35: return Effect.Controversy; // "35A";大辩士
            case 38: return Effect.StateAffairs; // "38A";
            case 39: return Effect.Support; // "39A";辅佐
            case 40: return Effect.Mechanical; // "40A";器械
            case 42: return Effect.Heal; // "42A";医师
            case 43: return Effect.Cure; // "43A";大医师
            case 44: return Effect.DisarmAttack; // "44A";巾帼
            case 47: return Effect.Persuade; // "47A";说客
            case 48: return Effect.Convince; // "48A";大说客
            case 49: return Effect.Crossbow; // "49A";弩兵
            case 50: return Effect.MagicStrike; // "50A";文士
            case 55: return Effect.FireShipExplode; // "55A0";火船
            case 551: return Effect.FireShipAttack; // "55A";火船
            case 56: return Effect.Barbarians; // "56A";蛮族
            case 57: return Effect.TengJiaAttack; // "57A";藤甲
            case 58: return Effect.HeavyCavalry; // "58A";铁骑
            case 60: return Effect.CavalryAssault; // "60A";急先锋
            case 65: return Effect.YellowBand; // "65A";黄巾
            case 651: return Effect.YellowBandB; // "65B";黄巾
        }
    }

    private static string GetTrapEffectByStyle(AttackStyle style)
    {
        switch (style.Military)
        {
                default : return Effect.BasicAttack ; // "0A";
                case 0: return Effect.ReflectDamage; // "7A";拒马
                case 1: return Effect.Explode; // "201A";地雷
                case 9: return Effect.Dropping; // "209A";滚石
                case 10: return Effect.Dropping ; // "209A";滚木
        }
    }
    private static string GetTowerEffectByStyle(AttackStyle style)
    {
        switch (style.Military)
        {
            default: return Effect.BasicAttack; // "0A";
            case 3: return Effect.LongBow; // "20A";箭楼
            case 6: return Effect.Shield; // "4A";轩辕台
            case 0: //营寨
            case 2: return Effect.Heal; // "42A";奏乐台
        }
    }
}