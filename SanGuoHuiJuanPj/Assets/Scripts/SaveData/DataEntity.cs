using System;
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
[Serializable]
//战斗状态类
public class FightState
{
    private static int[] _consInts=Enum.GetValues(typeof(Cons)).Cast<int>().ToArray();
    public enum Cons
    {
        /// <summary>
        /// 眩晕
        /// </summary>
        Stunned = 1,
        /// <summary>
        /// 护盾
        /// </summary>
        Shield = 2,
        /// <summary>
        /// 无敌
        /// </summary>
        Invincible = 3,
        /// <summary>
        /// 流血
        /// </summary>
        Bleed = 4,
        /// <summary>
        /// 毒
        /// </summary>
        Poison = 5,
        /// <summary>
        /// 灼烧
        /// </summary>
        Burn = 6,
        /// <summary>
        /// 战意
        /// </summary>
        Stimulate = 7,
        /// <summary>
        /// 禁锢
        /// </summary>
        Imprisoned = 8,
        /// <summary>
        /// 胆怯
        /// </summary>
        Cowardly = 9,
        /// <summary>
        /// 战鼓台
        /// </summary>
        ZhanGuTaiAddOn = 10,
        /// <summary>
        /// 风神台
        /// </summary>
        FengShenTaiAddOn = 11,
        /// <summary>
        /// 霹雳台
        /// </summary>
        PiLiTaiAddOn = 12,
        /// <summary>
        /// 琅琊台
        /// </summary>
        LangYaTaiAddOn = 13,
        /// <summary>
        /// 烽火台
        /// </summary>
        FengHuoTaiAddOn = 14,
        /// <summary>
        /// 死战
        /// </summary>
        DeathFight = 15,
        /// <summary>
        /// 卸甲
        /// </summary>
        Unarmed = 16,
        /// <summary>
        /// 内助
        /// </summary>
        Neizhu = 17,
        /// <summary>
        /// 神助
        /// </summary>
        ShenZhu = 18,
        /// <summary>
        /// 防护盾
        /// </summary>
        ExtendedHp = 19,
        /// <summary>
        /// 迷雾
        /// </summary>
        MiWuZhenAddOn = 20
    }

    public FightState() => data = _consInts.ToDictionary(s => s, _ => 0);
    private Dictionary<int, int> data;
    public IReadOnlyDictionary<int, int> Data => data;
    /// <summary>
    /// 眩晕回合数
    /// </summary>
    public int Stunned { get=>data[1]; set=>data[1] = value; }

    /// <summary>
    /// 护盾层数
    /// </summary>
    public int Shield { get => data[2]; set => data[2] = value; }

    /// <summary>
    /// 无敌回合
    /// </summary>
    public int Invincible { get => data[3]; set => data[3] = value; }

    /// <summary>
    /// 流血层数
    /// </summary>
    public int Bleed { get => data[4]; set => data[4] = value; }

    /// <summary>
    /// 中毒回合
    /// </summary>
    public int Poison { get => data[5]; set => data[5] = value; }

    /// <summary>
    /// 灼烧回合
    /// </summary>
    public int Burn { get => data[6]; set => data[6] = value; }

    /// <summary>
    /// 战意层数
    /// </summary>
    public int Stimulate { get => data[7]; set => data[7] = value; }

    /// <summary>
    /// 禁锢层数
    /// </summary>
    public int Imprisoned { get => data[8]; set => data[8] = value; }

    /// <summary>
    /// 怯战层数
    /// </summary>
    public int Cowardly { get => data[9]; set => data[9] = value; }

    /// <summary>
    /// 战鼓台-伤害加成
    /// </summary>
    public int ZhanGuTaiAddOn { get => data[10]; set => data[10] = value; }

    /// <summary>
    /// 风神台-闪避加成
    /// </summary>
    public int FengShenTaiAddOn { get => data[11]; set => data[11] = value; }

    /// <summary>
    /// 霹雳台-暴击加成
    /// </summary>
    public int PiLiTaiAddOn { get => data[12]; set => data[12] = value; }

    /// <summary>
    /// 狼牙台-会心加成
    /// </summary>
    public int LangYaTaiAddOn { get => data[13]; set => data[13] = value; }

    /// <summary>
    /// 烽火台-免伤加成
    /// </summary>
    public int FengHuoTaiAddOn { get => data[14]; set => data[14] = value; }

    /// <summary>
    /// 死战回合
    /// </summary>
    public int DeathFight { get => data[15]; set => data[15] = value; }

    /// <summary>
    /// 卸甲回合
    /// </summary>
    public int Unarmed { get => data[16]; set => data[16] = value; }

