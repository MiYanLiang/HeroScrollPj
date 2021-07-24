using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using Newtonsoft.Json;

[Serializable]public class FightCardData : ICardDrag
{
    //单位id,为0表示null
    public int unitId;

    [JsonIgnore]public WarGameCardUi cardObj;
    //卡牌obj
    //public GameObject cardObj;
    //卡牌类型
    public int cardType;
    //卡牌id
    public int cardId;
    //等级
    public int cardGrade;
    //伤害
    public int damage;

    public HitPoint Hp { get; protected set; }

    //战斗状态
    [JsonIgnore]public FightState fightState;
    //摆放位置记录
    public int posIndex = -1;
    //生命值回复
    public int hpr;
    //主被动单位
    public bool activeUnit;
    //此回合是否行动
    public bool isActionDone;
    //是否是玩家卡牌
    public bool isPlayerCard;
    /// <summary>
    /// 单位伤害类型0物理，1法术
    /// </summary>
    public int cardDamageType;
    /// <summary>
    /// 单位行动类型0近战，1远程
    /// </summary>
    public int cardMoveType;
    /// <summary>
    /// 被攻击者的行为，0受击，1防护盾，2闪避，3护盾，4无敌
    /// </summary>
    public int attackedBehavior;

    public FightCardData()
    {
        
    }
    public int PosIndex => posIndex;
    public bool IsAlive => Hp > 0;
    public bool IsActive => IsAlive && !isActionDone;
    public GameCardType CardType => cardObj.CardInfo.Type;

    public void UpdatePos(int pos) => posIndex = pos;
    //更新血条显示
    public void UpdateHpUi()
    {
        var isLose = Hp <= 0;

        if (isLose)
        {
            Hp.Set(0);
            UpdateHp();
            cardObj.SetLose(true);
            return;
        }
        UpdateHp();
        void UpdateHp() => cardObj.War.SetHp(1f * Hp.Value / Hp.Max);
    }

    public void ResetHp(int hp) => Hp = new HitPoint(hp);
}

public class HitPoint:IComparable, IEquatable<HitPoint>,IComparer<HitPoint>
{
    public int Max { get; private set; }
    public int Value { get; private set; }

    public HitPoint(int max)
    {
        Max = max;
        Value = max;
    }

    public void Set(int value,bool overMax = false)
    {
        Value = value;
        if(overMax)return;
        MaxAlign();
    }
    public void Add(int value,bool overMax = false)
    {
        Value += value;
        if(overMax)return;
        MaxAlign();
    }
    private void MaxAlign()
    {
        if (Value > Max) Value = Max;
    }

    public int CompareTo(object obj) => Value.CompareTo(obj);

