using CorrelateLib;
using Newtonsoft.Json;

public static class ViewBagExtension
{
    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static T GetDto<T>(this ViewBagBase vb, string key) where T : class =>
        vb.GetObj(key, s => JsonConvert.DeserializeObject<T>(s, settings));

    public static IViewBag SetDto(this IViewBag vb,string key,object obj)
    {
        ((ViewBagBase)vb).SetObj(key, () => JsonConvert.SerializeObject(obj, settings));
        return vb;
    }
    public static IViewBag PlayerDataDto(this IViewBag bag, PlayerDataDto dto) => bag.SetDto(ViewBag.PlayerDataDto, dto);
    public static PlayerDataDto GetPlayerDataDto(this ViewBag bag) => bag.GetDto<PlayerDataDto>(ViewBag.PlayerDataDto);
    public static IViewBag PlayerWarChests(this IViewBag bag, int[] dto) => bag.SetDto(ViewBag.Player_WarChestDtos, dto);
    public static int[] GetPlayerWarChests(this ViewBag bag) => bag.GetDto<int[]>(ViewBag.Player_WarChestDtos);
    public static IViewBag PlayerRedeemedCodes(this IViewBag bag, string[] list) => bag.SetDto(ViewBag.Player_RedeemedCodes, list);
    public static string[] GetPlayerRedeemedCodes(this ViewBag bag) => bag.GetDto<string[]>(ViewBag.Player_RedeemedCodes);
    public static IViewBag PlayerWarCampaignDtos(this IViewBag bag, WarCampaignDto[] dto) => bag.SetDto(ViewBag.Player_CampaignDtos, dto);
    public static WarCampaignDto[] GetPlayerWarCampaignDtos(this ViewBag bag) => bag.GetDto<WarCampaignDto[]>(ViewBag.Player_CampaignDtos);
    public static IViewBag PlayerGameCardDtos(this IViewBag bag, GameCardDto[] dto) => bag.SetDto(ViewBag.Player_GameCardDtos, dto);
    public static GameCardDto[] GetPlayerGameCardDtos(this ViewBag bag) => bag.GetDto<GameCardDto[]>(ViewBag.Player_GameCardDtos);
    public static IViewBag GameCardDto(this IViewBag bag, GameCardDto dto) => bag.SetDto(ViewBag.GameCardDto, dto);
    public static GameCardDto GetGameCardDto(this ViewBag bag) => bag.GetDto<GameCardDto>(ViewBag.GameCardDto);
    public static IViewBag ResourceDto(this IViewBag bag, ResourceDto dto) => bag.SetDto(ViewBag.ResourceDto, dto);
    public static ResourceDto GetResourceDto(this ViewBag bag) => bag.GetDto<ResourceDto>(ViewBag.ResourceDto);
    public static IViewBag RCode(this IViewBag bag, RCodeTable obj) => bag.SetDto(ViewBag.RCode, obj);
    public static RCodeTable GetRCode(this ViewBag bag) => bag.GetDto<RCodeTable>(ViewBag.RCode);
    public static IViewBag PlayerTroopDtos(this IViewBag bag, TroopDto[] dto) => bag.SetDto(ViewBag.Player_Troops, dto);
    public static TroopDto[] GetPlayerTroopDtos(this ViewBag bag) => bag.GetDto<TroopDto[]>(ViewBag.Player_Troops);
    public static IViewBag TroopDto(this IViewBag bag, TroopDto dto) => bag.SetDto(ViewBag.TroopDto, dto);
    public static TroopDto GetTroopDto(this ViewBag bag) => bag.GetDto<TroopDto>(ViewBag.TroopDto);
    public static IViewBag Chicken(this IViewBag bag, ChickenTable obj) => bag.SetDto(ViewBag.Chicken, obj);
    public static ChickenTable GetChicken(this ViewBag bag) => bag.GetDto<ChickenTable>(ViewBag.Chicken);
    public static IViewBag WarChestDto(this IViewBag bag, WarChestDto obj) => bag.SetDto(ViewBag.WarChestDto, obj);
    public static WarChestDto GetWarChest(this ViewBag bag) => bag.GetDto<WarChestDto>(ViewBag.WarChestDto);
    public static IViewBag JinNangDto(this IViewBag bag, JinNangDto obj) => bag.SetDto(ViewBag.JinNangDto, obj);
    public static JinNangDto GetJinNang(this ViewBag bag) => bag.GetDto<JinNangDto>(ViewBag.JinNangDto);
    public static IViewBag WarCampaignDto(this IViewBag bag, WarCampaignDto dto) => bag.SetDto(ViewBag.CampaignDto, dto);
    public static WarCampaignDto GetWarCampaignDto(this ViewBag bag) => bag.GetDto<WarCampaignDto>(ViewBag.CampaignDto);
    public static IViewBag PlayerCharacterDto(this IViewBag bag, CharacterDto dto) => bag.SetDto(ViewBag.Player_CharacterDto, dto);
    public static CharacterDto GetPlayerCharacterDto(this ViewBag bag) => bag.GetDto<CharacterDto>(ViewBag.Player_CharacterDto);
    public static IViewBag CharacterDtos(this IViewBag bag, CharacterDto[] dto) => bag.SetDto(ViewBag.CharacterDtos, dto);
    public static CharacterDto[] GetCharacterDtos(this ViewBag bag) => bag.GetDto<CharacterDto[]>(ViewBag.CharacterDtos);
}