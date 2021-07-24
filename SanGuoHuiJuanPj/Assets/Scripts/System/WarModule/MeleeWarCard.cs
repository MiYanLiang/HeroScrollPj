using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO.Pipelines;
using System.Linq;
using CorrelateLib;

public abstract class PieceOperator
{
    private static Random random = new Random();
    /// <summary>
    /// 棋子当前的位置
    /// </summary>
    public abstract int Pos { get; }
    /// <summary>
    /// 棋子当前的血量
    /// </summary>
    public abstract int Hp { get; }
    public abstract AttackStyle Style { get; }

    /// <summary>
    /// 当行动的时候获取目标的方法
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<PieceOperator> GetTargets();

    /// <summary>
    /// 生成伤害/治疗的方法
    /// </summary>
    /// <returns></returns>
    protected abstract CombatFactor[] GetOffensiveFactors();

    /// <summary>
    /// Random Range
    /// </summary>
    /// <param name="ratio">Ratio in 100</param>
    /// <param name="range">Max Included</param>
    /// <returns></returns>
    protected static bool IsRandomPass(int ratio, int range = 100) => random.Next(0, range) <= ratio;

    /// <summary>
    /// 主行动
    /// </summary>
    /// <returns></returns>
    public virtual PieceAction Invoke()
    {
        var cards = GetTargets();
        var move = PieceAction.Instance(Pos);
        cards.ToList().ForEach(card => card.Respond(this, move, GetOffensiveFactors()));
        return move;
    }

    //被动处理方法。当行动棋子提交伤害/治疗的时候对应方法
    public void Respond(PieceOperator offense, PieceAction action,params CombatFactor[] factors)
    {
        var damages = factors.Where(f => f.Kind == CombatFactor.Kinds.Damage).ToArray();
        var hit = PieceHit.Instance(Pos, Hp,
            damages.Select(d => DamageRespond(offense, d))
                .Concat(factors.Except(damages).Select(f => FavorRespond(offense, f))).ToArray());
        var counter = CounterMove(offense, hit);
        if (counter != null)
        {
            hit.Moves.Add(counter);
        }
        action.Hits = new[] {hit};
    }

    protected abstract PieceAction CounterMove(PieceOperator offense, PieceHit hit);

    /// <summary>
    /// 被赋予非直接伤害类的方法(例：治疗，上状态)
    /// </summary>
    /// <param name="offense"></param>
    /// <param name="heal"></param>
    /// <returns></returns>
    protected virtual CombatFactor FavorRespond(PieceOperator offense, CombatFactor heal) => heal;

    /// <summary>
    /// 被伤害的方法
    /// </summary>
    /// <param name="offense"></param>
    /// <param name="damage"></param>
    /// <returns></returns>
    protected virtual CombatFactor DamageRespond(PieceOperator offense, CombatFactor damage) => damage;

}

public abstract class ChessmanOperator : PieceOperator
{
    public FightCardData Card { get; private set; }
    public override AttackStyle Style => attackStyle;
    public override int Hp => Card.Hp.Value;
    public override int Pos => Card.posIndex;
    protected static int[] FrontRows { get; } = { 0, 1, 2, 3, 4 };
    private AttackStyle attackStyle;

    public void Init(FightCardData card, AttackStyle style)
    {
        Card = card;
        attackStyle = style;
    }
    
    protected override PieceAction CounterMove(PieceOperator offense, PieceHit hit)
    {
        var totalDmg = hit.Factors.Where(f => f.Kind == CombatFactor.Kinds.Damage).Sum(f => f.Total);
        var totalHeal = hit.Factors.Where(f => f.Kind == CombatFactor.Kinds.Heal).Sum(f => f.Total);
        if (Hp + totalHeal - totalDmg > 0)
        {
            return offense.Style.CounterStyle == 0 ? null : OnCounter(offense, hit);
        }
        return OnDeathTrigger(offense, hit);
    }
    /// <summary>
    /// 当被攻击，反击的方法，如果没有反击返回 = null
    /// </summary>
    /// <param name="offense"></param>
    /// <param name="hit"></param>
    /// <returns></returns>
    protected abstract PieceAction OnCounter(PieceOperator offense, PieceHit hit);
    /// <summary>
    /// 当死亡触发的方法，如果没有返回 = null
    /// </summary>
    /// <param name="offense"></param>
    /// <param name="hit"></param>
    /// <returns></returns>
    protected abstract PieceAction OnDeathTrigger(PieceOperator offense, PieceHit hit);
}

