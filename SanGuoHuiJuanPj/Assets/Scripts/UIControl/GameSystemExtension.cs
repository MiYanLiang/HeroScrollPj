using CorrelateLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static class GameSystemExtension
{
    public static T RandomPick<T>(this IEnumerable<T> data)
    {
        var list = data.ToList();
        var pick = Random.Range(0, list.Count);
        return list[pick];
    }

    /// <summary>
    /// 上阵
    /// </summary>
    /// <param name="cards"></param>
    /// <param name="forceId">军团Id</param>
    /// <returns></returns>
    public static IEnumerable<GameCard> Enlist(this IEnumerable<GameCard> cards, int forceId) =>
        cards.Where(card => GetForceId(card) == forceId && card.level > 0 && card.isFight > 0);
    public static bool IsEnlistAble(this GameCard card) => card.Level > 0;
    public static int GetForceId(this GameCard card)
    {
        //单位类型0武将 1士兵 2塔 3陷阱 4技能
        int force = -1;
        switch (card.typeIndex)
        {
            case 0 : 
                force = DataTable.Hero[card.id].ForceTableId;
                break;
            case 2 :
                force = DataTable.Tower[card.id].ForceId;
                break;
            case 3 :
                force = DataTable.Trap[card.id].ForceId;
                break;
        }
        return force;
    }

    public static GameCard GetOrInstance(this List<GameCard> cards, int cardId, int cardType, int level) =>
        cards.GetOrInstance(cardId, (GameCardType) cardType,level);

    public static GameCard GetOrInstance(this List<GameCard> cards, int cardId,
        GameCardType cardType, int level)
    {
        var card = cards.SingleOrDefault(c => c.id == cardId);
        if (card != null) return card;
        card = new GameCard().Instance(cardType, cardId, level);
        cards.Add(card);
        return card;
    }

    public static GameCard Instance(this GameCard card, GameCardType type, int cardId, int cardLevel)
    {
        card.id = cardId;
        card.level = cardLevel;
        card.maxLevel = cardLevel;
        card.typeIndex = (int) type;
        card.isHad = true;
        return card;
    }

    public static Chessman[] Poses(this GuideTable c, GuideProps prop)
    {
        switch (prop)
        { 
            case GuideProps.Card:
                return new[] {c.Card1, c.Card2, c.Card3, c.Card4, c.Card5};
            case GuideProps.Player:
                return new []{c.Pos1,c.Pos2,c.Pos3,c.Pos4,c.Pos5,c.Pos6,c.Pos7,c.Pos8,c.Pos9,c.Pos10,c.Pos11,c.Pos12,c.Pos13,c.Pos14,c.Pos15,c.Pos16,c.Pos17,c.Pos18,c.Pos19,c.Pos20};
            case GuideProps.Enemy:
                return new[]
                {
                    c.EPos1, c.EPos2, c.EPos3, c.EPos4, c.EPos5, c.EPos6, c.EPos7, c.EPos8, c.EPos9, c.EPos10, c.EPos11,
                    c.EPos12, c.EPos13, c.EPos14, c.EPos15, c.EPos16, c.EPos17, c.EPos18, c.EPos19, c.EPos20
                };
            default:
                throw new ArgumentOutOfRangeException($"{c.GetType().Name}.{nameof(prop)}", prop, null);
        }
    }
    public static int[] Poses(this EnemyTable c)
    {
        return new[]
        {
            c.Pos1, c.Pos2, c.Pos3, c.Pos4, c.Pos5, c.Pos6, c.Pos7, c.Pos8, c.Pos9, c.Pos10, c.Pos11, c.Pos12, c.Pos13,
            c.Pos14, c.Pos15, c.Pos16, c.Pos17, c.Pos18, c.Pos19, c.Pos20
        };
    }
    public static Chessman[] Poses(this StaticArrangementTable c)
    {
        return new[]
        {
            c.Pos1, c.Pos2, c.Pos3, c.Pos4, c.Pos5, c.Pos6, c.Pos7, c.Pos8, c.Pos9, c.Pos10, c.Pos11, c.Pos12, c.Pos13,
            c.Pos14, c.Pos15, c.Pos16, c.Pos17, c.Pos18, c.Pos19, c.Pos20
        };
    }

    public static GameCardInfo GetInfo(this GameCard card) => GameCardInfo.GetInfo(card);
    public static int Power(this GameCard card) => card.CardCapability(card.GetInfo().Rare);

    public static int GetValue(this GameCard card)
    {
        var info = card.GetInfo();
        int chips = card.chips + DataTable.CardLevel.Where(lv => lv.Key <= card.level).Sum(kv => kv.Value.ChipsConsume);
        int golds = 0;
        switch (info.Rare)
        {
            case 1:
                golds = 10;
                break;
            case 2:
                golds = 20;
                break;
            case 3:
                golds = 50;
                break;
            case 4:
                golds = 100;
                break;
            case 5:
                golds = 200;
                break;
            case 6:
                golds = 500;
                break;
            default:
                break;
        }
        return golds * chips;
    }

}

