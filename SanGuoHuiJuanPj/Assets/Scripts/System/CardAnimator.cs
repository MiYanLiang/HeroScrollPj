using System;
using System.Data;
using System.Linq;
using Assets.System.WarModule;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Monetization;
using UnityEngine.UI;

public class CardAnimator : MonoBehaviour
{
    public static CardAnimator instance;
    [Header("近战")][SerializeField] private MeleeField Melee;
    [Header("远程")][SerializeField] private RangeField Range;
    [Header("反击")][SerializeField] private CounterField Counter;
    [Header("闪避")][SerializeField] private DodgeField Dodge;
    [Header("辅助")][SerializeField] private AssistField Assist;
    [Header("其它")] public MiscField Misc;

    void Awake()
    {
        if (instance != null)
            throw new InvalidOperationException("Duplicated instance!");
        instance = this;
    }

    public Tween PreActionTween(FightCardData card, FightCardData target)
    {
        var tween = DOTween.Sequence();
        switch (card.ChessmanStyle.Type)
        {
            case CombatStyle.Types.None:
                break;
            case CombatStyle.Types.Melee when (target != null):
                tween.Join(MeleeMoveAnimation(card, target));
                break;
            case CombatStyle.Types.Range:
                tween.Join(RangePreActAnimation(card));
                break;
        }

        return tween;
    }

    //攻击行动方式0-适用于-主动塔,远程兵
    private Tween RangePreActAnimation(FightCardData card)
    {
        var obj = card.cardObj;
        return obj.transform.DOScale(Range.PreActScale, Range.PreAct).OnComplete(() => obj.transform.DOPlayBackwards());
    }

    /// <summary>
    /// 近战移动方式
    /// </summary>
    private Tween MeleeMoveAnimation(FightCardData card, FightCardData target)
    {
        var size = GetWorldSize(target.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? 1 : -1) * size.y;
        var targetPos = target.cardObj.transform.position;
        var facingPos = new Vector3(targetPos.x, targetPos.y - oneYOffset, targetPos.z);
        return DOTween.Sequence().Append(MoveTween(card, facingPos, Melee.Forward)).AppendInterval(Melee.Stop);
    }

    /// <summary>
    /// 结束动作(卡牌大小归位)
    /// </summary>
    public Tween FinalizeAnimation(FightCardData card, Vector3 origin)
    {
        var sBack = MoveTween(card, origin, Melee.Return);
        var scaleBack = card.cardObj.transform.DOScale(Vector3.one, Melee.Return);
        return DOTween.Sequence().Join(sBack).Join(scaleBack);
    }

    public Tween MoveTween(FightCardData card, Vector3 origin, float sec = -1)
    {
        if (sec <= 0) sec = Range.RecoverSec;
        return card.cardObj.transform.DOMove(origin, sec);
    }

    public Tween CounterAnimation(FightCardData card)
    {
        //这里反击所有兵种都显示文字效果。并不仅限于禁卫
        return StepBackAndHit(card, Counter.StepBack, Counter.StepBackSec, Counter.Charge).OnComplete(() =>
            GetVTextEffect(Effect.HeroActivityVText(13, 1), card.cardObj.transform));
    }

    //退后向前进攻模式
    public Sequence StepBackAndHit(FightCardData card, float backStepDistance = -0.5f, float stepBckTime = -1f,
        float chargeTime = -1f)
    {
        if (stepBckTime < 0)
            stepBckTime = Melee.StepBack;
        if (chargeTime < 0)
            chargeTime = Melee.Charge;
        if (backStepDistance < 0)
            backStepDistance = Melee.StepBackDistance;
        var ui = card.cardObj;
        var size = GetWorldSize(card.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? -1 : 1) * size.y;
        var origin = ui.transform.position;
        return DOTween.Sequence()
            .Append(MoveTween(card, origin + new Vector3(0, oneYOffset * backStepDistance), stepBckTime))
            .AppendInterval(chargeTime)
            .Append(MoveTween(card, origin, Melee.Hit));
    }

    /// <summary>
    /// 后座力动画
    /// </summary>
    /// <returns></returns>
    public Sequence RecoilTween(FightCardData card)
    {
        var ui = card.cardObj;
        var size = GetWorldSize(card.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? -1 : 1) * size.y;
        var origin = ui.transform.position;
        return DOTween.Sequence()
            .Append(card.cardObj.transform.DOMove(origin + new Vector3(0, oneYOffset * Range.Recoil), Range.RecoilSec));
    }

    /// <summary>
    /// 闪避动作
    /// </summary>
    public Tween SideDodgeAnimation(FightCardData target)
    {
        var ui = target.cardObj;
        var size = GetWorldSize(target.cardObj.transform);
        var origin = ui.transform.position;
        return DOTween.Sequence()
            .Append(ui.transform.DOMove(origin + new Vector3(size.x * Dodge.Distance, 0, 0), Dodge.Duration))
            .Append(ui.transform.DOMove(origin, Dodge.Duration * 0.5f));
    }

