using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 攻击方式。
    /// 远程，近战，(不)/可反击单位，兵种系数
    /// </summary>
    public class AttackStyle
    {
        public enum CombatStyles
        {
            Melee = 0,
            Range = 1,
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="military">兵种</param>
        /// <param name="armedType">兵种系数，塔=-1，陷阱=-2</param>
        /// <param name="combat">攻击类型，近战=0，远程=1</param>
        /// <param name="counter">反击类型，不可反击=0，可反击>0</param>
        /// <param name="element">0=物理，负数=特殊物理，正数=魔法</param>
        /// <param name="strength">力量</param>
        /// <param name="level">等级</param>
        /// <param name="troop">军团</param>
        /// <returns></returns>
        public static AttackStyle Instance(int military, int armedType, int combat, int counter, int element,
            int strength, int level, int troop) =>
            new AttackStyle(military, armedType, combat, counter, element, strength, level, troop);

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
        /// 可反击单位
        /// 0 = no, >1 = counter
        /// </summary>
        public int CounterStyle { get; set; }
        /// <summary>
        /// 攻击分类
        /// 0 = melee, 1 = range
        /// </summary>
        public CombatStyles CombatStyle { get; set; }
        /// <summary>
        /// 兵种系数
        /// -1 = 塔, -2 陷阱, 正数为兵种系数
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
        private AttackStyle()
        {
        }

        private AttackStyle(int military, int armedType, int combatStyle, int counterStyle, int element,int strength, int level, int troop)
        {
            CounterStyle = counterStyle;
            Element = element;
            CombatStyle = (CombatStyles)combatStyle;
            ArmedType = armedType;
            Military = military;
            Strength = strength;
            Level = level;
            Troop = troop;
        }

        public override string ToString()
        {
            var counterString = CounterStyle > 0 ? $".Counter({CounterStyle})" : string.Empty;
            return
                $"Combat({CombatStyle}){counterString}.Armed[{ArmedType}] MId({Military}):E[{Element}]";
        }
    }
}