public enum GuideProps
{
    Card,
    Player,
    Enemy
}

/// <summary>
/// 卡牌信息
/// </summary>
public class GameCardInfo
{
    public static GameCardInfo GetInfo(GameCard card) => GetInfo((GameCardType) card.typeIndex, card.id);

    public static GameCardInfo GetInfo(GameCardType type,int id)
    {
        switch (type)
        {
            case GameCardType.Hero:
            {
                var c = DataTable.Hero[id];
                var military = DataTable.Military[c.MilitaryUnitTableId];
                return new GameCardInfo(c.Id, GameCardType.Hero, c.Name, c.Intro, c.Rarity, c.ForceTableId,
                    c.ImageId, c.IsProduce > 0, military.Short, c.GameSetRecovery, c.Damages, c.Hps, c.CombatType,
                    c.DamageType, military.Info, IsNonGeneralDamageMilitary(military));
            }
            case GameCardType.Tower:
            {
                var c = DataTable.Tower[id];
                return new GameCardInfo(c.Id, GameCardType.Tower, c.Name, c.Intro, c.Rarity, c.ForceId,
                    c.ImageId, c.IsProduce > 0, c.Short, c.GameSetRecovery, c.Damages, c.Hps, 1, 0, c.About,false);
            }
            case GameCardType.Trap:
            {
                var c = DataTable.Trap[id];
                return new GameCardInfo(c.Id, GameCardType.Trap, c.Name, c.Intro, c.Rarity, c.ForceId, c.ImageId,
                    c.IsProduce > 0, c.Short, c.GameSetRecovery, c.Damages, c.Hps, 0, 0, c.About,false);
            }
            default:
                throw new ArgumentOutOfRangeException($"type = {type}, id = {id}", type, null);
        }

        bool IsNonGeneralDamageMilitary(MilitaryTable military) =>
            military.Id == 28 || military.Id == 29 || military.Id == 32 || military.Id == 33;
    }

    public static GameCardInfo RandomPick(GameCardType type, int rare)
    {
        var ids = new List<int>();
        switch (type)
        {
            case GameCardType.Hero:
                ids = DataTable.Hero.Values.Where(c => c.Rarity == rare).Select(c=>c.Id).ToList();
                break;
            case GameCardType.Tower:
                ids = DataTable.Tower.Values.Where(c => c.Rarity == rare).Select(c=>c.Id).ToList();
                break;
            case GameCardType.Trap:
                ids = DataTable.Trap.Values.Where(c => c.Rarity == rare).Select(c=>c.Id).ToList();
                break;
            default:
                throw new ArgumentOutOfRangeException($"type = {type}, rare = {rare}", type, null);
        }
        var pick = Random.Range(0, ids.Count);
        var id = ids[pick];
        return GetInfo(type, id);
    }

