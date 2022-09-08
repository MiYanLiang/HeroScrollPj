using System;
using System.Collections.Generic;
using System.Linq;
using Beebyte.Obfuscator;
using CorrelateLib;
using Newtonsoft.Json;

#region 玩家数据相关类

/// <summary>
/// 玩家账户信息存档
/// </summary>
public interface IUserInfo
{
    /// <summary>
    /// 账号
    /// </summary>
    string Username { get; set; }
    /// <summary>
    /// 密码
    /// </summary>
    string Password { get; set; }
    /// <summary>
    /// 手机号
    /// </summary>
    string Phone { get; set; }
    /// <summary>
    /// 硬件唯一标识id
    /// </summary>
    string DeviceId { get; set; }
    /// <summary>
    /// 与服务器最后一次互交的时间
    /// </summary>
    long LastUpdate { get; set; }
    /// <summary>
    /// 游戏版本号
    /// </summary>
    float GameVersion { get; set; }
}

/// <summary>
/// 玩家基本信息存档数据类
/// </summary>
public interface IPlayerData
{
    //等级
    int Level { get; set; }
    //经验
    int Exp { get; set; }
    //元宝
    int YuanBao { get; set; }
    //玉阙
    int YvQue { get; set; }
    //体力
    int Stamina { get; set; }
    //玩家初始势力id
    int ForceId { get; set; }
    //上次锦囊获取时间
    long LastJinNangRedeemTime { get; set; }
    //锦囊每天的获取次数
    int DailyJinNangRedemptionCount { get; set; }
    //上次酒坛获取时间
    long LastJiuTanRedeemTime { get; set; }
    //酒坛每天的获取次数
    int DailyJiuTanRedemptionCount { get; set; }
    //上次无双宝箱获取时间
    long LastFourDaysChestRedeemTime { get; set; }
    //上次史诗宝箱获取时间
    long LastWeekChestRedeemTime { get; set; }
    //上一个游戏版本号
    float LastGameVersion { get; set; }
    //免广告卷
    int AdPass { get; set; }
}

