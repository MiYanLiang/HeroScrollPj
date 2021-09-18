using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public static RewardManager instance;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(this);
    }

    public int GetRandomYvQue(int chestId)
    {
        var yvQue = DataTable.WarChest[chestId].YvQue;
        return UnityEngine.Random.Range(yvQue.Min, yvQue.ExcMax);
    }
    public int GetRandomYuanBao(int chestId)
    {
        var yuanBao = DataTable.WarChest[chestId].YuanBao;
        return UnityEngine.Random.Range(yuanBao.Min, yuanBao.ExcMax);
    }

    public List<CardReward> GetCards(int chestId, bool isZyBox)
    {
        return RandomPickBaseOnStrategy(GameCardType.Trap, DataTable.WarChest[chestId].Trap.ToList())
            .Concat(RandomPickBaseOnStrategy(GameCardType.Tower, DataTable.WarChest[chestId].Tower.ToList()))
            .Concat(RandomPickBaseOnStrategy(GameCardType.Hero, DataTable.WarChest[chestId].Hero.ToList())).ToList();

        //private method
        List<CardReward> RandomPickBaseOnStrategy(GameCardType gameCardType,List<CardRandomProduction> items)
        {
            var list = new List<CardReward>();
            items.ForEach(r =>
            {
                if (r.Ratio >= UnityEngine.Random.Range(0, 101))
                {
                    var type = gameCardType;
                    var rewardCards = GetWeightList(type,isZyBox, r.Rare);
                    if (rewardCards.Count == 0) return;
                    var pick = rewardCards.PickOrDefault();
                    if (pick == null) return;
                    var chips = UnityEngine.Random.Range(r.MinChips, r.ExcMaxChips);
                    RewardCard(type, pick.cardId, chips);
                    list.Add(new CardReward
                    {
                        cardId = pick.cardId,
                        cardType = (int) gameCardType,
                        cardChips = chips
                    });
                }
            });
            return list;
        }
    }

    /// <summary>
    /// 返回随机选中卡牌id
    /// </summary>
    private List<CardIdAndWeights> GetWeightList(GameCardType cardType, bool isZhanYi, int rarity)
    {
        var level = PlayerDataForGame.instance.pyData.Level;
        switch (cardType)
        {
            case GameCardType.Hero:
                return DataTable.Hero.Where(c => c.Value.IsProduce > 0 && c.Value.Rarity == rarity)
                    .Select(c =>
                    {
                        var production = isZhanYi ? c.Value.ZhanYiChestProduction : c.Value.ConsumeChestProduction;
                        return new CardIdAndWeights
                        {
                            cardId = c.Key,
                            outputLevel = production.Level,
                            weight = production.Weight
                        };
                    }).Where(c => c.outputLevel != 0 && c.outputLevel <= level).ToList();
            case GameCardType.Tower:
                return DataTable.Tower.Where(c => c.Value.IsProduce > 0 && c.Value.Rarity == rarity)
                    .Select(c =>
                    {
                        var production = isZhanYi ? c.Value.ZhanYiChestProduction : c.Value.ConsumeChestProduction;
                        return new CardIdAndWeights
                        {
                            cardId = c.Key,
                            outputLevel = production.Level,
                            weight = production.Weight
                        };
                    }).Where(c => c.outputLevel != 0 && c.outputLevel <= level).ToList();
            case GameCardType.Trap:
                return DataTable.Trap.Where(c => c.Value.IsProduce > 0 && c.Value.Rarity == rarity)
                    .Select(c =>
                    {
                        var production = isZhanYi ? c.Value.ZhanYiChestProduction : c.Value.ConsumeChestProduction;
                        return new CardIdAndWeights
                        {
                            cardId = c.Key,
                            outputLevel = production.Level,
                            weight = production.Weight
                        };
                    }).Where(c => c.outputLevel != 0 && c.outputLevel <= level).ToList();
            default:
                throw new ArgumentOutOfRangeException(nameof(cardType), cardType, null);
        }
    }

    /// <summary>
    /// 获取并存储奖励碎片
    /// </summary>
    /// <param name="cardType">卡牌种类</param>
    /// <param name="cardId">具体id</param>
    /// <param name="chips">碎片数量</param>
    /// <returns></returns>
    public void RewardCard(GameCardType cardType, int cardId, int chips)
    {
        switch (cardType)
        {
            case GameCardType.Hero:
                PlayerDataForGame.instance.hstData.heroSaveData.GetOrInstance(cardId, cardType, 0).chips += chips;
                break;
            case GameCardType.Tower:
                PlayerDataForGame.instance.hstData.towerSaveData.GetOrInstance(cardId, cardType, 0).chips += chips;
                break;
            case GameCardType.Trap:
                PlayerDataForGame.instance.hstData.trapSaveData.GetOrInstance(cardId, cardType, 0).chips += chips;
                break;
            case GameCardType.Spell:
            case GameCardType.Soldier:
            case GameCardType.Base:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cardType), cardType, null);
        }
    }


    //卡牌和权重类
    private class CardIdAndWeights : IWeightElement
    {
        public int Weight => weight;
        public int cardId;
        public int weight;
        public int outputLevel;
    }
}
