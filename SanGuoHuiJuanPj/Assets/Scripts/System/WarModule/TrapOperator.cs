using System.Collections.Generic;
using System.Linq;using UnityEngine.UI;

/// <summary>
/// 陷阱处理器
/// </summary>
public abstract class TrapOperator : ChessmanOperator
{
    /// <summary>
    /// 陷阱不会主动攻击
    /// </summary>
    /// <returns></returns>
    public override PieceAction Invoke() => null;

    protected override CombatFactor DamageRespond(PieceOperator offense, CombatFactor damage)
    {
        if (offense.Style.ArmedType == -2) //如果对面是陷阱类型
            return CombatFactor.InstanceDamage(damage.Basic * 2);//执行两倍伤害
        return base.DamageRespond(offense, damage);
    }

}

public class BlankTrapOperator : TrapOperator
{
    private readonly CombatFactor[] offensiveFactors = new CombatFactor[0];
    private readonly IEnumerable<PieceOperator> targets = new PieceOperator[0];

    protected override IEnumerable<PieceOperator> GetTargets()
    {
        return targets;
    }

    protected override CombatFactor[] GetOffensiveFactors() => offensiveFactors;

    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit) => null;
    protected override PieceAction OnDeathTrigger(PieceOperator offense, PieceHit hit) => null;
}

/// <summary>
/// 被销毁触发类的陷阱
/// </summary>
public abstract class DeathTriggerTrapOperator : TrapOperator
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit) => null;

    protected override PieceAction OnDeathTrigger(PieceOperator offense, PieceHit hit)
    {
        var move = PieceAction.Instance(Card.PosIndex);
        foreach (var target in GetTargets()) target.Respond(this, move, GetOffensiveFactors());
        return move;
    }
}
/// <summary>
/// 反伤类的陷阱
/// </summary>
public abstract class ReflexiveTrapOperator : TrapOperator
{
    protected override IEnumerable<PieceOperator> GetTargets() => new PieceOperator[0];
    protected override CombatFactor[] GetOffensiveFactors() => new CombatFactor[0];
    protected override PieceAction OnDeathTrigger(PieceOperator offense, PieceHit hit) => null;

    /// <summary>
    /// 是否攻城车
    /// </summary>
    /// <param name="piece"></param>
    /// <returns></returns>
    protected bool IsGongChengChe(PieceOperator piece) => piece.Style.Military == 23;
}
/// <summary>
/// 拒马
/// </summary>
public class JuMaOperator : ReflexiveTrapOperator
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit)
    {
        if (offense.Style.CombatStyle > 0) return null;
        if (IsGongChengChe(offense)) return null;//如果攻城车，不反伤
        var damage = hit.Factors.First(f => f.Kind == CombatFactor.Kinds.Damage).Total;
        var move = PieceAction.Instance(Pos);
        var factor = CombatFactor.InstanceDamage(damage * 0.01f * DataTable.GetGameValue(8));
        offense.Respond(this, move, factor);
        return move;
    }
}
/// <summary>
/// 滚木处理器
/// </summary>
public class GunMuOperator : DeathTriggerTrapOperator
{
    protected override IEnumerable<PieceOperator> GetTargets()
    {
        var cards = FightForManager.instance.GetCardList(!Card.isPlayerCard).Where(c => c.IsAlive)
            .OrderBy(c => c.PosIndex);
        var grid = FrontRows.ToDictionary(i => i, _ => false); //init mapper
        var max = FrontRows.Length;
        var targets = new List<FightCardData>();
        foreach (var card in cards)
        {
            var column = card.PosIndex % max; //获取直线排数
            if (grid[column]) continue; //如果当前排已有伤害目标，不记录后排
            targets.Add(card);
        }

        return targets.Select(WarOperatorManager.GetWarCard);
    }

    protected override CombatFactor[] GetOffensiveFactors()
    {
        //根据比率给出是否眩晕
        if (IsRandomPass(DataTable.GetGameValue(15)))
            return new[]
            {
                CombatFactor.InstanceDamage(Card.damage),
                CombatFactor.InstanceOffendState(FightState.Cons.Stunned)
            };
        return new[] {CombatFactor.InstanceDamage(Card.damage)};
    }
}
/// <summary>
/// 滚石处理器
/// </summary>
public class GunShiOperator : DeathTriggerTrapOperator
{
    protected override IEnumerable<PieceOperator> GetTargets()
    {
        var verticalIndex = Card.PosIndex % 5;
        var cards = FightForManager.instance.GetCardList(!Card.isPlayerCard)
            .Where(c => c.IsAlive && c.PosIndex == verticalIndex)
            .OrderBy(c => c.PosIndex);
        return cards.Select(WarOperatorManager.GetWarCard);
    }