    /// <summary>
    /// 内助回合
    /// </summary>
    public int Neizhu { get => data[17]; set => data[17] = value; }

    /// <summary>
    /// 神助回合
    /// </summary>
    public int ShenZhu { get => data[18]; set => data[18] = value; }

    /// <summary>
    /// 防护盾数值
    /// </summary>
    public int ExtendedHp { get => data[19]; set => data[19] = value; }

    /// <summary>
    /// 迷雾阵-远程闪避加成
    /// </summary>
    public int MiWuZhenAddOn { get => data[20]; set => data[20] = value; }

    public void AddState(Cons con,int value)
    {
        switch (con)
        {
            case Cons.Stunned:
                Stunned += MinZeroAlign(Stunned);
                break;
            case Cons.Shield:
                Shield += MinZeroAlign(Shield);
                break;
            case Cons.Invincible:
                Invincible += MinZeroAlign(Invincible);
                break;
            case Cons.Bleed:
                Bleed += MinZeroAlign(Bleed);
                break;
            case Cons.Poison:
                Poison += MinZeroAlign(Poison);
                break;
            case Cons.Burn:
                Burn += MinZeroAlign(Burn);
                break;
            case Cons.Stimulate:
                Stimulate += MinZeroAlign(Burn);
                break;
            case Cons.Imprisoned:
                Imprisoned += MinZeroAlign(Imprisoned);
                break;
            case Cons.Cowardly:
                Cowardly += MinZeroAlign(Cowardly);
                break;
            case Cons.ZhanGuTaiAddOn:
                ZhanGuTaiAddOn += MinZeroAlign(ZhanGuTaiAddOn);
                break;
            case Cons.FengShenTaiAddOn:
                FengShenTaiAddOn += MinZeroAlign(FengShenTaiAddOn);
                break;
            case Cons.PiLiTaiAddOn:
                PiLiTaiAddOn += MinZeroAlign(PiLiTaiAddOn);
                break;
            case Cons.LangYaTaiAddOn:
                LangYaTaiAddOn += MinZeroAlign(LangYaTaiAddOn);
                break;
            case Cons.FengHuoTaiAddOn:
                FengHuoTaiAddOn += MinZeroAlign(FengShenTaiAddOn);
                break;
            case Cons.DeathFight:
                DeathFight += MinZeroAlign(DeathFight);
                break;
            case Cons.Unarmed:
                Unarmed += MinZeroAlign(Unarmed);
                break;
            case Cons.Neizhu:
                Neizhu += MinZeroAlign(Neizhu);
                break;
            case Cons.ShenZhu:
                ShenZhu += MinZeroAlign(ShenZhu);
                break;
            case Cons.ExtendedHp:
                ExtendedHp += MinZeroAlign(ExtendedHp);
                break;
            case Cons.MiWuZhenAddOn:
                MiWuZhenAddOn += MinZeroAlign(MiWuZhenAddOn);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(con), con, null);
        }
        int MinZeroAlign(int ori) => value < 0 && ori < value ? -ori : value; //最多是清0
    }

    public void ClearState(Cons con)
    {
        switch (con)
        {
            case Cons.Stunned:
                Stunned = 0;
                break;
            case Cons.Shield:
                Shield = 0;
                break;
            case Cons.Invincible:
                Invincible = 0;
                break;
            case Cons.Bleed:
                Bleed = 0;
                break;
            case Cons.Poison:
                Poison = 0;
                break;
            case Cons.Burn:
                Burn = 0;
                break;
            case Cons.Stimulate:
                Stimulate = 0;
                break;
            case Cons.Imprisoned:
                Imprisoned = 0;
                break;
            case Cons.Cowardly:
                Cowardly = 0;
                break;
            case Cons.ZhanGuTaiAddOn:
                ZhanGuTaiAddOn = 0;
                break;
            case Cons.FengShenTaiAddOn:
                FengShenTaiAddOn = 0;
                break;
            case Cons.PiLiTaiAddOn:
                PiLiTaiAddOn = 0;
                break;
            case Cons.LangYaTaiAddOn:
                LangYaTaiAddOn = 0;
                break;
            case Cons.FengHuoTaiAddOn:
                FengHuoTaiAddOn = 0;
                break;
            case Cons.DeathFight:
                DeathFight = 0;
                break;
            case Cons.Unarmed:
                Unarmed = 0;
                break;
            case Cons.Neizhu:
                Neizhu = 0;
                break;
            case Cons.ShenZhu:
                ShenZhu = 0;
                break;
            case Cons.ExtendedHp:
                ExtendedHp = 0;
                break;
            case Cons.MiWuZhenAddOn:
                MiWuZhenAddOn = 0;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(con), con, null);
        }
    }
}

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