    public bool Equals(HitPoint other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public float Rate() => 1f * Value / Max;
    public static int operator +(HitPoint left, int right) => left.Value + right;
    public static int operator -(HitPoint left, int right) => left.Value - right;
    public static int operator /(HitPoint left, int right) => left.Value / right;
    public static int operator %(HitPoint left, int right) => left.Value % right;
    public static int operator *(HitPoint left, int right) => left.Value * right;
    public static int operator +(HitPoint left, HitPoint right) => left.Value + right;
    public static int operator -(HitPoint left, HitPoint right) => left.Value - right;
    public static int operator /(HitPoint left, HitPoint right) => left.Value / right;
    public static int operator %(HitPoint left, HitPoint right) => left.Value % right;
    public static int operator *(HitPoint left, HitPoint right) => left.Value * right;
    public static int operator +(int l, HitPoint r) => l + r.Value;
    public static int operator -(int l, HitPoint r) => l - r.Value;
    public static int operator /(int l, HitPoint r) => l / r.Value;
    public static int operator %(int l, HitPoint r) => l % r.Value;
    public static int operator *(int l, HitPoint r) => l * r.Value;
    public static bool operator ==(HitPoint left, HitPoint right) => Equals(left, right);

    public static bool operator !=(HitPoint left, HitPoint right) => !Equals(left, right);
    public static bool operator >(HitPoint left, HitPoint right) => left.Value > right.Value;

    public static bool operator <(HitPoint left, HitPoint right) => left.Value < right.Value;
    public static bool operator >=(HitPoint left, HitPoint right) => left.Value >= right.Value;

    public static bool operator <=(HitPoint left, HitPoint right) => left.Value <= right.Value;
    public static bool operator ==(HitPoint left, int right) => right.Equals(left?.Value);

    public static bool operator !=(HitPoint left, int right) => !right.Equals(left?.Value);
    public static bool operator >(HitPoint left, int right) => left.Value > right;

    public static bool operator <(HitPoint left, int right) => left.Value < right;
    public static bool operator >=(HitPoint left, int right) => left.Value >= right;

    public static bool operator <=(HitPoint left, int right) => left.Value <= right;
    public static bool operator ==(int right, HitPoint left) => right.Equals(left?.Value);

    public static bool operator !=(int right, HitPoint left) => !right.Equals(left?.Value);
    public static bool operator >(int l, HitPoint r) => l > r.Value;

    public static bool operator <(int l, HitPoint r) => l < r.Value;
    public static bool operator >=(int l, HitPoint r) => l >= r.Value;

    public static bool operator <=(int l, HitPoint r) => l <= r.Value;

    public int Compare(HitPoint x, HitPoint y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, y)) return 1;
        if (ReferenceEquals(null, x)) return -1;
        return x.Value.CompareTo(y.Value);
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((HitPoint)obj);
    }

    public override string ToString() => $"[{Value}/{Max}]";

    public override int GetHashCode() => Value;
}

[Serializable]public class PieceAction
{
    public static PieceAction Instance(int pos, PieceHit[] hits = null) =>
        new() {Hits = hits ?? new PieceHit[0], Pos = pos};

    /// <summary>
    /// Chessman Pos
    /// </summary>
    [JsonProperty("Po")]public int Pos { get; set; }
    [JsonProperty("H")]public PieceHit[] Hits { get; set; }
    [JsonProperty("S")]public int[,] States { get; set; }
    [JsonProperty("J")]public int[,] JiBan { get; set; }
    [JsonProperty("Tr")]public ICollection<PieceTrigger> Triggers { get; set; }
}

[Serializable]public class PieceTrigger
{
    public const int Gold = 0;
    public const int WarChest = 1;
    public static PieceTrigger Instance(int type, int amount) => new(type, amount);
    public static PieceTrigger Instance(int type, float amount) => new(type, amount);
    public static PieceTrigger Instance(int type, object obj) => new(type, Json.Serialize(obj));
    [JsonProperty("T")]public int Type { get; }
    [JsonProperty("A")]public float Amount { get; }
    [JsonProperty("S")]public string Value { get; }

    [JsonConstructor]
    private PieceTrigger()
    {
        
    }
    private PieceTrigger(int type, int amount)
    {
        Type = type;
        Amount = amount;
    }
    private PieceTrigger(int type, float amount)
    {
        Type = type;
        Amount = amount;
    }
    
    private PieceTrigger(int type, string value)
    {
        Type = type;
        Value = value;
    }

    public int GetInt => (int)Amount;
    public float GetFloat() => Amount;
    public T GetValue<T>() where T : class => Json.Deserialize<T>(Value);
}

[Serializable]public class PieceHit
{
    public static PieceHit Instance(int to, int hp, CombatFactor[] factors)
    {
        return new()
        {
            To = to, Hp = hp, Factors = factors,Moves = new List<PieceAction>()
        };
    }

    /// <summary>
    /// Target Pos
    /// </summary>
    [JsonProperty("To")]public int To { get; set; }
    /// <summary>
    /// Target Hp
    /// </summary>
    [JsonProperty("Hp")]public int Hp { get; set; }
    /// <summary>
    /// Suffered Factor
    /// </summary>
    [JsonProperty("F")]public CombatFactor[] Factors { get; set; }
    /// <summary>
    /// Target States
    /// </summary>
    [JsonProperty("S")]public int[,] States { get; set; }
    /// <summary>
    /// Target Inner Move(eg. Counter,Trigger)
    /// </summary>
    [JsonProperty("M")]public ICollection<PieceAction> Moves { get; set; }
    /// <summary>
    /// Target JiBans
    /// </summary>
    [JsonProperty("J")]public int[,] JiBan { get; set; }
}

