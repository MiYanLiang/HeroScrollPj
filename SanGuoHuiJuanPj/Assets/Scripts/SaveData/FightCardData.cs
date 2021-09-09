using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using Newtonsoft.Json;
using UnityEngine;

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

    //战斗状态
    [JsonIgnore]public CardState CardState = new CardState();
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
    public int combatType;
    /// <summary>
    /// 被攻击者的行为，0受击，1防护盾，2闪避，3护盾，4无敌
    /// </summary>
    public int attackedBehavior;

    private GameCardInfo info;

    public FightCardData()
    {
    }

    public FightCardData(GameCard card)
    {
        info = card.GetInfo();
        unitId = 1;
        cardType = card.typeIndex;
        cardId = card.CardId;
        cardGrade = card.Level;
        damage = info.GetDamage(card.Level);
        hpr = info.GameSetRecovery;
        cardDamageType = info.DamageType;
        combatType = info.CombatType;
        var hp = info.GetHp(card.Level);
        status = ChessStatus.Instance(hp,hp,Pos,0,new Dictionary<int, int>());
        StatesUi = new Dictionary<int, EffectStateUi>();
        var force = -1;
        switch (card.typeIndex)
        {
            case 0:
                force = DataTable.Hero[card.id].ForceTableId;
                break;
            case 2:
                force = DataTable.Tower[card.id].ForceId;
                break;
            case 3:
                force = DataTable.Trap[card.id].ForceId;
                break;
        }

        switch (CardType)
        {
            case GameCardType.Hero:
                var m = MilitaryInfo.GetInfo(card.CardId);
                style = CombatStyle.Instance(m.Id, m.ArmedType, info.CombatType, info.DamageType, info.GetDamage(card.Level), card.Level,force);
                break;
            case GameCardType.Tower:
                style = CombatStyle.Instance(cardId, -2, 1, 0, info.GetDamage(card.Level), card.Level,force);
                break;
            case GameCardType.Trap:
                style = CombatStyle.Instance(cardId, -3, -1, 0, info.GetDamage(card.Level), card.Level, force);
                break;
            case GameCardType.Base:
                style = CombatStyle.Instance(-1, -4, -1, 0, 0, card.Level, force);
                break;
            case GameCardType.Soldier:
            case GameCardType.Spell:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [JsonIgnore] public int PosIndex => posIndex;
    public int CardId => cardId;
    [JsonIgnore] public GameCardType CardType => (GameCardType) cardType;
    public GameCardInfo Info => info;
    public int HitPoint => status.MaxHp;
    public int Level => cardGrade;
    private CombatStyle style;
    public CombatStyle Style => style;
    private int instanceId;
    private ChessmanStyle chessmanStyle;
    private ChessStatus status;

    public CombatStyle GetStyle() => style;


    [JsonIgnore] public Dictionary<int, EffectStateUi> StatesUi { get; }

    public void UpdatePos(int pos)
    {
        posIndex = pos;
    }

    //更新血条显示
    public void UpdateHpUi()
    {
        var isLose = status.Hp <= 0;

        if (isLose)
        {
            status.Kill();
            UpdateHp();
            cardObj.SetLose(true);
            return;
        }
        UpdateHp();
        void UpdateHp() => cardObj.War.SetHp(status.HpRate);
    }

    public void ResetHp(int maxHp)
    {
        status.ResetHp(maxHp);
    }

    int IChessman.InstanceId
    {
        get => instanceId;
        set => instanceId = value;
    }
    public int InstanceId => instanceId;

    [JsonIgnore]public int Pos => PosIndex;
    [JsonIgnore]public bool IsPlayer => isPlayerCard;
    [JsonIgnore]public bool IsActed => isActionDone;
    [JsonIgnore] public ChessStatus Status => status;
    public void SetActed(bool isActed = true) => isActionDone = isActed;
    public void UpdateActivityStatus(ChessStatus stat)
    {
        status = stat.Clone();
        CardState.SetStates(stat.Buffs);
        cardObj.War.SetHp(stat.HpRate);
        if(stat.IsDeath)
            cardObj.SetLose(true);
    }

    public ChessmanStyle ChessmanStyle
    {
        get
        {
            if (chessmanStyle == null)
                chessmanStyle = GetChessmanStyle();
            return chessmanStyle;
        }
    }
    [Obsolete("新战斗系统别用")]
    public HitPoint Hp { get; set; }

    private ChessmanStyle GetChessmanStyle()
    {
        switch (CardType)
        {
            case GameCardType.Hero:
                return ChessUiStyle.Instance<HeroStyle>(Style);
            case GameCardType.Tower:
                return ChessUiStyle.Instance<TowerStyle>(Style);
            case GameCardType.Trap:
            case GameCardType.Base:
                return ChessUiStyle.Instance<TrapStyle>(Style);
            case GameCardType.Spell:
            case GameCardType.Soldier:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}

[Serializable]
public class HitPoint:IComparable, IEquatable<HitPoint>,IComparer<HitPoint>
{
    [SerializeField]private int max;
    [SerializeField]private int value;

    public int Max => max;

    public int Value
    {
        get => value;
        private set => this.value = value;
    }

    public HitPoint(int max)
    {
        this.max = max;
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