public class WarOperatorManager
{
    //public WarCard GetWarCard(FightCardData card)
    //{
    //    switch (DataTable.Hero[card.cardId].MilitaryUnitTableId)
    //    {
            
    //    }
    //}
    private const int TowerArmedType = -1;
    private const int TrapArmedType = -2;
    private const int RangeCombatStyle = 1;
    private const int MeleeCombatStyle = 0;
    private const int SpecialCombatStyle = 2;
    private const int NoCounter = 0;
    private const int BasicCounterStyle = 1;

    private static AttackStyle TowerRangeNoCounter = AttackStyle.Instance(TowerArmedType, TowerArmedType, RangeCombatStyle, NoCounter);
    private static AttackStyle TrapSpecialNoCounter = AttackStyle.Instance(TrapArmedType, TrapArmedType, SpecialCombatStyle, NoCounter);

    public static PieceOperator GetWarCard(FightCardData card)
    {

        switch ((GameCardType)card.cardType)
        {
            case GameCardType.Hero:
                return InstanceHero(card);
            case GameCardType.Tower:
                return InstanceTower(card);
            case GameCardType.Trap:
                return InstanceTrap(card);
            case GameCardType.Spell:
            case GameCardType.Base:
            case GameCardType.Soldier:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static PieceOperator InstanceHero(FightCardData card)
    {
        var op = null;
        switch (card.cardId)
        {
            case 1 : op = new  ; break;
            default: op = new GeneralHeroOperator(); break;
        }
    }

    private static PieceOperator InstanceTrap(FightCardData card)
    {
        TrapOperator op = null;
        switch (card.cardId)
        {
            case 0: op = new JuMaOperator(); break;
            case 1: op = new DiLeiOperator(); break;
            case 2: op = new ShiQiangOperator(); break;
            case 3: op = new BaZhenTuOperator(); break;
            case 4: op = new JinSuoZhenOperator(); break;
            case 5: op = new GuiBingZhenOperator(); break;
            case 6: op = new FireWallOperator(); break;
            case 7: op = new PoisonSpringOperator(); break;
            case 8: op = new BladeWallOperator(); break;
            case 9: op = new GunShiOperator(); break;
            case 10: op = new GunMuOperator(); break;
            case 11: op = new TreasureOperator(); break;
            case 12: op = new JuMaOperator(); break;
            default: op = new BlankTrapOperator(); break;
        }

        op.Init(card, TrapSpecialNoCounter);
        return op;
    }

    private static PieceOperator InstanceTower(FightCardData card)
    {
        TowerOperator op = null;
        switch (card.cardId)
        {
                //营寨
            case 0: op = new YingZhaiOperator(); break;
                //投石台
            case 1: op = new TouShiTaiOperator(); break;
                //奏乐台
            case 2: op = new ZouYueTaiOperator(); break;
                //箭楼
            case 3: op = new JianLouOperator(); break;
                //轩辕台
            case 6: op = new XuanYuanTaiOperator(); break;
            default: op = new BlankTowerOperator(); break;
        }
        op.Init(card, TowerRangeNoCounter);
        return op;
    }
}

public abstract class HeroOperator : ChessmanOperator
{
    /// <summary>
    /// 从数值表获取
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    protected static bool IsConfigTriggered(int id) => IsRandomPass(DataTable.GetGameValue(id));
    protected override CombatFactor[] GetOffensiveFactors() => Card.fightState.Imprisoned > 0 ? new[] {CombatFactor.InstanceDamage(Card.damage)} : MilitaryForceDamages();

    protected abstract CombatFactor[] MilitaryForceDamages();

    public override PieceAction Invoke() => Card.fightState.Stunned > 0 ? null : base.Invoke();
}

/// <summary>
/// 无死亡触发武将类别
/// </summary>
public abstract class NonDeathTriggerHero : HeroOperator
{

}
/// <summary>
/// 无反击，无死亡触发武将类别
/// </summary>
public abstract class NonCounterHero : NonDeathTriggerHero
{
    protected override PieceAction OnCounter(PieceOperator offense, PieceHit hit) => null;
    protected override PieceAction OnDeathTrigger(PieceOperator offense, PieceHit hit) => null;
}
/// <summary>
/// 先锋处理器
/// </summary>
public class XianFengOperator : NonCounterHero
{
    protected override IEnumerable<PieceOperator> GetTargets()
    {
        return targets;
    }

    protected override CombatFactor[] MilitaryForceDamages()
    {
        throw new NotImplementedException();
    }
}