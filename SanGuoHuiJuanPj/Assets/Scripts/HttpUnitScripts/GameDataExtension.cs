using CorrelateLib;

public static class GameDataExtension
{
    public static PlayerDataDto ToDto(this IPlayerData playerData)
    {
        return new PlayerDataDto
        {
            Level = playerData.Level,
            Exp = playerData.Exp,
            YuanBao = playerData.YuanBao,
            YvQue = playerData.YvQue,
            Stamina = playerData.Stamina,
            ForceId = playerData.ForceId,
            LastJinNangRedeemTime = playerData.LastJinNangRedeemTime,
            DailyJinNangRedemptionCount = playerData.DailyJinNangRedemptionCount,
            LastJiuTanRedeemTime = playerData.LastJiuTanRedeemTime,
            DailyJiuTanRedemptionCount = playerData.DailyJiuTanRedemptionCount,
            LastFourDaysChestRedeemTime = playerData.LastFourDaysChestRedeemTime,
            LastWeekChestRedeemTime = playerData.LastWeekChestRedeemTime,
            LastGameVersion = playerData.LastGameVersion
        };
    }

    public static GameCardDto ToDto(this GameCard chip) =>
        new(chip.CardId, type: chip.Type, level: chip.Level, chips: chip.Chips, arouse: chip.Arouse,
            chip.Deputy1Id, chip.Deputy1Level,
            chip.Deputy2Id, chip.Deputy2Level,
            chip.Deputy3Id, chip.Deputy3Level,
            chip.Deputy4Id, chip.Deputy4Level);
}