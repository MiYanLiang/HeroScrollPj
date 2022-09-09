using CorrelateLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.System.WarModule;


public static class GameSystemExtension
{
    public static readonly Random Random = new Random();
    public static T RandomPick<T>(this IEnumerable<T> data,[CallerMemberName]string method = null) where T : struct
    {
        var list = data.ToList();
        if (!list.Any())
            throw new InvalidOperationException($"{nameof(RandomPick)} of {method}(): Items is 0!");
        var pick = Random.Next(0, list.Count);
        return list[pick];
    }

    /// <summary>
    /// 出战？
    /// </summary>
    /// <param name="cards"></param>
    /// <param name="forceId">军团Id</param>
    /// <param name="isFight"></param>
    /// <returns></returns>
    public static IEnumerable<GameCard> Enlist(this IEnumerable<GameCard> cards, int forceId, bool isFight = true) =>
        isFight
            ? cards.Where(card => GetForceId(card) == forceId && card.Level > 0 && card.IsFight > 0)
            : cards.Where(card => GetForceId(card) == forceId && card.Level > 0);
    public static bool IsEnlistAble(this GameCard card) => card.Level > 0;
    public static int GetForceId(this GameCard card)
    {
        //单位类型0武将 1士兵 2塔 3陷阱 4技能
        int force = -1;
        switch (card.Type)
        {
            case 0 : 
                force = DataTable.Hero[card.CardId].ForceTableId;
                break;
            case 2 :
                force = DataTable.Tower[card.CardId].ForceId;
                break;
            case 3 :
                force = DataTable.Trap[card.CardId].ForceId;
                break;
        }
        return force;
    }

