using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

/// <summary>
/// 霸业管理类
/// </summary>
public class BaYeManager : MonoBehaviour
{
    public enum EventTypes
    {
        City,
        Story
    }
    public enum StoryEventTypes
    {
        无事件,
        开箱子,
        答题,
        讨伐,
        战令
    }

    public int BaYeGoldDefault = 30; //霸业初始金币
    public int BaYeMaxGold = 75; //霸业金币上限
    private List<BaYeCityEvent> map;
    private Dictionary<int, int[]> eventPointAndStoriesMap;//数据表缓存
    public bool isShowTips;//是否弹出文字
    public string tipsText;//弹出文字内容

    public IReadOnlyList<BaYeCityEvent> Map => map;
    public static BaYeManager instance;
    private bool isHourlyEventRegistered;
    public EventTypes CurrentEventType { get; private set; }//当前事件类型
    public int CurrentEventPoint { get; private set; }//当前事件点
    public BaYeStoryEvent CachedStoryEvent { get; private set; }//当前缓存的故事事件

    void Awake()
    {
        if (instance == null)
            instance = this;
        if (instance != this)
            Destroy(this);
    }

    public void Init()
    {
        var baYe = GamePref.GetBaYe;
        PlayerDataForGame.instance.baYe = baYe;
        var zhanLing = DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level].BaYeForceLings;
        var zhanLingMap = new Dictionary<int, int>();
        for (int i = 0; i < zhanLing.Length; i++) zhanLingMap.Add(i, zhanLing[i]);
        //如果没有霸业记录或是记录已经过期(不是今天)将初始化新的霸业记录
        if (baYe == null || !SystemTimer.IsToday(baYe.lastBaYeActivityTime))
        {
            PlayerDataForGame.instance.baYe = new BaYeDataClass
            {
                lastBaYeActivityTime = SystemTimer.instance.NowUnixTicks,
                gold = BaYeGoldDefault,
                openedChest = new bool[UIManager.instance.baYeChestButtons.Length],
                zhanLingMap = zhanLingMap
            };
            GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
        }
        InitBaYeMap();
    }

    private void InitBaYeMap()
    {
        var typeName = nameof(BaYeManager);
        try
        {

            //初始化城池
            map = DataTable.BaYeCity.Values.Select(city =>
            {
                var cityId = city.EventPoint;
                var baYeEventId = city.BaYeCityEventTableIds.Select(id =>
                    new BaYeEventWeightElement(id, DataTable.BaYeCityEvent[id].Weight)).Pick().Id;
                //根据地图获取对应的事件id列表，并根据权重随机获取一个事件id
                var baYeEvent = GetBaYeEvent(baYeEventId, cityId);
                return new BaYeCityEvent
                    {CityId = cityId, EventId = baYeEventId, ExpList = baYeEvent.ExpList, WarIds = baYeEvent.WarIds};
            }).ToList(); //根据权重随机战役id
        }
        catch (Exception e)
        {
            throw XDebug.Throw(e, "InitMap", typeName);
        }

        var baYe = PlayerDataForGame.instance.baYe;
        try
        {
            foreach (var baYeEvent in baYe.data)
            {
                if (map.All(c => c.CityId != baYeEvent.CityId))
                    throw new InvalidOperationException($"找不到城池[{baYeEvent.CityId}]");
                map[baYeEvent.CityId] = baYeEvent;
            }
        }
        catch (Exception e)
        {
            throw XDebug.Throw(e, "Set cities", typeName);
        }

        //初始化故事事件
        //事件点初始化
        var now = DateTime.Now;
        try
        {
            eventPointAndStoriesMap = DataTable.BaYeStoryPool.Values
                .Where(m => DataTable.BaYeStoryEvent[m.EventId].Time.IsTableTimeInRange(now))
                .ToDictionary(m => m.EventId, m => m.BaYeStoryTableIds); //读取数据表转化城key=事件点,value=事件列

        }
        catch (Exception e)
        {
            throw XDebug.Throw(e, "Set Event point and pool", typeName);
        }

        if (isHourlyEventRegistered) return;
        isHourlyEventRegistered = true;
        TimeSystemControl.instance.OnHourly += GenerateBaYeStoryEvents;
        if (SystemTimer.instance.Now - PlayerDataForGame.instance.baYe.lastStoryEventsRefreshHour >=
#if UNITY_EDITOR
            TimeSpan.FromHours(1) //调试期间每次开启游戏都会刷新霸业
#else
            TimeSpan.FromHours(1)
#endif
        ) //如果现在时间与上一次刷新时间(大于)相差1小时以上
            GenerateBaYeStoryEvents(); //刷新霸业事件
    }

    /// <summary>
    /// 刷新霸业故事事件
    /// </summary>
    public void GenerateBaYeStoryEvents()
    {
        //获取玩家等级可开启的事件点id列表
        //获取每个故事id的权重
        //根据权重随机故事事件(story)id
        //再每个事件点上的故事id随机行上的奖励范围
        //储存在霸业存档里
        //事件点和故事信息
        var eventPointStoryMap = DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level].BaYeStoryPoints
            .Join(eventPointAndStoriesMap, pId => pId, se => se.Key, (point, s) => new {point, storyIds = s.Value})
            .Select(s =>
            {
                return new
                {
                    s.point, storyEvents = s.storyIds.Select(id =>
                    {
                        var story = DataTable.BaYeStoryEvent[id];
                        return new StoryEvent(id, story.Weight, story.StoryType,
                            (story.Gold.Min, story.Gold.IncMax),
                            (story.Exp.Min, story.Exp.IncMax),
                            (story.YvQue.Min, story.YvQue.IncMax),
                            (story.YuanBao.Min, story.YuanBao.IncMax),
                            (story.ForceLing.Min, story.ForceLing.IncMax));
                    })
                };
            })
            .ToDictionary(e => e.point, e => e.storyEvents.Pick());
        PlayerDataForGame.instance.baYe.storyMap = eventPointStoryMap.ToDictionary(m => m.Key, m =>
        {
            var story = m.Value;
            var warId = DataTable.BaYeStoryEvent[story.Id].WarId;
            warId = warId == default ? -1 : warId;
            var amount = story.Type == 4 ? 2 : 1;//答题(4)会生成两个战令供选择
            return new BaYeStoryEvent
            {
                StoryId = story.Id,
                Type = story.Type,
                GoldReward = Random.Range(story.GoldRange.Item1, story.GoldRange.Item2),
                ExpReward = Random.Range(story.ExpRange.Item1, story.ExpRange.Item2),
                YvQueReward = Random.Range(story.YuQueRange.Item1, story.YuQueRange.Item2),
                YuanBaoReward = Random.Range(story.YuanBaoRange.Item1, story.YuanBaoRange.Item2),
                WarId = warId,
                ZhanLing = GetZhanLing(amount,story.ZhanLingRange.Item1, story.ZhanLingRange.Item2)
            };
        }).ToDictionary(kv => kv.Key, kv => kv.Value);
        PlayerDataForGame.instance.baYe.lastStoryEventsRefreshHour =
            SystemTimer.instance.Now.Date.AddHours(SystemTimer.instance.Now.Hour);
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
        UIManager.instance.storyEventUiController.ResetUi();
        if(GameSystem.CurrentScene == GameSystem.GameScene.MainScene)
        {
            SelectorUIMove(false, null);
        }

        Dictionary<int, int> GetZhanLing(int amount,int min, int incMax)
        {
            var forceList = DataTable.Force.Keys.ToList();
            if (forceList.Count < amount)
                throw XDebug.Throw<BaYeManager>($"要求生成的数量[{amount}]大于军团数[{forceList.Count}]");
            var zhanLingSelection = new Dictionary<int, int>();
            for (int i = 0; i < amount; i++)
            {
                var index = Random.Range(0, forceList.Count);
                var pick = forceList[index];
                zhanLingSelection.Add(pick, Random.Range(min, incMax + 1));
                forceList.Remove(pick);
            }
            return zhanLingSelection;
        }
    }

    public void OnBaYeWarEventPointSelected(EventTypes type,int eventPoint)
    {
        var isWarEvent = false;
        switch (type)
        {
            case EventTypes.City:
            {
                isWarEvent = true;
                var cEvent = map.Single(e => e.CityId == eventPoint);
                PlayerDataForGame.instance.selectedCity = cEvent.CityId;
                PlayerDataForGame.instance.selectedBaYeEventId = cEvent.EventId;
                SelectorUIMove(cEvent.EventId == PlayerDataForGame.instance.selectedBaYeEventId,
                    UIManager.instance.baYeBattleEventController.eventList[cEvent.CityId].transform);
                print(string.Join(",", cEvent.WarIds));
            }
                break;
            case EventTypes.Story:
            {
                var storyMap = PlayerDataForGame.instance.baYe.storyMap;
                if (!storyMap.ContainsKey(eventPoint))
                    throw XDebug.Throw<BaYeManager>("霸业故事点不存在!");
                var sEvent = storyMap[eventPoint];
                OnStoryEventTrigger(eventPoint,sEvent);
                isWarEvent = (StoryEventTypes) sEvent.Type == StoryEventTypes.讨伐;
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        if (!isWarEvent) return;
        //讨伐事件将记录讨伐类型和讨伐点
        CurrentEventType = type;
        CurrentEventPoint = eventPoint;
    }

    //选择器UI
    private void SelectorUIMove(bool display,Transform targetTransform)
    {
        var selector = UIManager.instance.chooseBaYeEventImg;
        selector.SetActive(display);
        if (!display)
        {
            selector.transform.position = Vector3.zero;
            return;
        }
        selector.transform.position = targetTransform.position;
    }

    private BaYeCityEvent GetBaYeEvent(int eventId,int cityId)
    {
        var baYeEvent = DataTable.BaYeCityEvent[eventId];//读取事件数据
        var mappingTable = DataTable.BaYeLevelMapping[baYeEvent.BaYeLevelMappingId];//获取对应战斗表

        return new BaYeCityEvent {EventId = eventId, CityId = cityId, WarIds = GetWarList(mappingTable).ToList(), ExpList = baYeEvent.BaYeExps.ToList(), PassedStages = new bool[baYeEvent.BaYeExps.Length]};
    }
    private int[] GetWarList(BaYeLevelMappingTable mapping)
    {
        var levelMappingId = DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level].BaYeLevel;//根据等级表获取战斗id
        var levels = new[]
        {
            mapping.Level0, mapping.Level1, mapping.Level2, mapping.Level3, mapping.Level4, mapping.Level5,
            mapping.Level6, mapping.Level7, mapping.Level8, mapping.Level9
        };
        return levels[levelMappingId];
    }
    public void AddExp(int expIndex,int exp)
    {
        if (PlayerDataForGame.instance.baYe.ExpData.ContainsKey(expIndex))
            PlayerDataForGame.instance.baYe.ExpData[expIndex] += exp;
        else PlayerDataForGame.instance.baYe.ExpData.Add(expIndex, exp);
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
    }

    public void SetGold(int value)
    {
        PlayerDataForGame.instance.baYe.gold = value > BaYeMaxGold ? BaYeMaxGold : value;
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
    }

    public void AddGold(int gold)
    {
        PlayerDataForGame.instance.baYe.gold += gold;
        if (PlayerDataForGame.instance.baYe.gold > BaYeMaxGold)
            PlayerDataForGame.instance.baYe.gold = BaYeMaxGold;
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
    }

    public bool TradeZhanLing(int forceId, int amount)
    {
        if (!PlayerDataForGame.instance.baYe.zhanLingMap.ContainsKey(forceId))
            PlayerDataForGame.instance.baYe.zhanLingMap.Add(forceId, 0);
        if (amount < 0 && PlayerDataForGame.instance.baYe.zhanLingMap[forceId] < -amount)
            return false; //如果数目是负数，并玩家数量小于数量返回交易失败
        PlayerDataForGame.instance.baYe.zhanLingMap[forceId] += amount;
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
        return true;
    }

    public void OnBayeStoryEventReward(BaYeStoryEvent storyEvent)
    {
        var expIndex = -1; //霸业故事事件送的expId一律 -1
        var baYe = PlayerDataForGame.instance.baYe;
        if (baYe.ExpData.ContainsKey(expIndex))
            baYe.ExpData[expIndex] += storyEvent.ExpReward;
        else baYe.ExpData.Add(expIndex, storyEvent.ExpReward);
        baYe.gold += storyEvent.GoldReward;
        if (baYe.gold > BaYeMaxGold) //不超过上限
            baYe.gold = BaYeMaxGold;
        //战令
        foreach (var ling in storyEvent.ZhanLing)
        {
            baYe.zhanLingMap.Trade(ling.Key, ling.Value, true);
        }
        //todo 霸业暂时没有元宝和玉阙的事件，所以不再更新玉阙和元宝
        //var py = PlayerDataForGame.instance.pyData;
        //if (storyEvent.YvQueReward > 0) py.YvQue += storyEvent.YvQueReward;
        //if (storyEvent.YuanBaoReward > 0) py.YuanBao += storyEvent.YuanBaoReward;
        //if(GameSystem.CurrentScene == GameSystem.GameScene.MainScene)
        //{
        //    UIManager.instance.yuanBaoNumText.text = py.YvQue.ToString();
        //    UIManager.instance.yvQueNumText.text = py.YvQue.ToString();
        //}
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
        //PlayerDataForGame.instance.isNeedSaveData = true;
        //LoadSaveData.instance.SaveGameData(5);
    }

    public void SetExp(int expIndex,int exp)
    {
        if (PlayerDataForGame.instance.baYe.ExpData.ContainsKey(expIndex))
            PlayerDataForGame.instance.baYe.ExpData[expIndex] = exp;
        else PlayerDataForGame.instance.baYe.ExpData.Add(expIndex, exp);
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);
    }

    //当霸业故事事件被触发
    private void OnStoryEventTrigger(int eventPoint,BaYeStoryEvent sEvent)
    {
        switch ((StoryEventTypes)sEvent.Type)
        {
            case StoryEventTypes.无事件: break;
            case StoryEventTypes.开箱子:
                OnReward(sEvent);
                break;
            case StoryEventTypes.答题:
            {
                var pick = Random.Range(0, DataTable.Quest.Count);
                UIManager.instance.baYeWindowUi.ShowQuest(DataTable.Quest[pick], () =>
                    {
                        OnReward(sEvent);
                        PlayerDataForGame.instance.ShowStringTips("三日不见，当刮目相看！");
                    },
                    () => PlayerDataForGame.instance.ShowStringTips("糊涂啊~"));
                break;
            }
            case StoryEventTypes.讨伐:
            {
                //标记霸业的战斗点。等待开启战斗
                SelectorUIMove(true, UIManager.instance.storyEventUiController.storyEventPoints[eventPoint].transform);
                print($"故事type[{sEvent.Type}]-讨伐事件 storyId[{sEvent.StoryId}] warId[{sEvent.WarId}]");
                return;//征战活动不删除记录。等到触发了才删除
            }
            case StoryEventTypes.战令:
            {
                UIManager.instance.baYeWindowUi.ShowSelection(sEvent.ZhanLing, selectedId =>
                {
                    var message = TradeZhanLing(selectedId, sEvent.ZhanLing[selectedId])
                        ? $"获得[{DataTable.Force[selectedId].Short}]战令"
                        : "战令获取异常！";
                    UIManager.instance.baYeForceSelectorUi.UpdateZhanLing();
                    PlayerDataForGame.instance.ShowStringTips(message);
                });
                break;
            }
            default:
                XDebug.LogError<BaYeManager>($"未知故事事件类型={sEvent.Type}!");
                throw new ArgumentOutOfRangeException();
        }
        PlayerDataForGame.instance.baYe.storyMap.Remove(eventPoint);
        GamePref.SaveBaYe(PlayerDataForGame.instance.baYe);

        void OnReward(BaYeStoryEvent se)
        {
            var rewardMap = InstanceReward(se);
            UIManager.instance.baYeWindowUi.Show(rewardMap);
            OnBayeStoryEventReward(se);
            UIManager.instance.baYeWindowUi.ShowAdButton(success =>
            {
                if (!success) return;
                UIManager.instance.baYeWindowUi.Show(InstanceReward(se, 2));
                OnBayeStoryEventReward(se);
                UIManager.instance.ResetBaYeProgressAndGold();
            });
            UIManager.instance.ResetBaYeProgressAndGold();

            Dictionary<int, int> InstanceReward(BaYeStoryEvent baYeStoryEvent, int rate = 1) =>
                new Dictionary<int, int>()
                {
                    {0, baYeStoryEvent.GoldReward * rate},
                    {1, baYeStoryEvent.ExpReward * rate},
                    {2, baYeStoryEvent.YuanBaoReward * rate},
                    {3, baYeStoryEvent.YvQueReward * rate}
                };
        }
    }


    private class StoryEvent : IWeightElement
    {
        public int Id { get; }
        public int Weight { get; }
        public int Type { get; }
        public (int,int) GoldRange { get; }
        public (int,int) ExpRange { get; }
        public (int,int) YuQueRange { get; }
        public (int,int) YuanBaoRange { get; }
        public (int,int) ZhanLingRange { get; }

        public StoryEvent(int id, int weight, int type, (int, int) goldRange, (int, int) expRange,(int,int) yuQueRange,(int,int) yuanBaoRange,(int,int) zhanLingRange)
        {
            Id = id;
            Weight = weight;
            Type = type;
            GoldRange = goldRange;
            ExpRange = expRange;
            YuQueRange = yuQueRange;
            YuanBaoRange = yuanBaoRange;
            ZhanLingRange = zhanLingRange;
        }
    }

    private class BaYeEventWeightElement : IWeightElement
    {
        public int Id { get; set; }
        public int Weight { get; set; }

        public BaYeEventWeightElement(int id, int weight)
        {
            Id = id;
            Weight = weight;
        }
    }

    public void CacheCurrentStoryEvent() => CachedStoryEvent = PlayerDataForGame.instance.baYe.storyMap[CurrentEventPoint];
}