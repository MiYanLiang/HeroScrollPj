using System.Collections.Generic;
using System.Linq;
using CorrelateLib;

/// <summary>
/// 塔单位棋子处理器
/// </summary>
public abstract class TowerOperator : ChessmanOperator
{
    protected override ChessPosProcess OnCounter(PieceOperator offense, PieceAction action) => null;

    protected override ChessPosProcess OnDeathTrigger(PieceOperator offense, PieceAction action) => null;
    protected override PieceTrigger[] OnActionDoneTriggers() => null;
}

public class BlankTowerOperator : TowerOperator
{
    private readonly CombatFactor[] getOffensiveFactors = new CombatFactor[0];
    private readonly IEnumerable<PieceOperator> targets = new PieceOperator[0];

    protected override IEnumerable<PieceOperator> GetTargets() => targets;

    protected override CombatFactor[] GetOffensiveFactors()=> getOffensiveFactors;
}
/// <summary>
/// 营寨
/// </summary>
public class YingZhaiOperator : TowerOperator
{
    protected override CombatFactor[] GetOffensiveFactors()
    {
        return new[] {CombatFactor.InstanceHeal(Chessman.damage, Chessman.cardDamageType)};
    }

    protected override IEnumerable<PieceOperator> GetTargets()
    {
        //获取最少血的友军目标
        var target = FightForManager.instance.GetFriendlyNeighbors(Pos, Chessman, card =>
            card != null && card.IsAlive && card.CardType == GameCardType.Hero).OrderBy(c => c.Hp.Rate()).FirstOrDefault();
        return target == null ? new PieceOperator[0] : new[] {ChessOperatorManager.GetWarCard(target)};
    }
}
/// <summary>
/// 投石台
/// </summary>
public class TouShiTaiOperator : TowerOperator
{
    protected override CombatFactor[] GetOffensiveFactors() =>
        new[] {CombatFactor.InstanceDamage(Chessman.damage, Chessman.cardDamageType)};

    // 获取对位与周围的单位

    protected override IEnumerable<PieceOperator> GetTargets() =>
        FightForManager.instance
            .GetNeighbors(Pos, !Chessman.isPlayerCard, c => c != null && c.IsAlive)
            .Select(ChessOperatorManager.GetWarCard).ToArray();
}
/// <summary>
/// 奏乐台
/// </summary>
public class ZouYueTaiOperator : TowerOperator
{
    protected override CombatFactor[] GetOffensiveFactors() => new[] {CombatFactor.InstanceHeal(Chessman.damage)};

    protected override IEnumerable<PieceOperator> GetTargets() =>
        FightForManager.instance.GetFriendlyNeighbors(Pos, Chessman,
                c => c != null && c.IsAlive && c.cardObj.CardInfo.Type == GameCardType.Hero)
            .Select(ChessOperatorManager.GetWarCard).ToArray();
}
/// <summary>
/// 箭楼
/// </summary>
public class JianLouOperator : TowerOperator
{
    //17, 箭楼远射伤害百分比

    protected override CombatFactor[] GetOffensiveFactors() =>
        new[]
            {CombatFactor.InstanceDamage(DataTable.GetGameValue(17) / 100f * Chessman.damage)};

    protected override IEnumerable<PieceOperator> GetTargets()
    {
        var cards = FightForManager.instance.GetCardList(!Chessman.isPlayerCard);
        var maxTargets = DataTable.GetGameValue(18); //箭楼攻击数量
        var pick = cards.Count > maxTargets ? maxTargets : cards.Count;
        return cards.Where(c => c != null && c.IsAlive).Select(RandomElement.Instance).Pick(pick)
            .Select(e => ChessOperatorManager.GetWarCard(e.Card))
            .ToArray();
    }

    class RandomElement : IWeightElement
    {
        public static RandomElement Instance(FightCardData card) => new RandomElement(card);
        public int Weight => 1;
        public FightCardData Card { get; }

        public RandomElement(FightCardData card)
        {
            Card = card;
        }
    }
}
/// <summary>
/// 轩辕台
/// </summary>
public class XuanYuanTaiOperator : TowerOperator
{
    protected override CombatFactor[] GetOffensiveFactors() =>
        new[]
            {CombatFactor.InstanceDefendState(FightState.Cons.Withstand, Chessman.damage)};

    protected override IEnumerable<PieceOperator> GetTargets() =>
        FightForManager.instance.GetFriendlyNeighbors(Pos, Chessman,
            c => c != null && c.IsAlive && c.fightState.Withstand < 1).Select(ChessOperatorManager.GetWarCard).ToArray();
}