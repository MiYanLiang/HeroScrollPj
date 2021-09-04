using CorrelateLib;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 战斗方式类。描述战斗单位的抽象层。
    /// 远程，近战，兵种系数
    /// </summary>
    public class CombatStyle
    {
        public enum Types
        {
            None = -1,
            Melee = 0,
            Range = 1,
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="military">兵种</param>
        /// <param name="armedType">兵种系数，棋盘=-1，塔=-2，陷阱=-3，老巢=-4</param>
        /// <param name="combat">攻击类型，近战=0，远程=1，非战=-1</param>
        /// <param name="element">0=物理，负数=特殊物理，正数=魔法</param>
        /// <param name="strength">力量/基础伤害值</param>
        /// <param name="level">等级</param>
        /// <param name="troop">军团</param>
        /// <returns></returns>
        public static CombatStyle Instance(int military, int armedType, int combat, int element,
            int strength, int level, int troop) =>
            new CombatStyle(military, armedType, combat, element, strength, level, troop);

        /// <summary>
        /// 普通系
        /// </summary>
        public const int General = 0;

        /// <summary>
        /// 护盾系
        /// </summary>
        public const int Shield = 1;

        /// <summary>
        /// 步兵系
        /// </summary>
        public const int Infantry = 2;

        /// <summary>
        /// 长持系
        /// </summary>
        public const int LongArmed = 3;

        /// <summary>
        /// 短持系
        /// </summary>
        public const int ShortArmed = 4;

        /// <summary>
        /// 骑兵系
        /// </summary>
        public const int Knight = 5;

        /// <summary>
        /// 特种系
        /// </summary>
        public const int Special = 6;

        /// <summary>
        /// 战车系
        /// </summary>
        public const int Chariot = 7;

        /// <summary>
        /// 战船系
        /// </summary>
        public const int Warship = 8;

        /// <summary>
        /// 弓兵系
        /// </summary>
        public const int Archer = 9;

        /// <summary>
        /// 蛮族系
        /// </summary>
        public const int Barbarian = 10;

        /// <summary>
        /// 统御系
        /// </summary>
        public const int Commander = 11;

        /// <summary>
        /// 干扰系
        /// </summary>
        public const int Interfere = 12;

        /// <summary>
        /// 辅助系
        /// </summary>
        public const int Assist = 13;

        /// <summary>
        /// 攻击分类
        /// 0 = melee, 1 = range
        /// </summary>
        public Types Type { get; set; }

        /// <summary>
        /// 兵种系数
        /// -1 = 棋盘/自然, -2 = 塔, -3 陷阱, 正数为兵种系数
        /// </summary>
        public int ArmedType { get; set; }

        /// <summary>
        /// 武将类 = 兵种, 塔/陷阱 = id
        /// </summary>
        public int Military { get; set; }

        /// <summary>
        /// 攻击元素, 与<see cref="CombatConduct"/>的元素对应。
        /// 0=物理，负数为特殊物理，正数为魔法
        /// </summary>
        public int Element { get; set; }

        public int Troop { get; set; }
        public int Strength { get; set; }
        public int Level { get; set; }

        [JsonConstructor]
        protected CombatStyle()
        {
        }

        protected CombatStyle(int military, int armedType, int combatStyle, int element, int strength, int level,
            int troop)
        {
            Element = element;
            Type = (Types)combatStyle;
            ArmedType = armedType;
            Military = military;
            Strength = strength;
            Level = level;
            Troop = troop;
        }
        protected CombatStyle(CombatStyle s)
        {
            Element = s.Element;
            Type = s.Type;
            ArmedType = s.ArmedType;
            Military = s.Military;
            Strength = s.Strength;
            Level = s.Level;
            Troop = s.Troop;
        }

        public override string ToString() => $"Combat({Type}).Armed[{ArmedType}] MId({Military}):E[{Element}]";

    }
}