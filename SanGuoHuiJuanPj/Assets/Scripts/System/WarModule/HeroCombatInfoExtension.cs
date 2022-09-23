using System.Collections.Generic;
using Assets.System.WarModule;
using CorrelateLib;

namespace System.WarModule
{
    public static class HeroCombatInfoExtension
    {
        public static int GetArousedStrength(this HeroTable h, int level, int arouse) =>
            CombatStyle.DamageFormula(h.Strength, level) + GetArouseArrayValue(h.ArouseStrengths, arouse);

        public static int GetArousedIntelligent(this HeroTable h, int arouse) =>
             h.Intelligent + GetArouseArrayValue(h.ArouseIntelligents, arouse);

        public static int GetArousedHitPoint(this HeroTable h, int level, int arouse) =>
            CombatStyle.HitPointFormula(h.HitPoint, level) + GetArouseArrayValue(h.ArouseHitPoints, arouse);

        public static int GetArousedSpeed(this HeroTable h, int arouse) =>
            h.Speed + GetArouseArrayValue(h.ArouseSpeeds, arouse);

        public static int GetArousedArmor(this HeroTable h, int arouse) =>
            h.ArmorResist + GetArouseArrayValue(h.ArouseArmors, arouse);

        public static int GetArousedMagicRest(this HeroTable h, int arouse) =>
            h.MagicResist + GetArouseArrayValue(h.ArouseMagicRests, arouse);

        public static int GetArousedDodge(this HeroTable h, int arouse) =>
            h.DodgeRatio + GetArouseArrayValue(h.ArouseDodges, arouse);

