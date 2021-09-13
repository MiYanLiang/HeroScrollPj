using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    [Serializable]
//战斗状态类
    public class CardState
    {
        public static Cons[] NegativeBuffs { get; } = new[]
        {
            Cons.Stunned,
            Cons.Bleed,
            Cons.Poison,
            Cons.Burn,
            Cons.Imprisoned,
            Cons.Cowardly,
            Cons.Disarmed
        };
        public static bool IsNegativeBuff(Cons con)
        {
            return con == Cons.Stunned ||
                   con == Cons.Bleed ||
                   con == Cons.Poison ||
                   con == Cons.Burn ||
                   con == Cons.Imprisoned ||
                   con == Cons.Cowardly ||
                   con == Cons.Disarmed;
        }
        private static int[] _consInts=Enum.GetValues(typeof(Cons)).Cast<int>().ToArray();
        public static Cons[] ConsArray { get; } = Enum.GetValues(typeof(Cons)).Cast<Cons>().ToArray();
        public static string IconName(Cons con)
        {
            switch (con)
            {
                case Cons.Stunned:
                    return StringNameStatic.StateIconPath_dizzy;
                case Cons.Shield:
                    return StringNameStatic.StateIconPath_withStand;
                case Cons.Invincible:
                    return StringNameStatic.StateIconPath_invincible;
                case Cons.Bleed:
                    return StringNameStatic.StateIconPath_bleed;
                case Cons.Poison:
                    return StringNameStatic.StateIconPath_poisoned;
                case Cons.Burn:
                    return StringNameStatic.StateIconPath_burned;
                case Cons.Stimulate:
                    return StringNameStatic.StateIconPath_willFight;
                case Cons.Imprisoned:
                    return StringNameStatic.StateIconPath_imprisoned;
                case Cons.Cowardly:
                    return StringNameStatic.StateIconPath_cowardly;
                case Cons.StrengthUp:
                    return StringNameStatic.StateIconPath_zhangutaiAddtion;
                case Cons.DodgeUp:
                    return StringNameStatic.StateIconPath_fengShenTaiAddtion;
                case Cons.CriticalUp:
                    return StringNameStatic.StateIconPath_pilitaiAddtion;
                case Cons.RouseUp:
                    return StringNameStatic.StateIconPath_langyataiAddtion;
                case Cons.DefendUp:
                    return StringNameStatic.StateIconPath_fenghuotaiAddtion;
                case Cons.DeathFight:
                    return StringNameStatic.StateIconPath_deathFight;
                case Cons.Disarmed:
                    return StringNameStatic.StateIconPath_removeArmor;
                case Cons.Neizhu:
                    return StringNameStatic.StateIconPath_neizhu;
                case Cons.ShenZhu:
                    return StringNameStatic.StateIconPath_shenzhu;
                case Cons.EaseShield:
                    return StringNameStatic.StateIconPath_shield;
                case Cons.Forge:
                    return StringNameStatic.StateIconPath_miWuZhenAddtion;
                default:
                    throw new ArgumentOutOfRangeException(nameof(con), con, null);
            }
        }

        public static bool IsDamageBuff(Cons con)
        {
            switch (con)
            {
                case Cons.Bleed:
                case Cons.Poison:
                case Cons.Burn:
                    return true;
                case Cons.Stunned:
                case Cons.Shield:
                case Cons.Invincible:
                case Cons.Stimulate:
                case Cons.Imprisoned:
                case Cons.Cowardly:
                case Cons.StrengthUp:
                case Cons.DodgeUp:
                case Cons.CriticalUp:
                case Cons.RouseUp:
                case Cons.DefendUp:
                case Cons.DeathFight:
                case Cons.Disarmed:
                case Cons.Neizhu:
                case Cons.ShenZhu:
                case Cons.EaseShield:
                case Cons.Forge:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(con), con, null);
            }
        }
        public enum Cons
        {
            /// <summary>
            /// 眩晕
            /// </summary>
            Stunned = 1,
            /// <summary>
            /// 护盾
            /// </summary>
            Shield = 2,
            /// <summary>
            /// 无敌
            /// </summary>
            Invincible = 3,
            /// <summary>
            /// 流血
            /// </summary>
            Bleed = 4,
            /// <summary>
            /// 毒
            /// </summary>
            Poison = 5,
            /// <summary>
            /// 灼烧
            /// </summary>
            Burn = 6,
            /// <summary>
            /// 战意
            /// </summary>
            Stimulate = 7,
            /// <summary>
            /// 禁锢
            /// </summary>
            Imprisoned = 8,
            /// <summary>
            /// 胆怯
            /// </summary>
            Cowardly = 9,
            /// <summary>
            /// 攻击力提升
            /// </summary>
            StrengthUp = 10,
            /// <summary>
            /// 闪避提升
            /// </summary>
            DodgeUp = 11,
            /// <summary>
            /// 霹雳台
            /// </summary>
            CriticalUp = 12,
            /// <summary>
            /// 琅琊台
            /// </summary>
            RouseUp = 13,
            /// <summary>
            /// 烽火台
            /// </summary>
            DefendUp = 14,
            /// <summary>
            /// 死战
            /// </summary>
            DeathFight = 15,
            /// <summary>
            /// 卸甲
            /// </summary>
            Disarmed = 16,
            /// <summary>
            /// 内助
            /// </summary>
            Neizhu = 17,
            /// <summary>
            /// 神助
            /// </summary>
            ShenZhu = 18,
            /// <summary>
            /// 缓冲盾
            /// </summary>
            EaseShield = 19,
            /// <summary>
            /// 迷雾
            /// </summary>
            Forge = 20
        }
    
        public CardState() => data = _consInts.ToDictionary(s => s, _ => 0);
        private Dictionary<int, int> data;
        public IReadOnlyDictionary<int, int> Data => data;
        /// <summary>
        /// 眩晕回合数
        /// </summary>
        [JsonIgnore] public int Stunned { get=>data[1]; set=>data[1] = value; }

        /// <summary>
        /// 护盾层数
        /// </summary>
        [JsonIgnore] public int Shield { get => data[2]; set => data[2] = value; }

        /// <summary>
        /// 无敌回合
        /// </summary>
        [JsonIgnore] public int Invincible { get => data[3]; set => data[3] = value; }

        /// <summary>
        /// 流血层数
        /// </summary>
        [JsonIgnore] public int Bleed { get => data[4]; set => data[4] = value; }

        /// <summary>
        /// 中毒回合
        /// </summary>
        [JsonIgnore] public int Poison { get => data[5]; set => data[5] = value; }

        /// <summary>
        /// 灼烧回合
        /// </summary>
        [JsonIgnore] public int Burn { get => data[6]; set => data[6] = value; }

        /// <summary>
        /// 战意层数
        /// </summary>
        [JsonIgnore] public int Stimulate { get => data[7]; set => data[7] = value; }

        /// <summary>
        /// 禁锢层数
        /// </summary>
        [JsonIgnore] public int Imprisoned { get => data[8]; set => data[8] = value; }

        /// <summary>
        /// 怯战层数
        /// </summary>
        [JsonIgnore] public int Cowardly { get => data[9]; set => data[9] = value; }

        /// <summary>
        /// 战鼓台-伤害加成
        /// </summary>
        [JsonIgnore] public int StrengthUp { get => data[10]; set => data[10] = value; }

        /// <summary>
        /// 风神台-闪避加成
        /// </summary>
        [JsonIgnore] public int DodgeUp { get => data[11]; set => data[11] = value; }

        /// <summary>
        /// 霹雳台-暴击加成
        /// </summary>
        [JsonIgnore] public int CriticalUp { get => data[12]; set => data[12] = value; }

        /// <summary>
        /// 狼牙台-会心加成
        /// </summary>
        [JsonIgnore] public int RouseUp { get => data[13]; set => data[13] = value; }

        /// <summary>
        /// 烽火台-免伤加成
        /// </summary>
        [JsonIgnore] public int FengHuoTaiAddOn { get => data[14]; set => data[14] = value; }

        /// <summary>
        /// 死战回合
        /// </summary>
        [JsonIgnore] public int DeathFight { get => data[15]; set => data[15] = value; }

        /// <summary>
        /// 卸甲回合
        /// </summary>
        [JsonIgnore] public int Unarmed { get => data[16]; set => data[16] = value; }

        /// <summary>
        /// 内助回合
        /// </summary>
        [JsonIgnore] public int Neizhu { get => data[17]; set => data[17] = value; }

        /// <summary>
        /// 神助回合
        /// </summary>
        [JsonIgnore] public int ShenZhu { get => data[18]; set => data[18] = value; }

        /// <summary>
        /// 防护盾数值
        /// </summary>
        [JsonIgnore] public int ExtendedHp { get => data[19]; set => data[19] = value; }

        /// <summary>
        /// 迷雾阵-远程闪避加成
        /// </summary>
        [JsonIgnore] public int MiWuZhenAddOn { get => data[20]; set => data[20] = value; }

        public void AddState(Cons con,int value)
        {
            switch (con)
            {
                case Cons.Stunned:
                    Stunned += MinZeroAlign(Stunned);
                    break;
                case Cons.Shield:
                    Shield += MinZeroAlign(Shield);
                    break;
                case Cons.Invincible:
                    Invincible += MinZeroAlign(Invincible);
                    break;
                case Cons.Bleed:
                    Bleed += MinZeroAlign(Bleed);
                    break;
                case Cons.Poison:
                    Poison += MinZeroAlign(Poison);
                    break;
                case Cons.Burn:
                    Burn += MinZeroAlign(Burn);
                    break;
                case Cons.Stimulate:
                    Stimulate += MinZeroAlign(Burn);
                    break;
                case Cons.Imprisoned:
                    Imprisoned += MinZeroAlign(Imprisoned);
                    break;
                case Cons.Cowardly:
                    Cowardly += MinZeroAlign(Cowardly);
                    break;
                case Cons.StrengthUp:
                    StrengthUp += MinZeroAlign(StrengthUp);
                    break;
                case Cons.DodgeUp:
                    DodgeUp += MinZeroAlign(DodgeUp);
                    break;
                case Cons.CriticalUp:
                    CriticalUp += MinZeroAlign(CriticalUp);
                    break;
                case Cons.RouseUp:
                    RouseUp += MinZeroAlign(RouseUp);
                    break;
                case Cons.DefendUp:
                    FengHuoTaiAddOn += MinZeroAlign(DodgeUp);
                    break;
                case Cons.DeathFight:
                    DeathFight += MinZeroAlign(DeathFight);
                    break;
                case Cons.Disarmed:
                    Unarmed += MinZeroAlign(Unarmed);
                    break;
                case Cons.Neizhu:
                    Neizhu += MinZeroAlign(Neizhu);
                    break;
                case Cons.ShenZhu:
                    ShenZhu += MinZeroAlign(ShenZhu);
                    break;
                case Cons.EaseShield:
                    ExtendedHp += MinZeroAlign(ExtendedHp);
                    break;
                case Cons.Forge:
                    MiWuZhenAddOn += MinZeroAlign(MiWuZhenAddOn);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(con), con, null);
            }
            int MinZeroAlign(int ori) => value < 0 && ori < value ? -ori : value; //最多是清0
        }

        public void ClearState(Cons con)
        {
            switch (con)
            {
                case Cons.Stunned:
                    Stunned = 0;
                    break;
                case Cons.Shield:
                    Shield = 0;
                    break;
                case Cons.Invincible:
                    Invincible = 0;
                    break;
                case Cons.Bleed:
                    Bleed = 0;
                    break;
                case Cons.Poison:
                    Poison = 0;
                    break;
                case Cons.Burn:
                    Burn = 0;
                    break;
                case Cons.Stimulate:
                    Stimulate = 0;
                    break;
                case Cons.Imprisoned:
                    Imprisoned = 0;
                    break;
                case Cons.Cowardly:
                    Cowardly = 0;
                    break;
                case Cons.StrengthUp:
                    StrengthUp = 0;
                    break;
                case Cons.DodgeUp:
                    DodgeUp = 0;
                    break;
                case Cons.CriticalUp:
                    CriticalUp = 0;
                    break;
                case Cons.RouseUp:
                    RouseUp = 0;
                    break;
                case Cons.DefendUp:
                    FengHuoTaiAddOn = 0;
                    break;
                case Cons.DeathFight:
                    DeathFight = 0;
                    break;
                case Cons.Disarmed:
                    Unarmed = 0;
                    break;
                case Cons.Neizhu:
                    Neizhu = 0;
                    break;
                case Cons.ShenZhu:
                    ShenZhu = 0;
                    break;
                case Cons.EaseShield:
                    ExtendedHp = 0;
                    break;
                case Cons.Forge:
                    MiWuZhenAddOn = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(con), con, null);
            }
        }

        public void SetStates(Dictionary<int, int> buffs)
        {
            data = buffs;
        }
    }
}