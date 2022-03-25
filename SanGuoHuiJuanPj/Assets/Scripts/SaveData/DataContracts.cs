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
public class GameCard : IGameCard,IComparable<GameCard>
{
    public static GameCard Instance(GameCardDto dto) => Instance(dto.CardId,(int)dto.Type,dto.Level,dto.Chips);
    public static GameCard Instance(int cardId,int type,int level,int chips = 0)
    {
        return new GameCard
        {
            id = cardId,
            chips = chips, 
            level = level, 
            typeIndex = type
        };
    }
    public int id;          //id
    public int level = 1;   //当前等级
    public int chips;       //拥有碎片
    public int isFight;     //是否出战
    public int typeIndex;   //单位类型0武将1士兵2塔3陷阱4技能

    public int CardId => id;
    public int Level
    {
        get => level;
        set => level = value;
    }
    public int Chips
    {
        get => chips;
        set => chips = value;
    }
    public int Type => typeIndex;

    public int CompareTo(GameCard other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var isFightComparison = isFight.CompareTo(other.isFight);
        if (isFightComparison != 0) return -isFightComparison;
        var isEnlistable = (level > 0).CompareTo(other.level > 0);
        if (isEnlistable != 0) return -isEnlistable;
        var levelComparison = level.CompareTo(other.level);
        if (levelComparison != 0) return -levelComparison;
        var rareComparison = GetRare(this).CompareTo(GetRare(other));
        if (rareComparison != 0) return -rareComparison;
        var typeIndexComparison = typeIndex.CompareTo(other.typeIndex);
        if (typeIndexComparison != 0) return typeIndexComparison;
        var chipsComparison = chips.CompareTo(other.chips);
        if (chipsComparison != 0) return -chipsComparison;
        return id.CompareTo(other.id);

        int GetRare(GameCard c)
        {
            switch ((GameCardType) c.typeIndex)
            {
                case GameCardType.Hero:
                    return DataTable.Hero[c.id].Rarity;
                case GameCardType.Tower:
                    return DataTable.Tower[c.id].Rarity;
                case GameCardType.Trap:
                    return DataTable.Trap[c.id].Rarity;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

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