    public int Id { get; }
    public GameCardType Type { get; }
    public string Name { get; private set; }
    public string Intro { get; private set; }
    public string About { get; }
    public int Rare { get; }
    public int ForceId { get; }
    public int ImageId { get; }
    public bool IsProduce { get; }
    public string Short { get; }
    public int GameSetRecovery { get; }
    public int CombatType { get; }
    public int DamageType { get; }
    public bool IsNonGeneralDamage { get; }
    private readonly Dictionary<int, int> damageMap;
    private readonly Dictionary<int, int> hpsMap;
    public IReadOnlyDictionary<int, int> DamageMap => damageMap;
    public IReadOnlyDictionary<int, int> HpMap => hpsMap;

    private GameCardInfo(int id, GameCardType type, string name, string intro, int rare, int forceId, int imageId,
        bool isProduce, string @short, int gameSetRecovery, int[] damages, int[] hps, int combatType, int damageType, string about, bool isNonGeneralDamage)
    {
        Id = id;
        Name = name;
        Intro = intro;
        Rare = rare;
        ForceId = forceId;
        ImageId = imageId;
        IsProduce = isProduce;
        Short = @short;
        GameSetRecovery = gameSetRecovery;
        CombatType = combatType;
        DamageType = damageType;
        About = about;
        IsNonGeneralDamage = isNonGeneralDamage;
        Type = type;
        damageMap = new Dictionary<int, int>();
        hpsMap = new Dictionary<int, int>();
        var index = 1;
        foreach (var damage in damages)
        {
            damageMap.Add(index, damage);
            index++;
        }
        index = 1;
        foreach (var hp in hps)
        {
            hpsMap.Add(index, hp);
            index++;
        }
    }

    public void Rename(string name, string nickname, string sign)
    {
        Name = name;
        Intro = $"字 【{nickname}】\n {sign}";
    }

    public int GetDamage(int level) => damageMap[level];
    public int GetHp(int level) => hpsMap[level];

    public Color GetNameColor()
    {
        switch (Rare)
        {
            case 1:
                return ColorDataStatic.name_gray;
            case 2:
                return ColorDataStatic.name_green;
            case 3:
                return ColorDataStatic.name_blue;
            case 4:
                return ColorDataStatic.name_purple;
            case 5:
                return ColorDataStatic.name_orange;
            case 6:
                return ColorDataStatic.name_red;
            case 7:
                return ColorDataStatic.name_black;
            default:
                return ColorDataStatic.name_gray;
        }
    }
}

/// <summary>
/// 兵种信息(仅限英雄类)
/// </summary>
public class MilitaryInfo
{
    public static MilitaryInfo GetInfo(int heroId) => new MilitaryInfo(DataTable.Military[DataTable.Hero[heroId].MilitaryUnitTableId]);
    public int Id { get; }
    public string Name { get; }
    public string Specialty { get; }
    public string Short { get; }
    public string Info { get; }
    public int ArmedType { get; }

    private MilitaryInfo(MilitaryTable t)
    {
        Id = t.Id;
        Name = t.Type;
        Specialty = t.Specialty;
        Short = t.Short;
        Info = t.Info;
        ArmedType = t.ArmedType;
    }
}

/// <summary>
/// 英雄战斗信息结构
/// </summary>
public class HeroCombatInfo
{
    public static HeroCombatInfo GetInfo(int heroId) => new HeroCombatInfo (DataTable.Hero[heroId]);
    public int DodgeRatio { get; }
    public int Armor { get; }
    public int CriticalRatio { get; }
    public int CriticalDamage { get; }
    public int RouseRatio { get; }
    public int RouseDamage { get; }
    public int ConditionResist { get; }
    public int PhysicalResist { get; }
    public int MagicResist { get; }

    private HeroCombatInfo(HeroTable h)
    {
        DodgeRatio = h.DodgeRatio;
        Armor = h.ArmorResist;
        CriticalRatio = h.CriticalRatio;
        CriticalDamage = h.CriticalDamage;
        RouseRatio = h.RouseRatio;
        RouseDamage = h.RouseDamage;
        ConditionResist = h.ConditionResist;
        PhysicalResist = h.PhysicalResist;
        MagicResist = h.MagicResist;
    }

    public float GetRouseDamage(int damage) => RouseDamage / 100f * damage;
    public float GetCriticalDamage(int damage) => CriticalDamage / 100f * damage;
}