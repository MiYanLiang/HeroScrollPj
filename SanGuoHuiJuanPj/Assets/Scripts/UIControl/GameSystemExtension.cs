﻿using CorrelateLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.System.WarModule;
using UnityEngine;
using Random = UnityEngine.Random;

public static class GameSystemExtension
{
    public static T RandomPick<T>(this IEnumerable<T> data,[CallerMemberName]string method = null) where T : struct
    {
        var list = data.ToList();
        if (!list.Any())
            throw new InvalidOperationException($"{nameof(RandomPick)} of {method}(): Items is 0!");
        var pick = Random.Range(0, list.Count);
        return list[pick];
    }

    /// <summary>
    /// 出战？
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
        card.typeIndex = (int) type;
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
    /// <summary>
    /// 卡牌出售价格
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
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
                    c.ImageId, c.Rarity, c.HitPoint, c.Strength, military.CombatStyle, military.Element, c.ForceTableId,
                    c.IsProduce > 0, c.GameSetRecovery);
            }
            case GameCardType.Tower:
            {
                var c = DataTable.Tower[id];
                return new GameCardInfo(c.Id, GameCardType.Tower, c.Name, c.Short, c.Intro, c.About, c.ImageId,
                    c.Rarity, c.HitPoint, c.Strength, combatType: 1, element: 0, c.ForceId, c.IsProduce > 0, c.GameSetRecovery);
            }
            case GameCardType.Trap:
            {
                var c = DataTable.Trap[id];
                return new GameCardInfo(c.Id, GameCardType.Trap, c.Name, c.Short, c.Intro, c.About, c.ImageId, c.Rarity,
                    c.HitPoint, c.Strength, combatType: 0, element: 0, c.ForceId, c.IsProduce > 0, c.GameSetRecovery);
            }
            case GameCardType.Base:
                return new GameCardInfo(id: 0, type: GameCardType.Base, name: string.Empty, @short: string.Empty, intro: string.Empty, about: string.Empty, imageId: 0,
                    rare: 1, hitPoint: 0, strength: 0, combatType: -1, element: 0, forceId: -1, isProduce: false, gameSetRecovery: 0);
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
    public int Element { get; }
    public int Strength { get; }
    public int HitPoint { get; }

    private GameCardInfo(int id, GameCardType type, string name, string @short, string intro, string about, int imageId,
        int rare, int hitPoint, int strength, int combatType, int element, int forceId, bool isProduce, int gameSetRecovery)
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

    public int GetDamage(int level) => CombatStyle.DamageFormula(Strength, level);

    public int GetHp(int level) => CombatStyle.HitPointFormula(HitPoint, level);
}