        public static int GetDeputyStrength(this IReadOnlyDictionary<int, HeroTable> table, 
            int deputy1Id, int deputy1Level,
            int deputy2Id, int deputy2Level, 
            int deputy3Id, int deputy3Level,
            int deputy4Id, int deputy4Level
        )
        {
            var d1 = table.GetDeputy(deputy1Id, deputy1Level, t => t.DeputyStrengths);
            var d2 = table.GetDeputy(deputy2Id, deputy2Level, t => t.DeputyStrengths);
            var d3 = table.GetDeputy(deputy3Id, deputy3Level, t => t.DeputyStrengths);
            var d4 = table.GetDeputy(deputy4Id, deputy4Level, t => t.DeputyStrengths);
            return d1 + d2 + d3 + d4;
        }
        public static int GetDeputyIntelligent(this IReadOnlyDictionary<int, HeroTable> table,
            int deputy1Id, int deputy1Level,
            int deputy2Id, int deputy2Level,
            int deputy3Id, int deputy3Level,
            int deputy4Id, int deputy4Level
        )
        {
            var d1 = table.GetDeputy(deputy1Id, deputy1Level, t => t.DeputyIntelligents);
            var d2 = table.GetDeputy(deputy2Id, deputy2Level, t => t.DeputyIntelligents);
            var d3 = table.GetDeputy(deputy3Id, deputy3Level, t => t.DeputyIntelligents);
            var d4 = table.GetDeputy(deputy4Id, deputy4Level, t => t.DeputyIntelligents);
            return d1 + d2 + d3 + d4;
        }
        public static int GetDeputyHitPoint(this IReadOnlyDictionary<int, HeroTable> table,
            int deputy1Id, int deputy1Level,
            int deputy2Id, int deputy2Level,
            int deputy3Id, int deputy3Level,
            int deputy4Id, int deputy4Level
        )
        {
            var d1 = table.GetDeputy(deputy1Id, deputy1Level, t => t.DeputyHitPoints);
            var d2 = table.GetDeputy(deputy2Id, deputy2Level, t => t.DeputyHitPoints);
            var d3 = table.GetDeputy(deputy3Id, deputy3Level, t => t.DeputyHitPoints);
            var d4 = table.GetDeputy(deputy4Id, deputy4Level, t => t.DeputyHitPoints);
            return d1 + d2 + d3 + d4;
        }
        public static int GetDeputySpeed(this IReadOnlyDictionary<int, HeroTable> table,
            int deputy1Id, int deputy1Level,
            int deputy2Id, int deputy2Level,
            int deputy3Id, int deputy3Level,
            int deputy4Id, int deputy4Level
        )
        {
            var d1 = table.GetDeputy(deputy1Id, deputy1Level, t => t.DeputySpeeds);
            var d2 = table.GetDeputy(deputy2Id, deputy2Level, t => t.DeputySpeeds);
            var d3 = table.GetDeputy(deputy3Id, deputy3Level, t => t.DeputySpeeds);
            var d4 = table.GetDeputy(deputy4Id, deputy4Level, t => t.DeputySpeeds);
            return d1 + d2 + d3 + d4;
        }
        public static int GetDeputyArmor(this IReadOnlyDictionary<int, HeroTable> table,
            int deputy1Id, int deputy1Level,
            int deputy2Id, int deputy2Level,
            int deputy3Id, int deputy3Level,
            int deputy4Id, int deputy4Level
        )
        {
            var d1 = table.GetDeputy(deputy1Id, deputy1Level, t => t.DeputyArmors);
            var d2 = table.GetDeputy(deputy2Id, deputy2Level, t => t.DeputyArmors);
            var d3 = table.GetDeputy(deputy3Id, deputy3Level, t => t.DeputyArmors);
            var d4 = table.GetDeputy(deputy4Id, deputy4Level, t => t.DeputyArmors);
            return d1 + d2 + d3 + d4;
        }
        public static int GetDeputyMagicRest(this IReadOnlyDictionary<int, HeroTable> table,
            int deputy1Id, int deputy1Level,
            int deputy2Id, int deputy2Level,
            int deputy3Id, int deputy3Level,
            int deputy4Id, int deputy4Level
        )
        {
            var d1 = table.GetDeputy(deputy1Id, deputy1Level, t => t.DeputyMagicRests);
            var d2 = table.GetDeputy(deputy2Id, deputy2Level, t => t.DeputyMagicRests);
            var d3 = table.GetDeputy(deputy3Id, deputy3Level, t => t.DeputyMagicRests);
            var d4 = table.GetDeputy(deputy4Id, deputy4Level, t => t.DeputyMagicRests);
            return d1 + d2 + d3 + d4;
        }
        public static int GetDeputyDodge(this IReadOnlyDictionary<int, HeroTable> table,
            int deputy1Id, int deputy1Level,
            int deputy2Id, int deputy2Level,
            int deputy3Id, int deputy3Level,
            int deputy4Id, int deputy4Level
        )
        {
            var d1 = table.GetDeputy(deputy1Id, deputy1Level, t => t.DeputyDodges);
            var d2 = table.GetDeputy(deputy2Id, deputy2Level, t => t.DeputyDodges);
            var d3 = table.GetDeputy(deputy3Id, deputy3Level, t => t.DeputyDodges);
            var d4 = table.GetDeputy(deputy4Id, deputy4Level, t => t.DeputyDodges);
            return d1 + d2 + d3 + d4;
        }
        private static int GetDeputy(this IReadOnlyDictionary<int, HeroTable> table, int index, int level,
            Func<HeroTable, int[]> getPropFunc)
        {
            var array = !table.ContainsKey(index) ? Array.Empty<int>() : getPropFunc(table[index]);
            return GetValueFromArrayOrDefault(array, level - 1);
        }
        private static int GetValueFromArrayOrDefault(int[] array, int index)
        {
            if (index < 0) return 0;
            if (index >= array.Length) return 0;
            var result = array[index];
            return result;
        }

        private static int GetArouseArrayValue(int[] array, int arouse)
        {
            if (arouse == 0) return 0;
            if (array == null || array.Length == 0) return 0;
            var index = arouse - 1;
            return array[index];
        }
    }
}