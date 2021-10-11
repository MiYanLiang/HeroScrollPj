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
    private float MeleeMove => meleeMove;
    private float MeleeReturn => meleeReturn;
    private float MeleeStepBack => meleeStepBack;
    private float MeleeCharge => meleeCharge;
    private float MeleeHit => meleeHit;
    private float RangePreAction => rangePreAct;
    private float CounterTempo => counterTempo;
    private float CounterStepBack => counterStepBack;
    private float CounterCharge => counterCharge;
    private float DodgeDistance => dodgeDistance;
    private float DodgeDuration => dodgeDuration;

    [Header("近战前进(秒)")]
    [SerializeField] private float meleeMove = 0.2f;
    [SerializeField] private float meleeStop = 0.15f;
    [Header("近战攻击(秒)")]
    [SerializeField] private float meleeStepBack = 0.3f;
    [SerializeField] private float meleeCharge = 0.4f;
    [SerializeField] private float meleeHit = 0.1f;
    [Header("近战归位(秒)")]
    [SerializeField] private float meleeReturn = 0.15f;
    [Header("后退距离(格)")]
    [SerializeField] private float stepBackDistance = 1f;

    [Header("远程前摇(秒)")] 
    [SerializeField] private float rangePreAct = 0.5f;

    [Header("反击节奏(倍速)")][SerializeField] private float counterTempo = 0.3f;
    [Header("反击后退距离(格)")][SerializeField] private float counterStepBack = 0.3f;
    [Header("反击蓄力(秒)")][SerializeField] private float counterCharge= 0.1f;
    [Header("闪避")][SerializeField] private float dodgeDistance= 0.3f;
    [SerializeField] private float dodgeDuration = 0.2f;
    [Header("竖文字显示(秒)")][SerializeField] private float VTextLasting = 1.5f;
    [Header("数字显示(秒)")][SerializeField] private float HTextLasting = 1.5f;

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
    private Tween RangePreActAnimation(FightCardData card)
    {
        var obj = card.cardObj;
        return obj.transform.DOScale(new Vector3(1.15f, 1.15f, 1), RangePreAction).SetAutoKill(false).OnComplete(() => obj.transform.DOPlayBackwards());
    }

    /// <summary>
    /// 近战移动方式
    /// </summary>
    private Tween MeleeMoveAnimation(FightCardData card, FightCardData target)
    {
        var ui = card.cardObj;
        var size = GetWorldSize(target.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? 1 : -1) * size.y;
        var targetPos = target.cardObj.transform.position;
        var facingPos = new Vector3(targetPos.x, targetPos.y - oneYOffset, targetPos.z);
        return DOTween.Sequence().Append(ui.transform.DOMove(facingPos, MeleeMove)).AppendInterval(MeleeCharge);
    }

    /// <summary>
    /// 结束动作(卡牌大小归位)
    /// </summary>
    public Tween FinalizeAnimation(FightCardData card, Vector3 origin)
    {
        var sBack = card.cardObj.transform.DOMove(origin, MeleeReturn);
        var scaleBack = card.cardObj.transform.DOScale(Vector3.one, MeleeReturn);
        return DOTween.Sequence().Join(sBack).Join(scaleBack);
    }

    public Tween CounterAnimation(FightCardData card)
    {
        //这里反击所有兵种都显示文字效果。并不仅限于禁卫
        return StepBackAndHit(card, CounterStepBack, CounterTempo, CounterCharge).OnComplete(() =>
            GetVTextEffect(Effect.HeroActivityVText(13, 1), card.cardObj.transform));
    }

    //退后向前进攻模式
    public Tween StepBackAndHit(FightCardData card, float backStepDistance = 0.5f,float time = 1f,float chargeRate = 1f)
    {
        var ui = card.cardObj;
        var size = GetWorldSize(card.cardObj.transform);
        var oneYOffset = (card.isPlayerCard ? -1 : 1) * size.y;
        var origin = ui.transform.position;
        return DOTween.Sequence()
            .Append(ui.transform.DOMove(origin + new Vector3(0, oneYOffset * backStepDistance), MeleeStepBack * time))
            .AppendInterval(MeleeCharge * chargeRate)
            .Append(ui.transform.DOMove(origin, MeleeHit * time));
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
            .Append(ui.transform.DOMove(origin + new Vector3(size.x * DodgeDistance, 0, 0), DodgeDuration))
            .Append(ui.transform.DOMove(origin, DodgeDuration * 0.5f));
    }
    /// <summary>
    /// 被攻击摇晃动作
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public Tween SufferShakeAnimation(FightCardData target) =>
        target.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10));

    /// <summary>
    /// 文字特效
    /// </summary>
    /// <param name="target"></param>
    /// <param name="conduct"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public void NumberEffectTween(FightCardData target, CombatConduct conduct,Color color = default)
    {
        var value = (int)conduct.Total;
        if (value == 0) return;
        if (color == default)
            color = CombatConduct.IsPositiveConduct(conduct)
                ? ColorDataStatic.huiFu_green
                : ColorDataStatic.name_deepRed;
        var effect =
            EffectsPoolingControl.instance.GetVTextEffect(Effect.DropBlood, HTextLasting,
                target.cardObj.transform);
        effect.GetComponentInChildren<Text>().text = value.ToString();
        effect.GetComponentInChildren<Text>().color = color;
    }

    /// <summary>
    /// 被击退一格
    /// </summary>
    public Tween OnRePos(FightCardData target, ChessPos pos) =>
        target.cardObj.transform.DOMove(pos.transform.position,
            0.2f);
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
        ((RectTransform) transform).sizeDelta * transform.lossyScale;

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
    public void UpdateStateIcon(FightCardData target, CardState.Cons con) =>
        UpdateStateIcon(target, (int)con);
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
            //更新效果图
            if (target.StatesUi.ContainsKey(key))
            {
                var e = target.StatesUi[key];
                target.StatesUi.Remove(key);
                EffectsPoolingControl.instance.RecycleStateBuff(e);
            }
            //更新小图标
            if (target.cardObj.War.CardStates.ContainsKey(iconId))
                DestroySateIcon(target.cardObj, con);

            return;
        }

        if (!target.StatesUi.ContainsKey(key) || target.StatesUi[key] == null)//添加效果图
        {
            var effect = EffectsPoolingControl.instance.GetStateBuff(con, target.cardObj.transform);
            if (!target.StatesUi.ContainsKey(key))
                target.StatesUi.Add(key, null);
            target.StatesUi[key] = effect;
        }

        if (!target.cardObj.War.CardStates.ContainsKey(iconId))
            CreateSateIcon(target.cardObj, con);

        target.StatesUi[key].ImageFading(Effect.BuffFading(con, stateValue));
    }


    public void DisplayTextEffect(FightCardData target, Activity activity)
    {
        if (activity.IsRePos)
            GetHTextEffect(17, target.cardObj.transform, Color.red);

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
                GetHTextEffect(tableId, target.cardObj.transform, color);
            if (conduct.Critical > 0)
                GetHTextEffect(23, target.cardObj.transform, color);
            if (conduct.Rouse > 0)
                GetHTextEffect(22, target.cardObj.transform, color);
        }
    }

    public void VTextEffect(int vTextId, Transform trans) => GetVTextEffect(vTextId, trans);

    private void GetVTextEffect(int vTextId , Transform trans)
    {
        if (vTextId == -1) return;
        var effectObj = EffectsPoolingControl.instance.GetVTextEffect(Effect.SpellTextV, VTextLasting, trans);
        effectObj.GetComponentsInChildren<Image>()[1].sprite = GameResources.Instance.VText[vTextId];
    }

    private void GetHTextEffect(int id,Transform trans, Color color)
    {
        var effectObj = EffectsPoolingControl.instance.GetVTextEffect(Effect.SpellTextH, VTextLasting, trans);
        effectObj.GetComponentInChildren<Text>().text = DataTable.GetStringText(id);
        effectObj.GetComponentInChildren<Text>().color = color;
    }

    /// <summary>
    /// 创建状态图标
    /// </summary>
    private void CreateSateIcon(WarGameCardUi ui, CardState.Cons con) => ui.War.CreateStateIco(con);

    //删除状态图标
    private void DestroySateIcon(WarGameCardUi ui, CardState.Cons con) => ui.War.RemoveStateIco(con);
}