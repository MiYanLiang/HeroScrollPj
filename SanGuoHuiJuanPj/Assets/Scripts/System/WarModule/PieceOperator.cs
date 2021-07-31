using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.IO.Pipelines;
using System.Linq;

public interface IPieceOperator<TChess> where TChess : class, IChessman, new()
{
    /// <summary>
    /// 反馈棋子行动
    /// </summary>
    /// <param name="pieceAction"></param>
    /// <param name="offender"></param>
    /// <param name="grid"></param>
    /// <returns></returns>
    PieceAction[] Respond(PieceAction pieceAction, IPieceOperator<TChess> offender, ChessGrid<TChess> grid);
    /// <summary>
    /// 更新状态
    /// </summary>
    void UpdateFactors(CombatFactor[] stateFactors);
    PieceAction[] MainActions(ChessGrid<TChess> grid);
}
public abstract class PieceOperator : IPieceOperator<FightCardData>
{
    public abstract FightCardData Chessman { get; }
    public abstract AttackStyle Style { get; }
    public abstract PieceStatus Status { get; }

    public virtual PieceAction[] Respond(PieceAction pieceAction, IPieceOperator<FightCardData> offender,
        ChessGrid<FightCardData> grid)
    {
        foreach (var factor in pieceAction.Factors) UpdateFactor(factor);
        return null;
    }

    private void UpdateFactor(CombatFactor factor)
    {
        switch (factor.Kind)
        {
            case CombatFactor.Kinds.State:
                UpdateState(factor);
                return;
            case CombatFactor.Kinds.Damage:
                OnDamage(factor);
                return;
            case CombatFactor.Kinds.Heal:
                OnHeal(factor);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void UpdateFactors(CombatFactor[] factors)
    {
        foreach (var factor in factors) UpdateFactor(factor);
    }


    public abstract PieceAction[] MainActions(ChessGrid<FightCardData> grid);

    /// <summary>
    /// 加血量的方法。血量不会超过最大数
    /// </summary>
    /// <param name="factor"></param>
    protected virtual void OnHeal(CombatFactor factor)
    {
        Status.Hp += (int)factor.Total;
        if (Status.Hp > Status.MaxHp)
            Status.Hp = Status.MaxHp;
    }

    /// <summary>
    /// 扣除血量的方法,血量最低为0
    /// </summary>
    /// <param name="factor"></param>
    protected virtual void OnDamage(CombatFactor factor)
    {
        Status.Hp -= (int) factor.Total;
        if (Status.Hp < 0) Status.Hp = 0;
    }

    /// <summary>
    /// 状态添加或删减。如果状态值小或等于0，将直接删除。
    /// </summary>
    /// <param name="factor"></param>
    protected virtual void UpdateState(CombatFactor factor)
    {
        if (Status.States.ContainsKey(factor.Element))
            Status.States.Add(factor.Element, 0);
        Status.States[factor.Element] += (int)factor.Total;
        //去掉负数或是0的状态
        Status.States = Status.States.Where(s => s.Value <= 0).ToDictionary(s => s.Key, s => s.Value);
    }

}

public abstract class ChessmanOperator : PieceOperator
{
    protected static int[] FrontRows { get; } = { 0, 1, 2, 3, 4 };
    private FightCardData chessman;
    private AttackStyle attackStyle;
    private PieceStatus status;

    public override FightCardData Chessman => chessman;
    public override AttackStyle Style => attackStyle;
    public override PieceStatus Status => status;

    public virtual void Init(FightCardData card, AttackStyle style)
    {
        chessman = card;
        attackStyle = style;
        status = PieceStatus.Instance(card.Hp.Value, card.Hp.Max, card.Pos,
            card.fightState.Data.ToDictionary(s => s.Key, s => s.Value));
    }
}

public abstract class HeroOperator : ChessmanOperator
{
    public HeroCombatInfo CombatInfo => combatInfo;
    private HeroCombatInfo combatInfo;
    public override void Init(FightCardData card, AttackStyle style)
    {
        base.Init(card, style);
        combatInfo = HeroCombatInfo.GetInfo(card.cardId);
    }

    protected virtual CombatFactor[] MilitaryForceDamages() => new[] { ChessOperatorManager.GetDamage(this) };

    protected override CombatFactor[] GetOffensiveFactors()
    {
        return Chessman.fightState.Imprisoned > 0
            ? new[] {CombatFactor.InstanceDamage(Chessman.damage)}
            : MilitaryForceDamages();
    }

    public override ChessPosProcess Invoke() => Chessman.fightState.Stunned > 0 ? null : base.Invoke();
}

/// <summary>
/// 定点(单体)攻击英雄
/// </summary>
public abstract class SpotAttackHero : HeroOperator
{
    protected override IEnumerable<PieceOperator> GetTargets() => new[] { ChessOperatorManager.GetSequenceTarget(this) };
}
/// <summary>
/// (9)先锋处理器
/// </summary>
public class XianFengOperator : SpotAttackHero
{
    protected virtual int ComboRatio => DataTable.GetGameValue(42);
    protected virtual int ComboTimes => DataTable.GetGameValue(43);
    
    protected override PieceTrigger[] OnActionDoneTriggers() => new[]
    {
        PieceTrigger.Instance(PieceTrigger.Combo, new [] {ComboRatio, ComboTimes})
    };
    protected override ChessPosProcess OnCounter(PieceOperator offense, PieceAction action) => null;
    protected override ChessPosProcess OnDeathTrigger(PieceOperator offense, PieceAction action) => null;
}
/// <summary>
/// (60)急先锋处理器
/// </summary>
public class JiXianFengOperator : XianFengOperator
{
    protected override int ComboRatio => DataTable.GetGameValue(44);
    protected override int ComboTimes => DataTable.GetGameValue(45);
}
/// <summary>
/// (16)骠骑处理器
/// </summary>
public class PiaoQiOperator : SpotAttackHero
{
    protected virtual int ComboRatio => DataTable.GetGameValue(47);

    protected override PieceTrigger[] OnActionDoneTriggers() => new[]
        {PieceTrigger.Instance(PieceTrigger.ComboOnSpecialAttack, ComboRatio)};

    protected override ChessPosProcess OnCounter(PieceOperator offense, PieceAction action) => null;

    protected override ChessPosProcess OnDeathTrigger(PieceOperator offense, PieceAction action) => null;
}