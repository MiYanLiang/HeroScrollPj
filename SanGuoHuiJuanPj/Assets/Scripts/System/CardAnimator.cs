using System;
using System.Linq;
using Assets.System.WarModule;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Monetization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CardAnimator
{
    static float RangeHeroPreActionBegin = 0.5f;
    static float RangeHeroPreActionFinalize = 0.5f;
    static float yuanChengShakeTime = 0.1f;

    //攻击行动方式0-适用于-主动塔,远程兵
    public static Tween RangePreAction(FightCardData card)
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
    public static Tween Counter(FightCardData card)
    {
        //这里反击所有兵种都显示文字效果。并不仅限于禁卫
        return DOTween.Sequence()
            .Join(StepBackAndHit(card, 0.3f, 0.3f, 0.1f))
            .OnComplete(() => GetVTextEffect(13.ToString(), card.cardObj.transform));
    }

    //退后向前进攻模式
    public static Tween StepBackAndHit(FightCardData card, float backStepDistance = 0.5f,float time = 1f,float chargeRate = 1f)
    {
        var ui = card.cardObj;
        var size = GetWorldSize(card.cardObj.transform);
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

    public static Tween EffectIconTween(FightCardData target, string iconString, int value, Color color)
    {
        var tween = DOTween.Sequence().AppendCallback(() =>
        {
            var effect =
                EffectsPoolingControl.instance.GetEffectToFight(iconString, 1.5f,
                    target.cardObj.transform);
            effect.GetComponentInChildren<Text>().text = value.ToString();
            effect.GetComponentInChildren<Text>().color = color;
        });
        if (value < 0) tween.Join(target.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10)));
        return tween;
    }

    /// <summary>
    /// 被击退一格
    /// </summary>
    public static Tween OnRePos(FightCardData target, ChessPos pos) =>
        target.cardObj.transform.DOMove(pos.transform.position,
            0.2f);
    static float chessboardShakeIntensity = 30f;
    /// <summary>
    /// 棋盘震动
    /// </summary>
    public static Tween ChessboardConduct(Activity activity,Chessboard chessboard)
    {
        var transform = chessboard.transform;
        var origin = transform.position;
        if (activity.Conducts.Any(c=>c.Kind == CombatConduct.DamageKind && (c.Rouse>0|c.Critical>0)))
        {
            return transform.DOShakePosition(0.25f, chessboardShakeIntensity);
        }
        return transform.DOMove(origin, 0);
    }

    private static Vector2 GetWorldSize(Transform transform) =>
        ((RectTransform) transform).sizeDelta * transform.lossyScale;

    /// <summary>
    /// 更新棋子上的状态效果
    /// </summary>
    public static Tween UpdateStatusEffect(FightCardData target, int con = -1)
    {
        return DOTween.Sequence().OnComplete(() =>
        {
            if (con == -1)
                foreach (var state in CardState.ConsArray)
                    UpdateSingleStateEffect(target, (int)state);
            else UpdateSingleStateEffect(target, con);
        });
    }
    /// <summary>
    /// 更新棋子上的状态效果
    /// </summary>
    public static Tween UpdateStatusEffect(FightCardData target, CardState.Cons con) =>
        UpdateStatusEffect(target, (int)con);
    /// <summary>
    /// 更新棋子上的状态效果
    /// </summary>
    private static void UpdateSingleStateEffect(FightCardData target, int key)
    {
        var con = (CardState.Cons)key;
        var status = target.CardState.Data;
        var stateValue = status.ContainsKey(key) ? status[key] : 0;
        var stateName = CardState.IconName(con);
        if (stateValue <= 0)
        {
            //更新效果图
            if (target.StatesUi.ContainsKey(key))
            {
                var e = target.StatesUi[key];
                target.StatesUi.Remove(key);
                EffectsPoolingControl.instance.TakeBackStateIcon(e);
            }
            //更新小图标
            if (target.cardObj.War.CardStates.ContainsKey(stateName))
                DestroySateIcon(target.cardObj, con);

            return;
        }

        if (!target.StatesUi.ContainsKey(key))//添加效果图
        {
            var effect = EffectsPoolingControl.instance.GetStateEffect(CardState.IconName(con),
                target.cardObj.transform);
            target.StatesUi.Add(key, effect);

            if (con == CardState.Cons.EaseShield)
            {
                var fade = Math.Max(0.3f, 1f * stateValue / DataTable.GetGameValue(119));
                effect.Image.color = new Color(1, 1, 1, fade);
            }
        }

        if (!target.cardObj.War.CardStates.ContainsKey(stateName))
            CreateSateIcon(target.cardObj, con);
    }

    public static EffectStateUi AddSpriteEffect(ChessPos pos, CombatConduct conduct)
    {
        var effectId = GetSpriteEffectName(conduct);
        if (string.IsNullOrWhiteSpace(effectId)) return null;
        return EffectsPoolingControl.instance.GetStateEffect(effectId, pos.transform);
    }
    private static string GetSpriteEffectName(CombatConduct conduct)
    {
        var effectId = string.Empty;
        switch (conduct.Element)
        {
            case TerrainSprite.Forge:
                effectId = CardState.IconName(CardState.Cons.Forge);
                break;
            case TerrainSprite.YeHuo:
                effectId = CardState.IconName(CardState.Cons.Burn);
                break;
        }
        return effectId;
    }

    private static string GetEffectByStyle(CombatStyle style,CombatConduct conduct)
    {
        if (style.ArmedType >= 0)
            return GetHeroEffectByStyle(style);
        if (style.ArmedType == -2)//tower
            return GetTowerEffectByStyle(style);
        if (style.ArmedType == -3)//trap
            return GetTrapEffectByStyle(style);
        throw new ArgumentOutOfRangeException($"Arg = {style.ArmedType}");
    }

    private static string GetHeroEffectByStyle(CombatStyle style)
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
            case 42: value = Effect.Heal42A;break; // "42A";医师
            case 43: value = Effect.Heal43A;break; // "43A";大医师
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
            case 55: value = Effect.FireShip55A;break; // "55A";火船
            case 551: value = Effect.FireShip55A0;break; // "55A0";火船
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

    private static string GetTrapEffectByStyle(CombatStyle style)
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
    private static string GetTowerEffectByStyle(CombatStyle style)
    {
        switch (style.Military)
        {
            default: return Effect.Basic0A;break; // "0A";
            case 3: return Effect.Bow20A;break; // "20A";箭楼
            case 6: return Effect.Shield4A;break; // "4A";轩辕台
            case 0: //营寨
            case 2: return Effect.Heal42A;break; // "42A";奏乐台
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
            case ActivityResult.Types.EaseShield:
                tween.OnComplete(()=>TextEffectByResult(activity.Result.Type,target));
                break;
            default:
                break;
        }

        foreach (var conduct in activity.Conducts)
        {
            var color = Color.red;
            var tableId = -1;
            switch (conduct.Kind)
            {
                case CombatConduct.BuffKind:
                    switch ((CardState.Cons)conduct.Element)
                    {
                        case CardState.Cons.Bleed:
                            tableId = 16;
                            break;
                        case CardState.Cons.Poison:
                            tableId = 12;
                            break;
                        case CardState.Cons.Burn:
                            tableId = 20;
                            break;
                        case CardState.Cons.Imprisoned:
                            tableId = 11;
                            break;
                        case CardState.Cons.Cowardly:
                            tableId = 21;
                            break;
                        case CardState.Cons.Stunned:
                            if (conduct.Total < 0 &&
                                target.Status.GetBuff(CardState.Cons.Stunned) <= 0)
                                tableId = 14; //如果buff值是负数，判定为镇定效果(移除buff)
                            break;
                        case CardState.Cons.EaseShield:
                        case CardState.Cons.Disarmed:
                        case CardState.Cons.Shield:
                        case CardState.Cons.Invincible:
                        case CardState.Cons.Stimulate:
                        case CardState.Cons.StrengthUp:
                        case CardState.Cons.DodgeUp:
                        case CardState.Cons.CriticalUp:
                        case CardState.Cons.RouseUp:
                        case CardState.Cons.DefendUp:
                        case CardState.Cons.DeathFight:
                        case CardState.Cons.ShenZhu:
                        case CardState.Cons.Neizhu:
                        case CardState.Cons.Forge:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

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
                case ActivityResult.Types.EaseShield:
                effectName = DataTable.GetStringText(18);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        GetVTextEffect(effectName, target.cardObj.transform);
    }

    public static Tween VTextEffect(string effectName, Transform transform) =>
        DOTween.Sequence().OnComplete(() => GetVTextEffect(effectName, transform));

    private static void GetVTextEffect(string effectName, Transform transform)
    {
        if (string.IsNullOrWhiteSpace(effectName)) return;
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

    /// <summary>
    /// 创建状态图标
    /// </summary>
    private static void CreateSateIcon(WarGameCardUi ui, CardState.Cons con)
    {
        ui.War.CreateStateIco(con);
    }

    //删除状态图标
    private static void DestroySateIcon(WarGameCardUi ui, CardState.Cons con)
    {
        ui.War.RemoveStateIco(con);
    }
}