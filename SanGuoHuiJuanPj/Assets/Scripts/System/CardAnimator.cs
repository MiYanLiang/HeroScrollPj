using System;
using System.Linq;
using Assets.System.WarModule;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Monetization;
using UnityEngine.UI;

public class CardAnimator
{
    static float RangeHeroPreActionBegin = 0.5f;

    public static float Move = 0.534f;
    public static float Back = 0.167f;
    public static float forward = 0.2f; //去
    public static float stepBack = 0.3f; //回
    public static float charge = 0.4f; //等
    public static float hit = 0.1f; //去

    public static Tween PreActionTween(FightCardData card, FightCardData target)
    {
        var tween = DOTween.Sequence();
        switch (card.ChessmanStyle.Type)
        {
            case CombatStyle.Types.None:
                break;
            case CombatStyle.Types.Melee:
                if (target != null) tween.Join(MeleeMoveAnimation(card, target));
                break;
            case CombatStyle.Types.Range:
                tween.Join(RangePreActAnimation(card));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return tween;
    }

    //攻击行动方式0-适用于-主动塔,远程兵
    private static Tween RangePreActAnimation(FightCardData card)
    {
        var obj = card.cardObj;
        return obj.transform.DOScale(new Vector3(1.15f, 1.15f, 1), RangeHeroPreActionBegin).SetAutoKill(false).OnComplete(() => obj.transform.DOPlayBackwards());
    }

    /// <summary>
    /// 近战移动方式
    /// </summary>
    private static Tween MeleeMoveAnimation(FightCardData card, FightCardData target)
    {
        var ui = card.cardObj;
        var size = GetWorldSize(target.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? 1 : -1) * size.y;
        var targetPos = target.cardObj.transform.position;
        var facingPos = new Vector3(targetPos.x, targetPos.y - oneYOffset, targetPos.z);
        return DOTween.Sequence().Append(ui.transform.DOMove(facingPos, Move * forward)).AppendInterval(charge);
    }

    /// <summary>
    /// 结束动作(卡牌大小归位)
    /// </summary>
    public static Tween FinalizeAnimation(FightCardData card, Vector3 origin)
    {
        var sBack = card.cardObj.transform.DOMove(origin, Back);
        var scaleBack = card.cardObj.transform.DOScale(Vector3.one, Back);
        return DOTween.Sequence().Join(sBack).Join(scaleBack);
    }

    public static Tween CounterAnimation(FightCardData card)
    {
        //这里反击所有兵种都显示文字效果。并不仅限于禁卫
        return StepBackAndHit(card, 0.3f, 0.3f, 0.1f).OnComplete(() => GetVTextEffect(13.ToString(), card.cardObj.transform));
    }

    //退后向前进攻模式
    public static Tween StepBackAndHit(FightCardData card, float backStepDistance = 0.5f,float time = 1f,float chargeRate = 1f)
    {
        var ui = card.cardObj;
        var size = GetWorldSize(card.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? -1 : 1) * size.y;
        var origin = ui.transform.position;
        return DOTween.Sequence()
            .Append(ui.transform.DOMove(origin + new Vector3(0, oneYOffset * backStepDistance), stepBack * time))
            .AppendInterval(charge * chargeRate)
            .Append(ui.transform.DOMove(origin, hit * time));
    }

    /// <summary>
    /// 闪避动作
    /// </summary>
    public static Tween SideDodgeAnimation(FightCardData target,float distance = 0.2f,float time = 0.2f)
    {
        var ui = target.cardObj;
        var size = GetWorldSize(target.cardObj.transform);
        var origin = ui.transform.position;
        return DOTween.Sequence()
            .Append(ui.transform.DOMove(origin + new Vector3(size.x * distance, 0, 0), time))
            .Append(ui.transform.DOMove(origin, time * 0.5f));
    }
    /// <summary>
    /// 被攻击摇晃动作
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static Tween SufferShakeAnimation(FightCardData target) =>
        target.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10));

    /// <summary>
    /// 文字特效
    /// </summary>
    /// <param name="target"></param>
    /// <param name="conduct"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static Tween NumberEffectTween(FightCardData target, CombatConduct conduct,Color color = default)
    {
        var value = (int)conduct.Total;
        if (value == 0) return DOTween.Sequence();
        if (color == default)
            color = CombatConduct.IsPositiveConduct(conduct)
                ? ColorDataStatic.huiFu_green
                : ColorDataStatic.name_deepRed;

        return DOTween.Sequence().AppendCallback(() =>
        {
            var effect =
                EffectsPoolingControl.instance.GetEffectToFight(Effect.DropBlood, 1.5f,
                    target.cardObj.transform);
            effect.GetComponentInChildren<Text>().text = value.ToString();
            effect.GetComponentInChildren<Text>().color = color;
        });
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
    public static Tween ChessboardConduct(Chessboard chessboard)
    {
        var transform = chessboard.transform;
        var origin = transform.position;
        return DOTween.Sequence().Join(transform.DOShakePosition(0.25f, chessboardShakeIntensity))
            .AppendInterval(0.3f)
            .OnComplete(() => transform.DOMove(origin, 0));
    }

    private static Vector2 GetWorldSize(Transform transform) =>
        ((RectTransform) transform).sizeDelta * transform.lossyScale;

    /// <summary>
    /// 更新棋子上的状态效果
    /// </summary>
    public static Tween UpdateStateIcon(FightCardData target, int con = -1)
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
    public static Tween UpdateStateIcon(FightCardData target, CardState.Cons con) =>
        UpdateStateIcon(target, (int)con);
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

        if (!target.StatesUi.ContainsKey(key) || target.StatesUi[key] == null)//添加效果图
        {
            var effect = EffectsPoolingControl.instance.GetBuffEffect(CardState.IconName(con),
                target.cardObj.transform);
            if (!target.StatesUi.ContainsKey(key))
                target.StatesUi.Add(key, null);
            target.StatesUi[key] = effect;

            if (con == CardState.Cons.EaseShield)
            {
                var fade = Math.Max(0.3f, 1f * stateValue / DataTable.GetGameValue(119));
                effect.Image.color = new Color(1, 1, 1, fade);
            }
        }

        if (!target.cardObj.War.CardStates.ContainsKey(stateName))
            CreateSateIcon(target.cardObj, con);
    }

    public static EffectStateUi AddPosSpriteEffect(ChessPos pos, CombatConduct conduct)
    {
        var effectId = string.Empty;
        switch (conduct.Element)
        {
            case PosSprite.Forge:
                effectId = CardState.IconName(CardState.Cons.Forge);
                break;
            case PosSprite.YeHuo:
                effectId = CardState.IconName(CardState.Cons.Burn);
                break;
            case PosSprite.Thunder:
                Debug.LogWarning("落雷的buff文件夹有问题!");
                break;
        }
        if (string.IsNullOrWhiteSpace(effectId)) return null;
        return EffectsPoolingControl.instance.GetBuffEffect(effectId, pos.transform);
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
                        case CardState.Cons.BattleSoul:
                        case CardState.Cons.StrengthUp:
                        case CardState.Cons.DodgeUp:
                        case CardState.Cons.CriticalUp:
                        case CardState.Cons.RouseUp:
                        case CardState.Cons.ArmorUp:
                        case CardState.Cons.DeathFight:
                        case CardState.Cons.ShenZhu:
                        case CardState.Cons.Neizhu:
                        case CardState.Cons.Forge:
                        case CardState.Cons.Stimulate:
                        case CardState.Cons.Confuse:
                        case CardState.Cons.YellowBand:
                        case CardState.Cons.Chained:
                        case CardState.Cons.Murderous:
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
        effectObj.GetComponentsInChildren<Image>()[1].sprite = GameResources.Instance.VText[effectName];
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
    private static void CreateSateIcon(WarGameCardUi ui, CardState.Cons con) => ui.War.CreateStateIco(con);

    //删除状态图标
    private static void DestroySateIcon(WarGameCardUi ui, CardState.Cons con) => ui.War.RemoveStateIco(con);
}