    /// <summary>
    /// 被攻击摇晃动作
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public Tween SufferShakeAnimation(FightCardData target) =>
        target.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10));

    public Tween AssistEnlargeAnimation(FightCardData target)
    {
        var obj = target.cardObj;
        return DOTween.Sequence().Append(obj.transform.DOScale(Assist.Enlarge, Assist.EnlargeSec))
            .Append(obj.transform.DOScale(1, Assist.EnlargeSec * 0.5f));
    }

    /// <summary>
    /// 文字特效
    /// </summary>
    /// <param name="target"></param>
    /// <param name="value"></param>
    /// <param name="color"></param>
    /// <param name="dmgType"></param>
    /// <returns></returns>
    public void NumberEffectTween(FightCardData target, int value, Color color, Damage.Types dmgType)
    {
        if (value == 0) return;
        NumberEffectTween(target, value.ToString(), color, dmgType);
    }
    public void NumberEffectTween(FightCardData target, string value, Color color, Damage.Types dmgType)
    {
        var effect =
            EffectsPoolingControl.instance.GetVTextEffect(Effect.DropBlood, Misc.VTextLasting,
                target.cardObj.transform);
        var text = effect.GetComponentInChildren<Text>();
        text.text = value;
        text.color = color;
        switch (dmgType)
        {
            case Damage.Types.General:
                effect.transform.DOScale(1, 0);
                break;
            case Damage.Types.Critical:
                effect.transform.DOScale(Misc.CriticalTextEnlarge, 0);
                break;
            case Damage.Types.Rouse:
                effect.transform.DOScale(Misc.RouseTextEnlarge, 0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dmgType), dmgType, null);
        }
    }

    /// <summary>
    /// 被击退一格
    /// </summary>
    public Tween OnRePos(FightCardData target, ChessPos pos) =>
        target.cardObj.transform.DOMove(pos.transform.position,Misc.RePos);

    static float chessboardShakeIntensity = 30f;

    /// <summary>
    /// 棋盘震动
    /// </summary>
    public Tween ChessboardConduct(Chessboard chessboard)
    {
        var trans = chessboard.transform;
        var origin = trans.position;
        return DOTween.Sequence().Join(trans.DOShakePosition(0.25f, chessboardShakeIntensity))
            .AppendInterval(0.3f)
            .OnComplete(() => trans.DOMove(origin, 0));
    }

    private static Vector2 GetWorldSize(Transform transform) =>
        ((RectTransform)transform).sizeDelta * transform.lossyScale;

    /// <summary>
    /// 更新棋子上的状态效果
    /// </summary>
    public void UpdateStateIcon(FightCardData target, int con = -1)
    {
        if (con == -1)
            foreach (var state in CardState.ConsArray)
                UpdateSingleStateEffect(target, (int)state);
        else UpdateSingleStateEffect(target, con);
    }

    /// <summary>
    /// 更新棋子上的状态效果
    /// </summary>
    private void UpdateSingleStateEffect(FightCardData target, int key)
    {
        var con = (CardState.Cons)key;
        var status = target.CardState.Data;
        var stateValue = status.ContainsKey(key) ? status[key] : 0;
        var iconId = Effect.GetStateIconId(con);
        if (stateValue <= 0)
        {
            //更新Buff效果
            if (target.StatesUi.ContainsKey(key))
            {
                var e = target.StatesUi[key];
                target.StatesUi.Remove(key);
                if (e != null) EffectsPoolingControl.instance.RecycleStateBuff(e);
            }

            //更新Icon小图标
            if (target.cardObj.War.CardStates.ContainsKey(iconId))
                DestroySateIcon(target.cardObj, con);

            return;
        }

        if (!target.StatesUi.ContainsKey(key) || target.StatesUi[key] == null) //添加效果图
        {
            EffectStateUi effect;
            if (con == CardState.Cons.Murderous) //杀气的特效是根据兵种提现不同特效
            {
                var murderousBuffId = Effect.GetMurderousBuffId(target.ChessmanStyle.Military);
                effect = EffectsPoolingControl.instance.GetStateBuff(murderousBuffId, target.cardObj.transform);
            }
            else effect = EffectsPoolingControl.instance.GetStateBuff(con, target.cardObj.transform);
            if (!target.StatesUi.ContainsKey(key))
                target.StatesUi.Add(key, null);
            target.StatesUi[key] = effect;
        }

        if (!target.cardObj.War.CardStates.ContainsKey(iconId))
            CreateSateIcon(target.cardObj, con);

        if (target.StatesUi.ContainsKey(key) && target.StatesUi[key])
            target.StatesUi[key].ImageFading(Effect.BuffFading(con, stateValue));
    }


    public void DisplayTextEffect(FightCardData target, Activity activity)
    {
        if (activity.IsRePos)
            GetHTextEffect(17, target.cardObj.transform, Color.red, 1);

        foreach (var conduct in activity.Conducts)
        {
            var color = Color.red;
            var tableId = -1;
            switch (conduct.Kind)
            {
                case CombatConduct.BuffKind:
                case CombatConduct.ElementDamageKind:
                    if (conduct.Element < 0) continue;
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
                        case CardState.Cons.Mark:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(conduct.Element), conduct.Element.ToString());
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

            var scale = 1;
            if (tableId >= 0)
                GetHTextEffect(tableId, target.cardObj.transform, color, scale);
            if (conduct.Critical > 0)
                GetHTextEffect(23, target.cardObj.transform, color, scale);
            if (conduct.Rouse > 0)
                GetHTextEffect(22, target.cardObj.transform, color, scale);
        }
    }

    public void VTextEffect(int vTextId, Transform trans) => GetVTextEffect(vTextId, trans);

    private void GetVTextEffect(int vTextId, Transform trans)
    {
        if (vTextId == -1) return;
        var effectObj =
            EffectsPoolingControl.instance.GetVTextEffect(Effect.SpellTextV, Misc.VTextLasting, trans);
        effectObj.transform.DOScale(1, 0);
        effectObj.GetComponentsInChildren<Image>()[1].sprite = GameResources.Instance.VText[vTextId];
    }

    private void GetHTextEffect(int id, Transform trans, Color color, float scale)
    {
        var effectObj = EffectsPoolingControl.instance.GetVTextEffect(Effect.SpellTextH, Misc.HTextLasting, trans);
        effectObj.GetComponentInChildren<Text>().text = DataTable.GetStringText(id);
        effectObj.GetComponentInChildren<Text>().color = color;
        effectObj.transform.DOScale(scale, 0);
    }

    /// <summary>
    /// 创建状态图标
    /// </summary>
    private void CreateSateIcon(WarGameCardUi ui, CardState.Cons con) => ui.War.CreateStateIco(con);

    //删除状态图标
    private void DestroySateIcon(WarGameCardUi ui, CardState.Cons con) => ui.War.RemoveStateIco(con);


    [Serializable]
    private class MeleeField
    {
        [Header("前进(秒)")] public float Forward = 0.2f;
        [Header("前摇停顿(秒)")]public float Stop = 0.15f;
        [Header("蓄力(秒)")]public float Charge = 0.3f;
        [Header("后退(秒)")] public float StepBack = 0.2f;
        [Header("后退距离(格)")] public float StepBackDistance = 0.5f;
        [Header("攻击(秒)")] public float Hit = 0.1f;
        [Header("归位(秒)")] public float Return = 0.15f;
    }

    [Serializable]
    private class RangeField
    {
        [Header("前摇(秒)")] public float PreAct = 0.3f;
        [Header("前摇放大(倍)")] public float PreActScale = 1.2f;
        [Header("后座力(格)")] public float Recoil = 0.3f;
        [Header("后座力时长(秒)")] public float RecoilSec = 0.05f;
        [Header("站稳时长(秒)")] public float RecoverSec = 0.05f;
    }

    [Serializable]
    private class CounterField
    {
        [Header("后退(秒)")] public float StepBackSec = 0.15f;
        [Header("后退距离(格)")] public float StepBack = 0.3f;
        [Header("蓄力(秒)")] public float Charge = 0.1f;
    }

    [Serializable]
    private class DodgeField
    {
        [Header("距离(格)")] public float Distance = 0.3f;
        [Header("时长(秒)")]public float Duration = 0.2f;
    }

    [Serializable]
    private class AssistField
    {
        [Header("放大(倍)")] public float Enlarge = 1.1f;
        [Header("时长(秒)")] public float EnlargeSec = 0.2f;
    }

    [Serializable]
    public class MiscField
    {
        [Header("竖文字显示时长(秒)")] public float VTextLasting = 1.5f;
        [Header("横文字显示时长(秒)")]public float HTextLasting = 1.5f;
        [Header("暴击文字大小(倍)")] public float CriticalTextEnlarge = 1.5f;
        [Header("会心文字大小(倍)")] public float RouseTextEnlarge = 2f;
        [Header("击退执行(秒)")] public float RePos = 0.2f;
        [Header("棋格显示/渐变时长(秒)")] public float ChessGridFadingSec = 3f;
        [Header("棋格渐变Alpha(度)")] [Range(0, 1)] public float ChessGridFading = 0.3f;
        [Header("(Buff)羁绊时长(秒)")] public float JBAnimLasting = 2f;
        [Header("金币掉落的文字颜色")]public Color TreasureChestTextColor = Color.yellow;
        [Header("天暗度(小)")] [Range(0.3f, 1f)] public float Shady = 0.6f;
        [Header("天暗度(大)")] [Range(0.3f, 1f)] public float Dark = 0.8f;
        [Header("掉血(秒)")] [Range(0f, 3f)] public float DropBloodSec = 0.7f;
    }
}
