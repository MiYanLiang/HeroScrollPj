using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public GenericValue Hp { get; protected set; }

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
    [JsonIgnore]public bool IsNonGeneralDamage => cardObj.CardInfo.IsNonGeneralDamage;
    public int PosIndex => posIndex;
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

    public void ResetHp(int hp) => Hp = new GenericValue(hp);
}

public class GenericValue:IComparable, IEquatable<GenericValue>,IComparer<GenericValue>
{
    public int Max { get; private set; }
    public int Value { get; private set; }

    public GenericValue(int max)
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

    public bool Equals(GenericValue other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public float Rate() => 1f * Value / Max;
    public static int operator +(GenericValue left, int right) => left.Value + right;
    public static int operator -(GenericValue left, int right) => left.Value - right;
    public static int operator /(GenericValue left, int right) => left.Value / right;
    public static int operator %(GenericValue left, int right) => left.Value % right;
    public static int operator *(GenericValue left, int right) => left.Value * right;
    public static int operator +(GenericValue left, GenericValue right) => left.Value + right;
    public static int operator -(GenericValue left, GenericValue right) => left.Value - right;
    public static int operator /(GenericValue left, GenericValue right) => left.Value / right;
    public static int operator %(GenericValue left, GenericValue right) => left.Value % right;
    public static int operator *(GenericValue left, GenericValue right) => left.Value * right;
    public static int operator +(int l, GenericValue r) => l + r.Value;
    public static int operator -(int l, GenericValue r) => l - r.Value;
    public static int operator /(int l, GenericValue r) => l / r.Value;
    public static int operator %(int l, GenericValue r) => l % r.Value;
    public static int operator *(int l, GenericValue r) => l * r.Value;
    public static bool operator ==(GenericValue left, GenericValue right) => Equals(left, right);

    public static bool operator !=(GenericValue left, GenericValue right) => !Equals(left, right);
    public static bool operator >(GenericValue left, GenericValue right) => left.Value > right.Value;

    public static bool operator <(GenericValue left, GenericValue right) => left.Value < right.Value;
    public static bool operator >=(GenericValue left, GenericValue right) => left.Value >= right.Value;

    public static bool operator <=(GenericValue left, GenericValue right) => left.Value <= right.Value;
    public static bool operator ==(GenericValue left, int right) => right.Equals(left?.Value);

    public static bool operator !=(GenericValue left, int right) => !right.Equals(left?.Value);
    public static bool operator >(GenericValue left, int right) => left.Value > right;

    public static bool operator <(GenericValue left, int right) => left.Value < right;
    public static bool operator >=(GenericValue left, int right) => left.Value >= right;

    public static bool operator <=(GenericValue left, int right) => left.Value <= right;
    public static bool operator ==(int right, GenericValue left) => right.Equals(left?.Value);

    public static bool operator !=(int right, GenericValue left) => !right.Equals(left?.Value);
    public static bool operator >(int l, GenericValue r) => l > r.Value;

    public static bool operator <(int l, GenericValue r) => l < r.Value;
    public static bool operator >=(int l, GenericValue r) => l >= r.Value;

    public static bool operator <=(int l, GenericValue r) => l <= r.Value;

    public int Compare(GenericValue x, GenericValue y)
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
        return Equals((GenericValue)obj);
    }

    public override string ToString() => $"[{Value}/{Max}]";

    public override int GetHashCode() => Value;
}

[Serializable]public class CardRound
{
    public int Hp { get; set; }
    [JsonProperty("H")]public CardHit[] Hits { get; set; }
    [JsonProperty("S")]public int[,] States { get; set; }
    public IEnumerator CardAction { get; set; }
}

[Serializable]public class CardHit
{
    [JsonProperty("P")]public int Pos { get; set; }
    [JsonProperty("D")]public CardDamage Damage { get; set; }
    //[JsonProperty("C")]public CardDamage Counter { get; set; }
}

[Serializable]
public class CardDamage
{
    [JsonProperty("M")] public int IsMagic { get; set; }
    [JsonProperty("C")] public float Critical { get; set; }
    [JsonProperty("R")] public float Rouse { get; set; }
    [JsonProperty("T")] public int Type { get; set; }
    [JsonProperty("D")] public float Damage { get; set; }

    public float Value => Damage + Damage * Critical + Damage * Rouse;
}