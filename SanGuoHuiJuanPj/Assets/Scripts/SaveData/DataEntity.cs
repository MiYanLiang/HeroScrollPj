using System.Collections.Generic;
using System.Linq;
using Assets;
using CorrelateLib;
using UnityEngine;

public class UserSaveArchive : IUserSaveArchive
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Phone { get; set; }
    public string DeviceId { get; set; }
    public long LastUpdate { get; set; }
    public string PlayerInfo { get; set; }
    public string CardsData { get; set; }
    public string Expedition { get; set; }
    public string RewardsRecord { get; set; }

    public UserSaveArchive(IUserInfo userInfo, IPlayerData playerData, HSTDataClass h, WarsDataClass w,
        GetBoxOrCodeData b)
    {
        var hst = new HSTDataClass
        {
            heroSaveData = h.heroSaveData.Where(c => c.IsOwning()).ToList(),
            soldierSaveData = h.soldierSaveData.Where(c => c.IsOwning()).ToList(),
            spellSaveData = h.spellSaveData.Where(c => c.IsOwning()).ToList(),
            towerSaveData = h.towerSaveData.Where(c => c.IsOwning()).ToList(),
            trapSaveData = h.trapSaveData.Where(c => c.IsOwning()).ToList()
        };
        var war = new WarsDataClass
        {
            warUnlockSaveData = w.warUnlockSaveData.Where(o => o.unLockCount > 0).ToList()
        };
        var reward = new GetBoxOrCodeData
        {
            fightBoxs = b.fightBoxs,
            redemptionCodeGotList = b.redemptionCodeGotList
        };
        Username = userInfo.Username;
        Password = userInfo.Password;
        Phone = userInfo.Phone;
        DeviceId = userInfo.DeviceId;
        LastUpdate = userInfo.LastUpdate;
        PlayerInfo = Json.Serialize(playerData);
        CardsData = Json.Serialize(hst);
        Expedition = Json.Serialize(war);
        RewardsRecord = Json.Serialize(reward);
    }
}

/// <summary>
/// 玩家账户信息存档
/// </summary>
public class UserInfo : IUserInfo
{
    /// <summary>
    /// 账号
    /// </summary>
    public string Username { get; set; }
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// 手机号
    /// </summary>
    public string Phone { get; set; }
    /// <summary>
    /// 硬件唯一标识id
    /// </summary>
    public string DeviceId { get; set; }
    /// <summary>
    /// 与服务器最后一次互交的时间
    /// </summary>
    public long LastUpdate { get; set; }
    /// <summary>
    /// 游戏版本号
    /// </summary>
    public float GameVersion { get; set; }
}
/// <summary>
/// 旧玩家基本信息存档数据类2
/// </summary>
public class ObsoletedPyData
{
    //等级
    public int level { get; set; }
    //经验
    public int exp { get; set; }
    //元宝
    public int yuanbao { get; set; }
    //玉阙
    public int yvque { get; set; }
    //体力
    public int stamina { get; set; }
    //玩家初始势力id
    public int forceId { get; set; }
    //战役宝箱
    public List<int> fightBoxs;
    //兑换码
    public List<RCode> redemptionCodeGotList;
}

/// <summary>
/// 旧玩家基本信息存档数据类
/// </summary>
public class ObsoletedPlayerData
{
    //姓名
    public string name { get; set; }
    //等级
    public int level { get; set; }
    //经验
    public int exp { get; set; }
    //元宝
    public int yuanbao { get; set; }
    //玉阙
    public int yvque { get; set; }
    //体力
    public int stamina { get; set; }
    //玩家初始势力id
    public int forceId { get; set; }
    //战役宝箱
    public List<int> fightBoxs;
    //兑换码
    public List<RCode> redemptionCodeGotList;
}

/// <summary>
/// 玩家基本信息存档数据类
/// </summary>
public class PlayerData : IPlayerData
{
    public static PlayerData Instance(PlayerDataDto dto)
    {
        return new PlayerData
        {
            Level = dto.Level,
            Exp = dto.Exp,
            YuanBao = dto.YuanBao,
            YvQue = dto.YvQue,
            Stamina = dto.Stamina,
            ForceId = dto.ForceId,
            AdPass = dto.AdPass,
            LastJinNangRedeemTime = dto.LastJinNangRedeemTime,
            DailyJinNangRedemptionCount = dto.DailyJinNangRedemptionCount,
            LastJiuTanRedeemTime = dto.LastJiuTanRedeemTime,
            DailyJiuTanRedemptionCount = dto.DailyJiuTanRedemptionCount,
            LastFourDaysChestRedeemTime = dto.LastFourDaysChestRedeemTime,
            LastWeekChestRedeemTime = dto.LastWeekChestRedeemTime,
            LastStaminaUpdateTicks = dto.LastStaminaUpdateTicks,
            LastGameVersion = float.Parse(Application.version)
        };
    }
    //等级
    public int Level { get; set; } = 1;
    //经验
    public int Exp { get; set; }
    //元宝
    public int YuanBao { get; set; }
    //玉阙
    public int YvQue { get; set; }
    //体力
    public int Stamina { get; set; }
    //玩家初始势力id
    public int ForceId { get; set; }
    //上次锦囊获取时间
    public long LastJinNangRedeemTime { get; set; }
    //锦囊每天的获取次数
    public int DailyJinNangRedemptionCount { get; set; }
    //上次酒坛获取时间
    public long LastJiuTanRedeemTime { get; set; }
    //酒坛每天的获取次数
    public int DailyJiuTanRedemptionCount { get; set; }
    //上次领取198宝箱时间
    public long LastFourDaysChestRedeemTime { get; set; }
    //上传领取298宝箱时间
    public long LastWeekChestRedeemTime { get; set; }
    //上个体力更新时间
    public long LastStaminaUpdateTicks { get; set; }
    //游戏版本号
    public float LastGameVersion { get; set; }
    //免广告卷
    public int AdPass { get; set; }
    //战斗的时间倍率
    public float WarTimeScale { get; set; } = 1;
}

public class Character : ICharacter
{
    public static Character Instance(ICharacter cha)
    {
        return cha == null || !cha.IsValidCharacter()
            ? null
            : new Character
            {
                Avatar = cha.Avatar, Gender = cha.Gender, Name = cha.Name, Nickname = cha.Nickname, Rank = cha.Rank,
                Settle = cha.Settle, Sign = cha.Sign
            };
    }
    public string Name { get; set; }
    public string Nickname { get; set; }
    public int Gender { get; set; }
    public int Avatar { get; set; }
    public string Sign { get; set; }
    public int Settle { get; set; }
    public int Rank { get; set; }

    public CharacterDto ToDto()
    {
        var dto = new CharacterDto
        {
            Name = Name,
            Nickname = Nickname,
            Gender = Gender,
            Avatar = Avatar,
            Sign = Sign,
            Settle = Settle,
            Rank = Rank
        };
        return dto;
    }
}

//战斗卡牌信息类

/// <summary>
/// 羁绊判断激活类
/// </summary>
public class JiBanActivedClass
{
    public int JiBanId { get; set; }
    public bool IsActive { get; set; }
    public bool IsHadBossId { get; set; }
    public List<JiBanCardTypeClass> List { get; set; }
}

/// <summary>
/// 单个羁绊中卡牌小类
/// </summary>
public class JiBanCardTypeClass
{
    public int CardType { get; set; }
    public int CardId { get; set; }
    public int BossId { get; set; }
    public List<FightCardData> Cards { get; set; }
}
