using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Editor;
using DG.Tweening;
using UnityEngine;

public class WarDataMocker : MonoBehaviour
{
    public int warId;
    [Tooltip("初始金币")]public int gold;//初始金币
    [Tooltip("初始城市等级")]public int cityLevel;//初始城市等级
    [Header("是否客制化卡牌，不点就用当前存档")]
    [Tooltip("是否选择客制化卡牌组合")]public bool isCustomCard;//是否客制化卡牌
    [Tooltip("只有选存档才会有效")]public ForceFlags force;
    public PlayerDataMock playerData;
    [Header("这里是客制化卡牌，必须点了客制化才会使用")]
    public MyCard[] heroes;
    public MyCard[] towers;
    public MyCard[] traps;
    public GameResources gameResources = new GameResources();

#if UNITY_EDITOR
    private void Start()
    {
        GameSystem.Instance.Init();
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => GameSystem.IsInit);
        playerData.WarReward = new WarReward("test", warId, 0);
        playerData.WarType = PlayerDataForGame.WarTypes.Expedition;
        playerData.selectedWarId = warId;
        playerData.zhanYiColdNums = gold;
        PrepareCards();
    }
#endif

    private void PrepareCards()
    {
        var hst = PlayerDataForGame.instance.hstData;
        var hfMap = DataTable.Hero.Values.Select(card =>
        {
            var id = card.Id;
            var origin = card.ForceTableId;
            return new {id, origin};
        }).ToDictionary(c => c.id, c => c.origin);
        Dictionary<int, List<int>> cards;
        var forceId = -2;//客制化阵容
        if (!isCustomCard)
        {
            forceId = (int) force;
            cards = hst.heroSaveData.Concat(hst.trapSaveData.Concat(hst.towerSaveData)) //合并所有卡牌
                .Where(c => hfMap[c.id] == forceId && c.IsEnlistAble()) //过滤选中势力，并符合出战条件
                .GroupBy(c => c.typeIndex, c => c.id, (type, ids) => new {type, ids}) //把卡牌再根据卡牌类型分类组合
                .ToDictionary(c => c.type, c => c.ids.ToList()); //根据卡牌类型写入字典
        }
        else
        {
            cards = heroes.Select(c => new GameCard {id = c.CardId, level = c.Level, typeIndex = 0})
                .Concat(towers.Select(t => new GameCard {id = t.CardId, level = t.Level, typeIndex = 2}))
                .Concat(traps.Select(t => new GameCard {id = t.CardId, level = t.Level, typeIndex = 3}))
                .GroupBy(c => c.typeIndex, c => c.id, (type, cIds) => new {type, cIds})
                .ToDictionary(c => c.type, c => c.cIds.ToList());
        }
        PlayerDataForGame.instance.WarForceMap[PlayerDataForGame.WarTypes.Expedition] = forceId;

        PlayerDataForGame.instance.fightHeroId = cards.ContainsKey(0) ? cards[0] : new List<int>();
        PlayerDataForGame.instance.fightTowerId = cards.ContainsKey(2) ? cards[2] : new List<int>();
        PlayerDataForGame.instance.fightTrapId = cards.ContainsKey(3) ? cards[3] : new List<int>();
        if(cityLevel==0)
            XDebug.LogError<WarDataMocker>("城池等级为0，请设置初始城池等级");
        if(WarsUIManager.instance)
        {
            WarsUIManager.instance.cityLevel = cityLevel;
            WarsUIManager.instance.Init();
        }
    }
    [Serializable]
    public class MyCard
    {
        public int CardId;
        public int Level;
    }
}
