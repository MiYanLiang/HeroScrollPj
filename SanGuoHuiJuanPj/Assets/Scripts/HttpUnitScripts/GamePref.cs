using CorrelateLib;
using UnityEngine;

public static class GamePref
{
    public static bool IsPlayedIntro => PlayerPrefs.GetInt(IsPlayedIntroString) > 0;
    public static void SetIsPlayedIntro(bool isPlayed) => PlayerPrefs.SetInt(IsPlayedIntroString, isPlayed ? 1 : 0);
    public static string Username => PlayerPrefs.GetString(AccountIdString);
    public static string Password => PlayerPrefs.GetString(PasswordString);
    public static string IsFirstPlay => PlayerPrefs.GetString(IsFirstPlayString);
    public static float PrefWarSpeed
    {
        get
        {
            var speed = PlayerPrefs.GetFloat(PlayerWarSpeedPrefs);
            if (speed != 0) return speed;
            SetPrefWarSpeed(1);
            return 1;
        }
    }
    public static bool PrefMusicPlay => PlayerPrefs.GetInt(PlayerMusicOffPrefs) == 0;
    /// <summary>
    /// 0 = 密码登录, 1 = 设备登录
    /// </summary>
    public static int ClientLoginMethod => PlayerPrefs.GetInt(LoginMethod);

    public static bool IsUserAccountCompleted => PlayerPrefs.GetString(AccountCompleteUsername) == Username;
    public const string BaYeSaveString = "BaYeSave";
    private static string BaYeSave
    {
        get
        {
            var zone = SignalRClient.instance.Zone;
            return zone == 0 ? BaYeSaveString : $"{BaYeSaveString}{zone}";
        }
    }

    public static BaYeDataClass GetBaYe
    {
        get
        {
            var json = PlayerPrefs.GetString(BaYeSave);
            return Json.Deserialize<BaYeDataClass>(json);
        }
    }

    public const string IsPlayedIntroString = "IsPlayedIntro";

    private const string LastServiceZoneString = "LastServiceZone";
    public static int LastServiceZone => PlayerPrefs.GetInt(LastServiceZoneString);
    public static void SetLastServiceZone(int zone) => PlayerPrefs.SetInt(LastServiceZoneString, zone);


    private const string PlayerMusicOffPrefs = "PlayerMusicOffPrefs";
    private const string PlayerWarSpeedPrefs = "PlayerWarSpeedPrefs";
    private const string AccountIdString = "accountName";
    private const string PasswordString = "Password";
    private const string RoasterChickenString = "RoasterChicken";

    public const string PhoneNumber = "Phone";
    public const string IsFirstPlayString = "IsFirstPlay";
    public const string AccountCompleteUsername = "IsUsersAccountComplete";
    public const string LoginMethod = "LoginMethod";

    public static void SetUsername(string username) => PlayerPrefs.SetString(AccountIdString,username);

    public static void SetPassword(string password) => PlayerPrefs.SetString(PasswordString, password);

    public static void SetIsFirstPlay(bool firstPlay)
    {
        PlayerPrefs.SetInt(IsFirstPlayString, firstPlay ? 1 : 0);
        if (firstPlay)
        {
            PlayerPrefs.SetInt(StringForGuide.guideJinBaoXiang, 0);
            PlayerPrefs.SetInt(StringForGuide.guideZYBaoXiang, 0);
            PlayerPrefs.SetInt(StringForGuide.guideHeCheng, 0);
            PlayerPrefs.SetInt(StringForGuide.guideStartZY, 0);
            PlayerPrefs.SetInt(StringForGuide.guideStartGQ, 0);
            PlayerPrefs.SetInt(StringForGuide.guideCheckCardInfo, 0);
            PlayerPrefs.SetInt(StringForGuide.guideShengJIZG, 0);
        }
        else
        {
            PlayerDataForGame.instance.GuideObjsShowed[0] = PlayerPrefs.GetInt(StringForGuide.guideJinBaoXiang);
            PlayerDataForGame.instance.GuideObjsShowed[1] = PlayerPrefs.GetInt(StringForGuide.guideZYBaoXiang);
            PlayerDataForGame.instance.GuideObjsShowed[2] = PlayerPrefs.GetInt(StringForGuide.guideHeCheng);
            PlayerDataForGame.instance.GuideObjsShowed[3] = PlayerPrefs.GetInt(StringForGuide.guideStartZY);
            PlayerDataForGame.instance.GuideObjsShowed[4] = PlayerPrefs.GetInt(StringForGuide.guideStartGQ);
            PlayerDataForGame.instance.GuideObjsShowed[5] = PlayerPrefs.GetInt(StringForGuide.guideCheckCardInfo);
            PlayerDataForGame.instance.GuideObjsShowed[6] = PlayerPrefs.GetInt(StringForGuide.guideShengJIZG);
        }
    }

    public static void SetPrefWarSpeed(float speed) => PlayerPrefs.SetFloat(PlayerWarSpeedPrefs, speed);
    public static void SetPrefMusic(bool isPlay) => PlayerPrefs.SetInt(PlayerMusicOffPrefs, isPlay ? 0 : 1);
    public static void FlagDeviceReg(string username) => PlayerPrefs.SetString(AccountCompleteUsername, username);
    public static void SaveBaYe(BaYeDataClass save)
    {
        PlayerPrefs.SetString(BaYeSave, Json.Serialize(save));
    }

    public static void FlagClientLoginMethod(bool isDeviceLogin) => PlayerPrefs.SetInt(LoginMethod, isDeviceLogin ? 1 : 0);

    public static string RoasterChickenRecord() => PlayerPrefs.GetString(RoasterChickenString);

    public static void SetRoasterChicken(string json) => PlayerPrefs.SetString(RoasterChickenString, json);
}