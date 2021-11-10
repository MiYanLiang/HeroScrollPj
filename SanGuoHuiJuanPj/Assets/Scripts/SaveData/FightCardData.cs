using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]public class FightCardData : IChessman
{
    public static FightCardData PlayerBaseCard(int level, int cityLevel = 1)
    {
        var baseConfig = DataTable.BaseLevel[cityLevel];
        var playerLvlCfg = DataTable.PlayerLevelConfig[level];
        var hp = playerLvlCfg.BaseHpAddOn + baseConfig.BaseHp;
        var baseCard = BaseCard(true, hp, cityLevel);
        baseCard.level = level;
        return baseCard;
    }

    public static FightCardData BaseCard(bool isPlayer, int hp,int level)
    {
        var baseCard = new FightCardData(GameCard.Instance(0, (int)GameCardType.Base, level)); //GetCard(17, true);
        baseCard.isPlayerCard = isPlayer;
        baseCard.IsLock = true;
        baseCard.SetPos(17);
        baseCard.status = ChessStatus.Instance(hp, hp, 17, new Dictionary<int, int>(), new List<int>(), 0);
        baseCard.ResetHp(hp);
        return baseCard;
    }

    public WarGameCardUi cardObj;
    //卡牌obj
    //public GameObject cardObj;
    //卡牌类型
    public int cardType;
    //卡牌id
    public int cardId;
    //等级
    public int level;
    //伤害
    public int Damage { get; }

    //战斗状态
    [JsonIgnore]public CardState CardState = new CardState();
    //摆放位置记录
    public int posIndex = -1;

    //此回合是否行动
    public bool isActionDone;
    //是否是玩家卡牌
    public bool isPlayerCard;
    /// <summary>
    /// 单位伤害类型0物理，1法术
    /// </summary>
    public int element;
    /// <summary>
    /// 单位行动类型0近战，1远程
    /// </summary>
    public int combatType;

    private GameCardInfo info;

    public FightCardData()
    {
    }

    public FightCardData(GameCard card)
    {
        Card = card;
        info = card.GetInfo();
        cardType = card.typeIndex;
        cardId = card.CardId;
        level = card.Level;
        this.element = info.Element;
        var strength = 0;
        var force = -1;
        var speed = 0;
        var intelligent = 0;
        var military = -1;
        var armedType = -4;
        var combatType = -1;
        var element = 0;
        var hitpoint = 0;
        switch (info.Type)
        {
            case GameCardType.Hero:
            {
                var hero = DataTable.Hero[card.id];
                var m = DataTable.Military[hero.MilitaryUnitTableId];
                strength = hero.Strength;
                force = hero.ForceTableId;
                speed = hero.Speed;
                intelligent = hero.Intelligent;
                    //CombatStyle.IntelligentFormula(hero.Intelligent, level);
                military = m.Id;
                armedType = m.ArmedType;
                combatType = m.CombatStyle;
                element = m.Element;
                hitpoint = hero.HitPoint;
                break;
            }
            case GameCardType.Tower:
            {
                var tower = DataTable.Tower[card.id];
                force = tower.ForceId;
                strength = tower.Strength;
                speed = tower.Speed;
                intelligent = CombatStyle.EffectFormula(tower.Effect, level, tower.EffectUp);
                military = tower.Id;
                armedType = -2;
                combatType = 1;
                element = 0;
                hitpoint = tower.HitPoint;
            }
                break;
            case GameCardType.Trap:
            {
                var trap = DataTable.Trap[card.id];
                strength = trap.Strength;
                force = trap.ForceId;
                speed = 0;
                intelligent = 0;
                military = trap.Id;
                armedType = -3;
                combatType = -1;
                element = 0;
                hitpoint = trap.HitPoint;
            }
                break;
            case GameCardType.Base:
            {
                strength = 0;
                force = 0;
                speed = 0;
                intelligent = 0;
                military = -1;
                armedType = -4;
                combatType = -1;
                element = 0;
                hitpoint = DataTable.BaseLevel[level].BaseHp;//老巢血量不在这里初始化
            }
                break;
            case GameCardType.Soldier:
            case GameCardType.Spell:
            default:
                throw new ArgumentOutOfRangeException();
        }

        hitpoint = CombatStyle.HitPointFormula(hitpoint, level);
        Troop = force;
        Speed = speed;
        Damage = CombatStyle.DamageFormula(strength, level);
        status = ChessStatus.Instance(hitpoint, hitpoint, Pos, new Dictionary<int, int>(), new List<int>(), 0);
        StatesUi = new Dictionary<int, EffectStateUi>();
        style = CombatStyle.Instance(military, armedType, combatType, element, Damage, level, hitpoint, speed, force,
            intelligent, info.GameSetRecovery, info.Rare);
    }

    public GameCard Card { get; }
    public int PosIndex => posIndex;
    public int CardId => cardId;
    public GameCardType CardType => (GameCardType) cardType;
    public int HitPoint => status.Hp;
    public int MaxHitPoint => status.MaxHp;
    public int Level => level;
    public int Speed { get; }
    public int Troop { get; }
    private CombatStyle style;
    public CombatStyle Style => style;
    private int instanceId = -1;
    private ChessmanStyle chessmanStyle;
    private ChessStatus status;

    public CombatStyle GetStyle() => style;

    public Dictionary<int, EffectStateUi> StatesUi { get; }

    public void SetPos(int pos)
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
        void UpdateHp() => cardObj.War.UpdateHpUi(status.HpRate);
    }

    public void ResetHp(int maxHp) => status.ResetHp(maxHp);

    int IChessman.InstanceId
    {
        get => instanceId;
        set => instanceId = value;
    }
    public int InstanceId => instanceId;

    public int Pos => PosIndex;
    public bool IsPlayer => isPlayerCard;
    public bool IsActed => isActionDone;
    public ChessStatus Status => status;
    public void SetActed(bool isActed = true) => isActionDone = isActed;
    public void UpdateActivityStatus(ChessStatus stat)
    {
        status = stat.Clone();
        CardState.SetStates(stat.Buffs);
        cardObj.War.UpdateHpUi(stat.HpRate);
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

    public bool IsLock { get; set; }

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

    public GameCardInfo GetInfo() => Card.GetInfo();
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

