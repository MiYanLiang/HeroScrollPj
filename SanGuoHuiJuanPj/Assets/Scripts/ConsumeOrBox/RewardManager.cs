using System;
using System.Collections;
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
                PlayerDataForGame.instance.hstData.heroSaveData.GetOrInstance(cardId, cardType, 0).Chips += chips;
                break;
            case GameCardType.Tower:
                PlayerDataForGame.instance.hstData.towerSaveData.GetOrInstance(cardId, cardType, 0).Chips += chips;
                break;
            case GameCardType.Trap:
                PlayerDataForGame.instance.hstData.trapSaveData.GetOrInstance(cardId, cardType, 0).Chips += chips;
                break;
            case GameCardType.Spell:
            case GameCardType.Soldier:
            case GameCardType.Base:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cardType), cardType, null);
        }
    }
}