    public static GameCard GetOrInstance(this List<GameCard> cards, int cardId, GameCardType cardType, int level)
    {
        var card = cards.SingleOrDefault(c => c.CardId == cardId);
        if (card != null) return card;
        card = GameCard.Instance(cardId, cardType, level, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        cards.Add(card);
        return card;
    }

    public static Dictionary<int, Chessman> Poses(this GuideTable c, GuideProps prop)
    {
        switch (prop)
        { 
            case GuideProps.Card:
                return new Dictionary<int, Chessman>
                {
                    { 0, c.Card1 }, { 1, c.Card2 }, { 2, c.Card3 }, { 3, c.Card4 },
                    { 4, c.Card5 }
                };
            case GuideProps.Player:
                return new Dictionary<int, Chessman>
                {
                    { 0, c.Pos1 }, { 1, c.Pos2 }, { 2, c.Pos3 }, { 3, c.Pos4 }, { 4, c.Pos5 }, { 5, c.Pos6 },
                    { 6, c.Pos7 }, { 7, c.Pos8 }, { 8, c.Pos9 }, { 9, c.Pos10 }, { 10, c.Pos11 }, { 11, c.Pos12 },
                    { 12, c.Pos13 }, { 13, c.Pos14 }, { 14, c.Pos15 }, { 15, c.Pos16 }, { 16, c.Pos17 }, { 17, c.Pos18 },
                    { 18, c.Pos19 }, { 19, c.Pos20 }
                };
            case GuideProps.Enemy:
                return new Dictionary<int,Chessman>
                {
                    { 0, c.EPos1 }, { 1, c.EPos2 }, { 2, c.EPos3 }, { 3, c.EPos4 }, { 4, c.EPos5 }, { 5, c.EPos6 },
                    { 6, c.EPos7 }, { 7, c.EPos8 }, { 8, c.EPos9 }, { 9, c.EPos10 }, { 10, c.EPos11 },
                    { 11, c.EPos12 }, { 12, c.EPos13 }, { 13, c.EPos14 }, { 14, c.EPos15 }, { 15, c.EPos16 },
                    { 16, c.EPos17 }, { 17, c.EPos18 }, { 18, c.EPos19 }, { 19, c.EPos20 }
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
    /// <summary>
    /// 卡牌出售价格
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public static int GetValue(this GameCard card)
    {
        var info = card.GetInfo();
        int chips = card.Chips + DataTable.CardLevel.Where(lv => lv.Key <= card.Level).Sum(kv => kv.Value.ChipsConsume);
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
    public static GameCardInfo GetInfo(GameCard card) => GetInfo((GameCardType) card.Type, card.CardId);
    public static GameCardInfo GetInfo(IChessman card) => GetInfo(card.CardType, card.CardId);

    public static GameCardInfo GetInfo(GameCardType type,int id)
    {
        switch (type)
        {
            case GameCardType.Hero:
            {
                var c = DataTable.Hero[id];
                var military = DataTable.Military[c.MilitaryUnitTableId];
                return new GameCardInfo(id, GameCardType.Hero, c.Name, military.Short, c.Intro, military.About,
                    c.ImageId, c.Rarity, c.HitPoint, c.Strength, c.Intelligent, c.Speed, c.DodgeRatio, c.ArmorResist,
                    c.MagicResist, c.CriticalRatio, c.RouseRatio, military.CombatStyle, military.Element,
                    c.ForceTableId,
                    c.IsProduce > 0, c.GameSetRecovery);
            }
            case GameCardType.Tower:
            {
                var c = DataTable.Tower[id];
                return new GameCardInfo(c.Id, GameCardType.Tower, c.Name, c.Short, c.Intro, c.About, c.ImageId,
                    c.Rarity, c.HitPoint, c.Strength,0,c.Speed, dodgeRatio: 0, armorResist: 0, magicResist: 0, criticalRatio: 0, rougeRatio: 0, combatType: 1, element: 0, c.ForceId, c.IsProduce > 0, c.GameSetRecovery);
            }
            case GameCardType.Trap:
            {
                var c = DataTable.Trap[id];
                return new GameCardInfo(c.Id, GameCardType.Trap, c.Name, c.Short, c.Intro, c.About, c.ImageId, c.Rarity,
                    c.HitPoint, c.Strength, intelligent: 0, speed: 0, dodgeRatio: 0, armorResist: 0, magicResist: 0, criticalRatio: 0, rougeRatio: 0, combatType: 0, element: 0, c.ForceId, c.IsProduce > 0, c.GameSetRecovery);
            }
            case GameCardType.Base:
                return new GameCardInfo(id: 0, type: GameCardType.Base, name: string.Empty, @short: string.Empty, intro: string.Empty, about: string.Empty, imageId: 0,
                    rare: 1, hitPoint: 0, strength: 0, intelligent: 0, speed: 0, dodgeRatio: 0, armorResist: 0, magicResist: 0, criticalRatio: 0, rougeRatio: 0,combatType: -1, element: 0, forceId: -1, isProduce: false, gameSetRecovery: 0);
            case GameCardType.Soldier:
            case GameCardType.Spell:
            default:
                throw new ArgumentOutOfRangeException($"type = {type}, id = {id}", type, null);
        }
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
        var pick = GameSystemExtension.Random.Next(0, ids.Count);
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
    public int Element { get; }
    public int Strength { get; }
    public int HitPoint { get; }
    public int Intelligent { get; }
    public int Speed { get; }
    public int DodgeRatio { get; }
    public int ArmorResist { get; }
    public int MagicResist { get; }
    public int CriticalRatio { get; }
    public int RougeRatio { get; }

    private GameCardInfo(int id, GameCardType type, string name, string @short, string intro, string about, int imageId,
        int rare, int hitPoint, int strength, int intelligent,int speed,int dodgeRatio,int armorResist,int magicResist,int criticalRatio, int rougeRatio,int combatType, int element, int forceId, bool isProduce, int gameSetRecovery)
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
        Element = element;
        About = about;
        Strength = strength;
        HitPoint = hitPoint;
        Intelligent = intelligent;
        Speed = speed;
        DodgeRatio = dodgeRatio;
        ArmorResist = armorResist;
        MagicResist = magicResist;
        CriticalRatio = criticalRatio;
        RougeRatio = rougeRatio;
        Type = type;
    }
    /// <summary>
    /// 玩家起名
    /// </summary>
    /// <param name="name"></param>
    /// <param name="nickname"></param>
    /// <param name="sign"></param>
    public void Rename(string name, string nickname, string sign)
    {
        Name = name;
        Intro = $"字 【{nickname}】\n {sign}";
    }
}