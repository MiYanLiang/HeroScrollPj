﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CorrelateLib;
using Newtonsoft.Json;

[Serializable]public class FightCardData : ICardDrag,IChessman
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
    public int Pos => PosIndex;
    public bool IsPlayer => isPlayerCard;
    public bool IsActed => isActionDone;
    public bool IsAvailable => !IsActed && IsAlive;
    public void SetActed(bool isActed = true) => isActionDone = isActed;
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

