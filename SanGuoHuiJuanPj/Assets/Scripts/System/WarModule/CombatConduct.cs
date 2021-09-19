using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using UnityEditor.Experimental.GraphView;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 战斗执行，描述棋子在战斗中对某一个目标执行的一个战斗行为
    /// </summary>
    //[Serializable]
    public class CombatConduct
    {
        #region Damages 伤害类型
        //正数定义为法术，负数定为物理，而0是基本物理
        public const int PhysicalDmg = 0;
        /// <summary>
        /// 固定伤害，不计算防御类的伤害
        /// </summary>
        public const int FixedDmg = -1;
        public const int BasicMagicDmg = 1;
        public const int FireDmg = 2;
        public const int ThunderDmg = 3;
        #endregion

        #region Kinds 类型
        public const int DamageKind = 0;
        public const int HealKind = 1;
        public const int BuffKind = 2;
        public const int KillingKind = 3;

        public const int PlayerDegreeKind = 10;
        #endregion
    
        private static CombatConduct _zeroDamage = InstanceDamage(0);

        //[JsonProperty("I")]
        public int InstanceId { get; set; }
        /// <summary>
        /// 战斗元素类型,其余资源类型并不使用这字段，请查<see cref="Element"/>
        /// 精灵类：精灵InstanceId
        /// </summary>
        //[JsonProperty("K")] 
        public int Kind { get; set; }
        /// <summary>
        /// 战斗类<see cref="DamageKind"/> 或 <see cref="HealKind"/> ： 0 = 物理 ，大于0 = 法术元素，小于0 = 特殊物理，
        /// 状态类<see cref="BuffKind"/>：状态Id，详情看 <see cref="CardState.Cons"/>
        /// 斩杀类<see cref="KillingKind"/>
        /// 玩家维度<see cref="PlayerDegreeKind"/>：资源Id(-1=金币,正数=宝箱id)
        /// 精灵类：精灵类别Id
        /// </summary>
        //[JsonProperty("E")] 
        public int Element { get; set; }
        /// <summary>
        /// 暴击。注：已计算的暴击值
        /// </summary>
        //[JsonProperty("C")] 
        public float Critical { get; set; }
        /// <summary>
        /// 会心。注：已计算的会心值
        /// </summary>
        //[JsonProperty("R")] 
        public float Rouse { get; set; }
        /// <summary>
        /// 基础值
        /// </summary>
        //[JsonProperty("B")] 
        public float Basic { get; set; }
        /// <summary>
        /// 总伤害 = 基础伤害+暴击+会心
        /// </summary>
        public float Total => Basic + Critical + Rouse;

        public static CombatConduct Instance(float value, float critical, float rouse, int element, int kind) =>
            new CombatConduct { Basic = value, Element = element, Critical = critical, Rouse = rouse, Kind = kind };

        public static CombatConduct AddSprite(int value,int spriteId, int typeId) => Instance(value, critical: 0, rouse: 0, element: typeId, kind: spriteId);
        public static CombatConduct RemoveSprite(int spriteId,int typeId) => Instance(-1, 0, 0, element: typeId, kind: spriteId);
        public static CombatConduct InstanceKilling() => Instance(0, 0, 0, 0, KillingKind);

        public static CombatConduct InstanceHeal(float heal, int element = 0) => Instance(heal, 0, 0, element, HealKind);
        /// <summary>
        /// 生成状态
        /// </summary>
        /// <param name="con"></param>
        /// <param name="value">默认1，-1为清除值</param>
        /// <returns></returns>
        public static CombatConduct InstanceBuff(CardState.Cons con, float value = 1) => Instance(value, 0, 0, (int)con, BuffKind);

        public static CombatConduct InstanceDamage(float damage, int element = 0) => Instance(damage, 0, 0, element,DamageKind);

        public static CombatConduct InstanceDamage(float damage, float critical, float rouse, int element) =>
            Instance(damage, critical, rouse, element, DamageKind);
        public static CombatConduct InstanceDamage(float damage, float critical, int element = 0) =>
            Instance(damage, critical, 0, element,DamageKind);

        /// <summary>
        /// 生成玩家维度的资源
        /// </summary>
        /// <param name="resourceId">金币=-1，正数=宝箱Id</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static CombatConduct InstancePlayerResource(int resourceId, int value = 1) =>
            Instance(value, 0, 0, resourceId, PlayerDegreeKind);
        public static CombatConduct ZeroDamage => _zeroDamage;

        public void Multiply(float rate)
        {
            Basic *= rate;
            Critical *= rate;
            Rouse *= rate;
        }

        public override string ToString()
        {
            var kindText = string.Empty;
            var elementText = string.Empty;
            var valueText = Total.ToString(CultureInfo.InvariantCulture);
            switch (Kind)
            {
                case BuffKind:
                {
                    kindText = "Buff";
                    elementText = $"{(CardState.Cons)Element}";
                    break;
                }
                case DamageKind:
                {
                    kindText = "伤害";
                    switch (Element)
                    {
                        case FireDmg: elementText = "火元素"; break;
                        case FixedDmg: elementText = "固伤";break;
                        case BasicMagicDmg: elementText = "基础法伤";break;
                        case PhysicalDmg: elementText = "物理伤害";break;
                        case ThunderDmg: elementText = "雷元素";break;
                        default: elementText = "未知伤害";break;
                    }

                    var str = new StringBuilder($"基础={Basic},");
                    if (Critical > 0) str.Append($"暴击加成={Critical},");
                    if (Rouse > 0) str.Append($"会心加成={Rouse},");
                    str.Append($"总:{Total}");
                    valueText = str.ToString();
                    break;
                }
                case HealKind: kindText = "治疗";break;
                case KillingKind: kindText = "击杀";break;
                case PlayerDegreeKind:
                {
                    kindText = "玩家资源";
                    if (Element == -1)
                        elementText = "金币";
                    else if (Element >= 0) elementText = "宝箱";
                    else elementText = "未知资源";
                    break;
                }
            }
            return $"{InstanceId}.{kindText}{elementText}[{valueText}]";
        }

        public void SetZero()
        {
            Basic = 0;
            Critical = 0;
            Rouse = 0;
        }

        public void SetBasic(int value)
        {
            Basic = value;
            Critical = 0;
            Rouse = 0;
        }

        public static bool IsPositiveConduct(CombatConduct conduct)
        {
            return conduct.Kind == HealKind ||
                   conduct.Kind == BuffKind && !CardState.IsNegativeBuff((CardState.Cons)conduct.Element) &&
                   conduct.Total > 0;
        }

        public CombatConduct Clone(float rate = 1f) => Instance(Basic * rate, Critical * rate, Rouse * rate, Element, Kind);

        public bool IsRouseDamage() => Rouse > 0;
        public bool IsCriticalDamage() => Critical > 0;
    }

    public class Damage
    {
        public enum Kinds
        {
            Physical,
            Magic,
            Fixed
        }

        public static Kinds GetKind(int element)
        {
            switch (element)
            {
                case CombatConduct.PhysicalDmg:
                    return Kinds.Physical;
                case CombatConduct.FixedDmg:
                    return Kinds.Fixed;
                case CombatConduct.BasicMagicDmg:
                case CombatConduct.FireDmg:
                case CombatConduct.ThunderDmg:
                    return Kinds.Magic;
                default: throw new ArgumentOutOfRangeException($"Unknown Damage kind ={element}!");
            }
        }

        public static Kinds GetKind(CombatConduct conduct) => GetKind(conduct.Element);

    }

}