/// <summary>
/// 玩家上传下载的数据规范
/// </summary>
public interface IUserSaveArchive
{
    // 账号
    string Username { get; set; }
    // 密码
    string Password { get; set; }
    // 手机号
    string Phone { get; set; }
    // 硬件唯一标识id
    string DeviceId { get; set; }
    // 与服务器最后一次互交的时间
    long LastUpdate { get; set; }
    //玩家信息
    string PlayerInfo { get; set; }
    /// <summary>
    ///卡牌数据 HSTDataClass
    /// </summary>
    string CardsData { get; set; }
    /// <summary>
    /// 征战记录 WarsDataClass
    /// </summary>
    string Expedition { get; set; }
    /// <summary>
    ///奖励或兑换码记录 GetBoxOrCodeData
    /// </summary>
    string RewardsRecord { get; set; }
}
[Skip]
public class GetBoxOrCodeData
{
    //战役宝箱
    public List<int> fightBoxs = new List<int>();
    //兑换码
    public List<string> redemptionCodeGotList = new List<string>();
}
[Skip]
public class RCode
{
    public int id;      //兑换码id
    public bool isGot;  //是否领取过
}
[Skip]
public class GameCard : IGameCard
{
    public static GameCard InstanceHero(int cardId, int level, int arouse, int deputy1Id,
        int deputy1Level,
        int deputy2Id, int deputy2Level,
        int deputy3Id, int deputy3Level,
        int deputy4Id, int deputy4Level,
        int chips = 0) => Instance(cardId, GameCardType.Hero, level, arouse, deputy1Id, deputy1Level,
        deputy2Id, deputy2Level,
        deputy3Id, deputy3Level,
        deputy4Id, deputy4Level,
        chips);
    public static GameCard InstanceTower(int cardId, int level) =>
        Instance(cardId, GameCardType.Tower, level, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public static GameCard InstanceTrap(int cardId, int level) =>
        Instance(cardId, GameCardType.Trap, level, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public static GameCard Instance(GameCardDto dto) =>
        Instance(dto.CardId, (int)dto.Type, dto.Level, dto.Arouse, dto.Chips, 
            dto.Deputy1Id, dto.Deputy1Level, 
            dto.Deputy2Id, dto.Deputy2Level, 
            dto.Deputy3Id, dto.Deputy3Level, 
            dto.Deputy4Id, dto.Deputy4Level);
    public static GameCard Instance(IGameCard card)
    {
        return Instance(card.CardId, card.Type, card.Level, card.Arouse, card.Chips,
            card.Deputy1Id, card.Deputy1Level,
            card.Deputy2Id, card.Deputy2Level,
            card.Deputy3Id, card.Deputy3Level,
            card.Deputy4Id, card.Deputy4Level);
    }

    public static GameCard Instance(int cardId, GameCardType type, int level, int arouse, int deputy1Id,
        int deputy1Level,
        int deputy2Id, int deputy2Level,
        int deputy3Id, int deputy3Level,
        int deputy4Id, int deputy4Level,
        int chips = 0) => Instance(cardId, (int)type, level, arouse, deputy1Id, deputy1Level,
        deputy2Id, deputy2Level,
        deputy3Id, deputy3Level,
        deputy4Id, deputy4Level,
        chips);

    public static GameCard Instance(int cardId, int type, int level, int arouse, int deputy1Id, int deputy1Level,
        int deputy2Id, int deputy2Level,
        int deputy3Id, int deputy3Level,
        int deputy4Id, int deputy4Level,
        int chips = 0)
    {
        return new GameCard(cardId,type,level,chips,0,arouse,
            deputy1Id,deputy1Level,
            deputy2Id,deputy2Level,
            deputy3Id,deputy3Level,
            deputy4Id,deputy4Level)
        {
            CardId = cardId,
            Chips = chips, 
            Level = level, 
            Type = type,
            Arouse = arouse
        };
    }

    public int IsFight { get; set; }
    public int CardId { get; set; }
    public int Level { get; set; } = 1;
    public int Chips { get; set; }
    public int Type { get; set; }
    public int Arouse { get; private set; }
    public int Deputy1Id { get; set; }
    public int Deputy1Level { get; set; }
    public int Deputy2Id { get; set; }
    public int Deputy2Level { get; set; }
    public int Deputy3Id { get; set; }
    public int Deputy3Level { get; set; }
    public int Deputy4Id { get; set; }
    public int Deputy4Level { get; set; }
    [JsonConstructor]
    private GameCard(int id, int type, int level, int chips, int isFight, int arouse, int deputy1Id, int deputy1Level, int deputy2Id, int deputy2Level, int deputy3Id, int deputy3Level, int deputy4Id, int deputy4Level)
    {
        CardId = id;
        Level = level;
        Chips = chips;
        IsFight = isFight;
        Type = type;
        Arouse = arouse;
        Deputy1Id = deputy1Id;
        Deputy1Level = deputy1Level;
        Deputy2Id = deputy2Id;
        Deputy2Level = deputy2Level;
        Deputy3Id = deputy3Id;
        Deputy3Level = deputy3Level;
        Deputy4Id = deputy4Id;
        Deputy4Level = deputy4Level;
    }
}
/// <summary>
/// 武将，士兵，塔等 信息存档数据类
/// </summary>
[Skip]
public class HSTDataClass
{
    //武将
    public List<GameCard> heroSaveData;
    //士兵
    public List<GameCard> soldierSaveData;
    //塔
    public List<GameCard> towerSaveData;
    //陷阱
    public List<GameCard> trapSaveData;
    //技能
    public List<GameCard> spellSaveData;
}
[Skip]
public class UnlockWarCount
{
    public int warId;           //战役id
    public int unLockCount;     //解锁关卡数
    public bool isTakeReward;   //是否领过首通宝箱
}
/// <summary>
/// 霸业管理类
/// </summary>
[Skip]
public class BaYeDataClass
{
    public long lastBaYeActivityTime;
    public DateTimeOffset lastStoryEventsRefreshHour;
    [JsonIgnore] public int CurrentExp => ExpData.Values.Sum();
    public int gold;
    /// <summary>
    /// 经验映像记录，key = 城池/事件id 注意：-1为额外添加的奖励事件, value = 奖励的经验值
    /// </summary>
    public Dictionary<int, int> ExpData = new Dictionary<int, int>();
    public List<BaYeCityEvent> data = new List<BaYeCityEvent>();
    public Dictionary<int, int> cityStories = new Dictionary<int, int>();
    /// <summary>
    /// 故事剧情映像表，key = eventPoint地点, value = storyEvent故事事件
    /// </summary>
    public Dictionary<int, BaYeStoryEvent> storyMap = new Dictionary<int, BaYeStoryEvent>();
    /// <summary>
    /// 战令，key = 势力Id, value = 战令数量
    /// </summary>
    public Dictionary<int, int> zhanLingMap = new Dictionary<int, int>();

    private bool[] openedChest1 = new bool[5];

    public bool[] openedChest
    {
        get
        {
            if (openedChest1 == null)
            {
                openedChest1 = new bool[5];
            }

            return openedChest1;
        }
        set => openedChest1 = value;
    }
}
[Skip]
public class BaYeCityEvent
{
    public List<int> ExpList { get; set; } = new List<int>();
    public List<int> WarIds { get; set; } = new List<int>();
    public bool[] PassedStages { get; set; } = new bool[0];
    public int CityId { get; set; }
    public int EventId { get; set; }
}
[Skip]
public class BaYeStoryEvent
{
    public int StoryId { get; set; }
    public int Type { get; set; }
    public int WarId { get; set; }
    public int GoldReward { get; set; }
    public int ExpReward { get; set; }
    public int YvQueReward { get; set; }
    public int YuanBaoReward { get; set; }
    public Dictionary<int,int> ZhanLing { get; set; }
}
[Skip]
public class WarsDataClass
{
    //战役解锁进度
    public List<UnlockWarCount> warUnlockSaveData;
    public UnlockWarCount GetCampaign(int warId)
    {
        var war = warUnlockSaveData.FirstOrDefault(w => w.warId == warId);
        if (war == null)
        {
            war = new UnlockWarCount {warId = warId};
            warUnlockSaveData.Add(war);
        }
        return war;
    }
}

#endregion

#region 游戏内容相关类

public class DeskReward
{
    private readonly List<CardReward> cards;
    public int YuanBao { get; }
    public int YuQue { get; }
    public int Exp { get; }
    public int Stamina { get; }
    public int AdPass { get; }

    public IReadOnlyList<CardReward> Cards => cards;

    public DeskReward()
    {
        
    }
    public DeskReward(int yuanBao, int yuQue, int exp, int stamina,int adPass ,List<CardReward> cards)
    {
        YuanBao = yuanBao;
        YuQue = yuQue;
        Exp = exp;
        AdPass = adPass;
        Stamina = stamina;
        this.cards = cards.Select(c => new {GameCardInfo.GetInfo((GameCardType) c.cardType, c.cardId).Rare, c})
            .OrderByDescending(c => c.c.cardType).ThenBy(c => c.Rare).Select(c => c.c).ToList();
    }
}

[Skip]
//宝箱卡片类
public class CardReward
{
    public int cardType;
    public int cardId;
    public int cardChips;
}

#endregion