[Serializable]
public struct CombatFactor
{
    public enum Kinds
    {
        Damage = 0,
        Heal = 1,
        OffendState = 2,
        DefendState = 3
    }
    private static CombatFactor _zeroDamage = InstanceDamage(0);
    /// <summary>
    /// 战斗元素类型
    /// </summary>
    [JsonProperty("K")] public Kinds Kind { get; set; }
    /// <summary>
    /// 0 = 物理 ，大于0 = 法术元素，小于0 = 特殊物理
    /// </summary>
    [JsonProperty("E")] public int Element { get; set; }
    /// <summary>
    /// 暴击。注：已计算的暴击值
    /// </summary>
    [JsonProperty("C")] public float Critical { get; set; }
    /// <summary>
    /// 会心。注：已计算的会心值
    /// </summary>
    [JsonProperty("R")] public float Rouse { get; set; }
    /// <summary>
    /// 基础值
    /// </summary>
    [JsonProperty("B")] public float Basic { get; set; }
    /// <summary>
    /// 总伤害 = 基础伤害+暴击+会心
    /// </summary>
    public float Total => Basic + Critical + Rouse;

    public static CombatFactor Instance(float value, float critical, float rouse, int element = 0, Kinds kind = Kinds.Damage) =>
        new() {Basic = value, Element = element, Critical = critical, Rouse = rouse, Kind = kind};

    public static CombatFactor InstanceHeal(float heal, int element = 0) => Instance(heal, 0, 0, element, Kinds.Heal);

    public static CombatFactor InstanceOffendState(FightState.Cons con, float value = 1) => Instance(value, 0, 0, (int)con, Kinds.OffendState);
    public static CombatFactor InstanceDefendState(FightState.Cons con, float value = 1) => Instance(value, 0, 0, (int)con, Kinds.DefendState);

    public static CombatFactor InstanceDamage(float damage, int element = 0) => Instance(damage, 0, 0, element);

    public static CombatFactor InstanceDamage(float damage, float critical, int element = 0) =>
        Instance(damage, critical, 0, element);

    public static CombatFactor ZeroDamage => _zeroDamage;
}

/// <summary>
/// 攻击方式。
/// 远程，近战，(不)/可反击单位，兵种系数
/// </summary>
public class AttackStyle
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="military">兵种</param>
    /// <param name="armedType">兵种系数，塔=-1，陷阱=-2</param>
    /// <param name="combat">攻击类型，近战=0，远程=1</param>
    /// <param name="counter">反击类型，不可反击=0，可反击>0</param>
    /// <returns></returns>
    public static AttackStyle Instance(int military,int armedType,int combat,int counter) => new AttackStyle(military, armedType, combat, counter);

    /// <summary>
    /// 普通系
    /// </summary>
    public const int General= 0;
    /// <summary>
    /// 护盾系
    /// </summary>
    public const int Shield= 1;
    /// <summary>
    /// 步兵系
    /// </summary>
    public const int Infantry= 2;
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
    public int CombatStyle { get; set; }
    /// <summary>
    /// 兵种系数
    /// -1 = 塔, -2 陷阱, 正数为兵种系数
    /// </summary>
    public int ArmedType { get; set; }
    /// <summary>
    /// 兵种
    /// </summary>
    public int Military { get; set; }

    [JsonConstructor]
    private AttackStyle()
    {
        
    }

    private AttackStyle(int military, int armedType, int combatStyle, int counterStyle)
    {
        CounterStyle = counterStyle;
        CombatStyle = combatStyle;
        ArmedType = armedType;
        Military = military;
    }
}