    protected override CombatFactor[] GetOffensiveFactors()
    {
        //根据比率给出是否眩晕
        if (IsRandomPass(DataTable.GetGameValue(16)))
            return new[]
            {
                CombatFactor.InstanceDamage(Card.damage),
                CombatFactor.InstanceOffendState(FightState.Cons.Stunned)
            };
        return new[] {CombatFactor.InstanceDamage(Card.damage)};
    }
}
/// <summary>
/// 地雷
/// </summary>
public class DiLeiOperator : ReflexiveTrapOperator
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit)
    {
        //非英雄，非可反击单位，非近战，不产出伤害
        if (offense.Style.ArmedType < 0 || offense.Style.CounterStyle == 0 || offense.Style.CombatStyle == 1)
            return null;
        if (Hp > 0) return null;
        var explode = PieceAction.Instance(Pos);
        var damage = Card.damage * 0.01f * DataTable.GetGameValue(9);
        offense.Respond(this, explode, CombatFactor.InstanceDamage(damage));
        return explode;
    }
}
/// <summary>
/// 石墙
/// </summary>
public class ShiQiangOperator : TrapOperator
{
    private readonly CombatFactor[] offensiveFactors = new CombatFactor[0];
    private readonly IEnumerable<PieceOperator> targets = new PieceOperator[0];

    protected override IEnumerable<PieceOperator> GetTargets()
    {
        return targets;
    }

    protected override CombatFactor[] GetOffensiveFactors()
    {
        return offensiveFactors;
    }

    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit) => null;

    protected override PieceAction OnDeathTrigger(PieceOperator offense, PieceHit hit) => null;
}
/// <summary>
/// 八阵图
/// </summary>
public class BaZhenTuOperator : ReflexiveTrapOperator
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit)
    {
        if (IsGongChengChe(offense)) return null;
        var stun = PieceAction.Instance(Pos);
        offense.Respond(this, stun,
            CombatFactor.InstanceOffendState(FightState.Cons.Stunned, DataTable.GetGameValue(133)));
        return stun;
    }
}
/// <summary>
/// 金锁阵
/// </summary>
public class JinSuoZhenOperator : ReflexiveTrapOperator
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit)
    {
        if (IsGongChengChe(offense)) return null;
        var move = PieceAction.Instance(Pos);
        offense.Respond(this, move,
            CombatFactor.InstanceOffendState(FightState.Cons.Imprisoned, DataTable.GetGameValue(10)));
        return move;
    }
}
/// <summary>
/// 鬼兵阵
/// </summary>
public class GuiBingZhenOperator : ReflexiveTrapOperator
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit)
    {
        if (IsGongChengChe(offense)) return null;
        var move = PieceAction.Instance(Pos);
        offense.Respond(this, move,
            CombatFactor.InstanceOffendState(FightState.Cons.Cowardly, DataTable.GetGameValue(11)));
        return move;

    }
}
/// <summary>
/// 火墙
/// </summary>
public class FireWallOperator : ReflexiveTrapOperator
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit)
    {
        if (IsGongChengChe(offense)) return null;
        var move = PieceAction.Instance(Pos);
        offense.Respond(this, move,
            CombatFactor.InstanceOffendState(FightState.Cons.Burned, DataTable.GetGameValue(12)));
        return move;

    }
}
/// <summary>
/// 毒泉
/// </summary>
public class PoisonSpringOperator : ReflexiveTrapOperator
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit)
    {
        if (IsGongChengChe(offense)) return null;
        var move = PieceAction.Instance(Pos);
        offense.Respond(this, move,
            CombatFactor.InstanceOffendState(FightState.Cons.Poisoned, DataTable.GetGameValue(13)));
        return move;

    }
}
/// <summary>
/// 刀墙
/// </summary>
public class BladeWallOperator : ReflexiveTrapOperator
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit)
    {
        if (IsGongChengChe(offense)) return null;
        var move = PieceAction.Instance(Pos);
        offense.Respond(this, move,
            CombatFactor.InstanceOffendState(FightState.Cons.Bleed, DataTable.GetGameValue(14)));
        return move;
    }
}
/// <summary>
/// 金币宝箱
/// </summary>
public class TreasureOperator : DeathTriggerTrapOperator
{
    private readonly CombatFactor[] offensiveFactors = new CombatFactor[0];
    private readonly IEnumerable<PieceOperator> targets = new PieceOperator[0];

    protected override IEnumerable<PieceOperator> GetTargets()
    {
        return targets;
    }

    protected override CombatFactor[] GetOffensiveFactors()
    {
        return offensiveFactors;
    }

    protected override PieceAction OnDeathTrigger(PieceOperator offense, PieceHit hit)
    {
        var action = PieceAction.Instance(Pos);
        action.Triggers.Add(PieceTrigger.Instance(PieceTrigger.Gold, DataTable.EnemyUnit[Card.unitId].GoldReward));
        return action;
    }
}
/// <summary>
/// 战役宝箱
/// </summary>
public class WarChestOperator : DeathTriggerTrapOperator
{
    private readonly CombatFactor[] offensiveFactors = new CombatFactor[0];
    private readonly IEnumerable<PieceOperator> targets = new PieceOperator[0];

    protected override IEnumerable<PieceOperator> GetTargets()
    {
        return targets;
    }

    protected override CombatFactor[] GetOffensiveFactors()
    {
        return offensiveFactors;
    }

    protected override PieceAction OnDeathTrigger(PieceOperator offense, PieceHit hit)
    {
        var action = PieceAction.Instance(Pos);
        action.Triggers.Add(PieceTrigger.Instance(PieceTrigger.WarChest, DataTable.EnemyUnit[Card.unitId].WarChest));
        return action;
    }
}
