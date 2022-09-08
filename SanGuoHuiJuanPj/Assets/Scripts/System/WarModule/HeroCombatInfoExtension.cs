using System.Collections.Generic;
using Assets.System.WarModule;
using CorrelateLib;
public static class HeroCombatInfoExtension
{
    public static int GetArousedStrength(this HeroTable h, int arouse) => h.Strength + GetArrayValue(h.ArouseStrengths, arouse);
    public static int GetArousedIntelligent(this HeroTable h, int arouse) => h.Intelligent + GetArrayValue(h.ArouseIntelligents, arouse);
    public static int GetArousedHitPointAddOn(this HeroTable h, int arouse) => GetArrayValue(h.ArouseHitPoints, arouse);
    public static int GetArousedSpeed(this HeroTable h, int arouse) => h.Speed + GetArrayValue(h.ArouseSpeeds, arouse);
    public static int GetArousedArmor(this HeroTable h, int arouse) => h.ArmorResist + GetArrayValue(h.ArouseArmors, arouse);
    public static int GetArousedMagicRest(this HeroTable h, int arouse) => h.MagicResist + GetArrayValue(h.ArouseMagicRests, arouse);
    public static int GetArousedDodge(this HeroTable h, int arouse) => h.DodgeRatio + GetArrayValue(h.ArouseDodges, arouse);

    public static int GetDeputyStrength(this IReadOnlyDictionary<int, HeroTable> table, int deputy1Id, int deputy1Level,
        int deputy2Id, int deputy2Level, int deputy3Id, int deputy3Level)
    {
        var d1 = 0;
        var d2 = 0;
        var d3 = 0;
        if (deputy1Id > 0) d1 = table[deputy1Id].DeputyStrengths[deputy1Level];
        if (deputy2Id > 0) d2 = table[deputy2Id].DeputyStrengths[deputy2Level];
        if (deputy3Id > 0) d3 = table[deputy3Id].DeputyStrengths[deputy3Level];
        return d1 + d2 + d3;
    }
    public static int GetDeputyIntelligent(this IReadOnlyDictionary<int, HeroTable> table, int deputy1Id, int deputy1Level,
        int deputy2Id, int deputy2Level, int deputy3Id, int deputy3Level)
    {
        var d1 = 0;
        var d2 = 0;
        var d3 = 0;
        if (deputy1Id > 0) d1 = table[deputy1Id].DeputyIntelligents[deputy1Level];
        if (deputy2Id > 0) d2 = table[deputy2Id].DeputyIntelligents[deputy2Level];
        if (deputy3Id > 0) d3 = table[deputy3Id].DeputyIntelligents[deputy3Level];
        return d1 + d2 + d3;
    }
    public static int GetDeputyHitPoint(this IReadOnlyDictionary<int, HeroTable> table, int deputy1Id, int deputy1Level,
        int deputy2Id, int deputy2Level, int deputy3Id, int deputy3Level)
    {
        var d1 = 0;
        var d2 = 0;
        var d3 = 0;
        if (deputy1Id > 0) d1 = table[deputy1Id].DeputyHitPoints[deputy1Level];
        if (deputy2Id > 0) d2 = table[deputy2Id].DeputyHitPoints[deputy2Level];
        if (deputy3Id > 0) d3 = table[deputy3Id].DeputyHitPoints[deputy3Level];
        return d1 + d2 + d3;
    }
    public static int GetDeputySpeed(this IReadOnlyDictionary<int, HeroTable> table, int deputy1Id, int deputy1Level,
        int deputy2Id, int deputy2Level, int deputy3Id, int deputy3Level)
    {
        var d1 = 0;
        var d2 = 0;
        var d3 = 0;
        if (deputy1Id > 0) d1 = table[deputy1Id].DeputySpeeds[deputy1Level];
        if (deputy2Id > 0) d2 = table[deputy2Id].DeputySpeeds[deputy2Level];
        if (deputy3Id > 0) d3 = table[deputy3Id].DeputySpeeds[deputy3Level];
        return d1 + d2 + d3;
    }
    public static int GetDeputyArmor(this IReadOnlyDictionary<int, HeroTable> table, int deputy1Id, int deputy1Level,
        int deputy2Id, int deputy2Level, int deputy3Id, int deputy3Level)
    {
        var d1 = 0;
        var d2 = 0;
        var d3 = 0;
        if (deputy1Id > 0) d1 = table[deputy1Id].DeputyArmors[deputy1Level];
        if (deputy2Id > 0) d2 = table[deputy2Id].DeputyArmors[deputy2Level];
        if (deputy3Id > 0) d3 = table[deputy3Id].DeputyArmors[deputy3Level];
        return d1 + d2 + d3;
    }
    public static int GetDeputyMagicRest(this IReadOnlyDictionary<int, HeroTable> table, int deputy1Id, int deputy1Level,
        int deputy2Id, int deputy2Level, int deputy3Id, int deputy3Level)
    {
        var d1 = 0;
        var d2 = 0;
        var d3 = 0;
        if (deputy1Id > 0) d1 = table[deputy1Id].DeputyMagicRests[deputy1Level];
        if (deputy2Id > 0) d2 = table[deputy2Id].DeputyMagicRests[deputy2Level];
        if (deputy3Id > 0) d3 = table[deputy3Id].DeputyMagicRests[deputy3Level];
        return d1 + d2 + d3;
    }
    public static int GetDeputyDodge(this IReadOnlyDictionary<int, HeroTable> table, int deputy1Id, int deputy1Level,
        int deputy2Id, int deputy2Level, int deputy3Id, int deputy3Level)
    {
        var d1 = 0;
        var d2 = 0;
        var d3 = 0;
        if (deputy1Id > 0) d1 = table[deputy1Id].DeputyDodges[deputy1Level];
        if (deputy2Id > 0) d2 = table[deputy2Id].DeputyDodges[deputy2Level];
        if (deputy3Id > 0) d3 = table[deputy3Id].DeputyDodges[deputy3Level];
        return d1 + d2 + d3;
    }

    private static int GetArrayValue(int[] array, int arouse)
    {
        if (arouse == 0) return 0;
        if (array == null || array.Length == 0) return 0;
        var index = arouse - 1;
        return array[index];
    }

}