using CorrelateLib;

/// <summary>
/// 英雄战斗信息结构
/// </summary>
public class HeroCombatInfo
{
    public static HeroCombatInfo GetInfo(HeroTable hero) => new HeroCombatInfo(hero);
    public int Strength { get; }
    public int Intelligent { get; }
    public int DodgeRatio { get; }
    public int HitPoint { get; }
    public int Speed { get; }
    public int Troop { get; }
    public int Armor { get; }
    public int MagicResist { get; }
    public int CriticalRatio { get; }
    public int RouseRatio { get; }
    public int Rare { get; }

    private HeroCombatInfo(HeroTable h)
    {
        Strength = h.Strength;
        Intelligent = h.Intelligent;
        Speed = h.Speed;
        Troop = h.ForceTableId;
        HitPoint = h.HitPoint;
        Rare = h.Rarity;
        DodgeRatio = h.DodgeRatio;
        Armor = h.ArmorResist;
        CriticalRatio = h.CriticalRatio;
        RouseRatio = h.RouseRatio;
        MagicResist = h.MagicResist;
    }
}