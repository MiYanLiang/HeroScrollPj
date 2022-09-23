using System;
using System.Collections.Generic;
using System.WarModule;
using Assets.System.WarModule;
using CorrelateLib;
using Newtonsoft.Json;
using UnityEngine;

public class FightCardData : IChessman
{
    public static FightCardData PlayerBaseCard(int level, int cityLevel = 1)
    {
        var baseConfig = DataTable.BaseLevel[cityLevel];
        var baseCard = BaseCard(true, baseConfig.BaseHp, cityLevel);
        baseCard.level = level;
        return baseCard;
    }

    public static FightCardData BaseCard(bool isPlayer, int hp,int level)
    {
        var baseCard =
            new FightCardData(GameCard.Instance(cardId: 0, type: (int)GameCardType.Base, level: level)); //GetCard(17, true);
        baseCard.isPlayerCard = isPlayer;
        baseCard.IsLock = true;
        baseCard.SetPos(17);
        baseCard.status = ChessStatus.Instance(hp, hp, 17, new Dictionary<int, int>(), new List<int>(), 0);
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
    //觉醒
    public int Arouse { get; }
    //伤害
    public int Damage { get; }
    //战斗状态
    [JsonIgnore]public CardState CardState = new CardState();
    //摆放位置记录
    private int posIndex = -1;

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

    public FightCardData(GameCard card)
    {
        Card = card;
        info = card.GetInfo();
        cardType = card.Type;
        cardId = card.CardId;
        level = card.Level;
        Arouse = card.Arouse;
        this.element = info.Element;
        var damage = 0;
        var force = -1;
        var speed = 0;
        var intelligent = 0;
        var military = -1;
        var armedType = -4;
        var combatType = -1;
        var element = 0;
        var hitpoint = 0;
        var magicResist = 0;
        var armor = 0;
        var dodge = 0;
        var gameSetRecover = 0;
        var rare = 0;
        switch (info.Type)
        {
            case GameCardType.Hero:
            {
                var chessman = card;
                var heroTable = DataTable.Hero;
                var hero = DataTable.Hero[card.CardId];
                var m = DataTable.Military[hero.MilitaryUnitTableId];
                damage = hero.GetArousedStrength(level,chessman.Arouse) +
                         heroTable.GetDeputyStrength(
                             chessman.Deputy1Id, chessman.Deputy1Level,
                             chessman.Deputy2Id, chessman.Deputy2Level,
                             chessman.Deputy3Id, chessman.Deputy3Level,
                             chessman.Deputy4Id, chessman.Deputy4Level);
                force = hero.ForceTableId;
                speed = hero.GetArousedSpeed(chessman.Arouse) +
                        heroTable.GetDeputySpeed(chessman.Deputy1Id, chessman.Deputy1Level,
                            chessman.Deputy2Id, chessman.Deputy2Level,
                            chessman.Deputy3Id, chessman.Deputy3Level,
                            chessman.Deputy4Id, chessman.Deputy4Level);
                intelligent = hero.GetArousedIntelligent(chessman.Arouse) +
                              heroTable.GetDeputyIntelligent(chessman.Deputy1Id, chessman.Deputy1Level,
                                  chessman.Deputy2Id, chessman.Deputy2Level,
                                  chessman.Deputy3Id, chessman.Deputy3Level,
                                  chessman.Deputy4Id, chessman.Deputy4Level);
                //CombatStyle.IntelligentFormula(hero.Intelligent, level);
                military = m.Id;
                armedType = m.ArmedType;
                combatType = m.CombatStyle;
                element = m.Element;
                hitpoint = hero.GetArousedHitPoint(level,chessman.Arouse) +
                           heroTable.GetDeputyHitPoint(chessman.Deputy1Id, chessman.Deputy1Level,
                               chessman.Deputy2Id, chessman.Deputy2Level,
                               chessman.Deputy3Id, chessman.Deputy3Level,
                               chessman.Deputy4Id, chessman.Deputy4Level);
                magicResist = hero.GetArousedMagicRest(chessman.Arouse) +
                              heroTable.GetDeputyMagicRest(chessman.Deputy1Id, chessman.Deputy1Level,
                                  chessman.Deputy2Id, chessman.Deputy2Level,
                                  chessman.Deputy3Id, chessman.Deputy3Level,
                                  chessman.Deputy4Id, chessman.Deputy4Level);
                armor = hero.GetArousedArmor(chessman.Arouse) +
                        heroTable.GetDeputyArmor(chessman.Deputy1Id, chessman.Deputy1Level,
                            chessman.Deputy2Id, chessman.Deputy2Level,
                            chessman.Deputy3Id, chessman.Deputy3Level,
                            chessman.Deputy4Id, chessman.Deputy4Level);
                dodge = hero.GetArousedDodge(chessman.Arouse) +
                        heroTable.GetDeputyDodge(chessman.Deputy1Id, chessman.Deputy1Level,
                            chessman.Deputy2Id, chessman.Deputy2Level,
                            chessman.Deputy3Id, chessman.Deputy3Level,
                            chessman.Deputy4Id, chessman.Deputy4Level);
                break;
            }
            case GameCardType.Tower:
            {
                var tower = DataTable.Tower[card.CardId];
                force = tower.ForceId;
                damage = CombatStyle.DamageFormula(tower.Strength, level);
                speed = tower.Speed;
                intelligent = CombatStyle.EffectFormula(tower.Effect, level, tower.EffectUp);
                military = tower.Id;
                armedType = -2;
                combatType = 1;
                element = 0;
                hitpoint = CombatStyle.HitPointFormula(tower.HitPoint, level);
            }
                break;
            case GameCardType.Trap:
            {
                var trap = DataTable.Trap[card.CardId];
                damage = CombatStyle.DamageFormula(trap.Strength, level);
                force = trap.ForceId;
                speed = 0;
                intelligent = 0;
                military = trap.Id;
                armedType = -3;
                combatType = -1;
                element = 0;
                hitpoint = trap.Id == 11 || trap.Id == 12 //如果是宝箱不会随着等级提升
                    ? trap.HitPoint
                    : CombatStyle.HitPointFormula(trap.HitPoint, level);
            }
                break;
            case GameCardType.Base:
            {
                damage = 0;
                force = 0;
                speed = 0;
                intelligent = 0;
                military = -1;
                armedType = -4;
                combatType = -1;
                element = 0;
                hitpoint = DataTable.BaseLevel[level].BaseHp;
            }
                break;
            case GameCardType.Soldier:
            case GameCardType.Spell:
            default:
                throw new ArgumentOutOfRangeException();
        }

        
        Troop = force;
        Speed = speed;
        Damage = damage;
        StatesUi = new Dictionary<int, EffectStateUi>();
        status = ChessStatus.Instance(hitpoint, hitpoint, Pos, new Dictionary<int, int>(), new List<int>(), 0);
        Style = CombatStyle.Instance(military, armedType, combatType, element, Damage, level, hitpoint, speed, force,
            intelligent, info.GameSetRecovery, info.Rare, magicResist, armor, dodge);
    }

    public GameCard Card { get; }
    public int CardId => cardId;
    public GameCardType CardType => (GameCardType) cardType;
    public int HitPoint => status.Hp;
    public int MaxHitPoint => status.MaxHp;
    public int Level => level;
    public int Speed { get; }
    public int Troop { get; }

    public CombatStyle Style
    {
        get
        {
            if (_style == null)
            {
                _style = ChessOperatorManager<FightCardData>.GetCombatStyle(
                    Card,
                    DataTable.Hero,
                    DataTable.Tower,
                    DataTable.Trap,
                    DataTable.Military,
                    DataTable.BaseLevel);
            }
            return _style;
        }
        set => _style = value;
    }

    private int instanceId = -1;
    private ChessmanStyle chessmanStyle;
    private ChessStatus status;
    private CombatStyle _style;

    public CombatStyle GetStyle() => Style;

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

    public int Pos => posIndex;
    public bool IsPlayer => isPlayerCard;
    public bool IsActed => isActionDone;
    public ChessStatus Status => status;
    public void SetActed(bool isActed = true) => isActionDone = isActed;
    public void UpdateActivityStatus(ChessStatus stat)
    {
        if (!status.IsDeath)
        {
            status = stat.CloneHp();
            cardObj.War.UpdateHpUi(stat.HpRate);
        }
        CardState.SetStates(stat.Buffs);
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

    public void SetInstanceId(int insId) => instanceId = insId;
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

