using CorrelateLib;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 战斗方式类。
    /// 主要用于战斗中交互的单位信息的抽象层。
    /// </summary>
    public class CombatStyle
    {
        public static int DamageFormula(int strength, int level) => (int)(strength * (1 + (level - 1) * 0.2f));
        //public static int IntelligentFormula(int intelligent, int level) => (int)(intelligent * (1 + (level - 1) * 0.1f));
        public static int HitPointFormula(int hitpoint, int level) => (int)(hitpoint * (1 + (level - 1) * 0.2f));
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
        /// <param name="speed"></param>
        /// <param name="troop">军团</param>
        /// <param name="hitPoint"></param>
        /// <param name="intelligent"></param>
        /// <param name="recovery"></param>
        /// <returns></returns>
        public static CombatStyle Instance(int military, int armedType, int combat, int element,
            int strength, int level, int hitPoint, int speed, int troop, int intelligent, int recovery, int rare) =>
            new CombatStyle(military, armedType, combat, element, strength, level, hitPoint, speed, troop, intelligent,
                recovery, rare);

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
        public int Speed { get; set; }
        public int Intelligent { get; set; }
        public int HitPoint { get; set; }
        public int Recovery { get; set; }
        public int Rare { get; set; }

        [JsonConstructor]
        protected CombatStyle()
        {
        }
        
        protected CombatStyle(int military, int armedType, int combatStyle, int element, int strength, int level,
            int hitPoint, int speed, int troop, int intelligent, int recovery, int rare)
        {
            Element = element;
            Type = (Types)combatStyle;
            ArmedType = armedType;
            Military = military;
            Strength = strength;
            Level = level;
            Troop = troop;
            Intelligent = intelligent;
            Recovery = recovery;
            Rare = rare;
            Speed = speed;
            HitPoint = hitPoint;
        }
        protected CombatStyle(CombatStyle s)
        {
            Rare = s.Rare;
            Recovery = s.Recovery;
            Intelligent = s.Intelligent;
            Speed = s.Speed;
            HitPoint = s.HitPoint;
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