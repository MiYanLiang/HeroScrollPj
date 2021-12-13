using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TimeSystemControl : MonoBehaviour
{
    public static TimeSystemControl instance;

    private string timeWebPath = "http://www.hko.gov.hk/cgi-bin/gts/time5a.pr?a=1";
    private string timeWebPath0 = "http://api.m.taobao.com/rest/api3.do?api=mtop.common.getTimestamp";

    #region UnityEditor

    public SystemTimer SystemTimer; //系统时间的脚本引用(编辑器里引用)
    public int JinNangTimeGapSecs = 3600; //锦囊间隔时间
    public int JiuTanTimeGapSecs = 3600; //酒坛间隔时间
    public int JiuTanRedeemCountPerDay = 10;
    public int JinNangRedeemCountPerDay = 10;

    #endregion

    public event Action OnHourly;
    private DateTimeOffset hour; //现在时间(小时制，只会跨小时不会跨分钟)
    public int NowHour => hour.Hour;

    int maxStamina; //记录最大体力值
    public int MaxStamina => maxStamina;

    bool isCanGetBox1;

    public bool IsCountdown { get; private set; }

    //烧鸡相关存档字符
    public static string openCKTime0_str = "openCKTime0"; //12点
    public static string openCKTime1_str = "openCKTime1"; //17点
    public static string openCKTime2_str = "openCKTime2"; //21点

    [HideInInspector] public bool isFInGame; //记录当天首次进入游戏
    private static string dayOfyearStr = "dayOfyearStr"; //存放上次进入游戏是哪一天
    private static string yearStr = "yearStr"; //存放上次进入游戏是哪一年

    public TimeSpan Free198TimeSpan { get; private set; }
    public TimeSpan Free298TimeSpan { get; private set; }

    private void Awake()
    {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;
    }

    private bool isInit;

    public void Init()
    {
        if (isInit) return;
        isInit = true;
        SystemTimer.instance.Init();
        StartGameToInitOpenTime();
    }

    //每次进入游戏对开启时间进行初始化
    private void StartGameToInitOpenTime()
    {
        JiuTanTimeGapSecs = DataTable.GetGameValue(0); //宝箱开启时间
        Free198TimeSpan = TimeSpan.FromSeconds(DataTable.GetGameValue(1));
        Free298TimeSpan = TimeSpan.FromSeconds(DataTable.GetGameValue(2));

        maxStamina = DataTable.ResourceConfig[2].NewPlayerValue; //startValue
    }

    private void Update()
    {
        if (!isInit) return;
        if (SystemTimer.Now == default) return;
        if (PlayerDataForGame.instance.pyData == null) return;
        UpdateTimeTrigger();
        UpdateJinNangTimer();
        UpdateJiuTanTimer();
        CountdownFreeFourDaysChest();
        CountdownFreeWeeklyChest();
        //UpdateChickenShoping();
        UpdateStamina();
    }

    public void InitStaminaCount(bool isCountdown) => IsCountdown = isCountdown;

    public void TestHourlyInvoke() => OnHourly?.Invoke();

    /// <summary>
    /// 更新时间触发器
    /// </summary>
    private void UpdateTimeTrigger()
    {
        if (SystemTimer.Now - hour > TimeSpan.FromHours(1))
        {
            hour = SystemTimer.Now.Date.AddHours(SystemTimer.Now.Hour);
            OnHourly?.Invoke();
        }
    }

    /// <summary>
    /// 确认是否是当天第一次加载游戏
    /// </summary>
    public void InitIsTodayFirstLoadingGame()
    {
        DateTime now = SystemTimer.Now.LocalDateTime;
        //DateTime nowTime = DateTime.Now;

        if (now.Year > PlayerPrefs.GetInt(yearStr))
        {
            PlayerPrefs.SetInt(yearStr, now.Year);
            PlayerPrefs.SetInt(dayOfyearStr, now.DayOfYear);
            isFInGame = true;
        }
        else
        {
            if (now.DayOfYear > PlayerPrefs.GetInt(dayOfyearStr))
            {
                PlayerPrefs.SetInt(dayOfyearStr, now.DayOfYear);
                isFInGame = true;
            }
            else
            {
                isFInGame = false;
            }
        }
    }

    //修正下次进入已经不是今天第一次进入游戏了
    public void UpdateIsNotFirstInGame()
    {
        DateTime nowTime = SystemTimer.Now.LocalDateTime;
        //DateTime nowTime = DateTime.Now;

        PlayerPrefs.SetInt(yearStr, nowTime.Year);
        PlayerPrefs.SetInt(dayOfyearStr, nowTime.DayOfYear);
        isFInGame = false;
    }

    public string TimeDisplayInChineseText(long from, long to) => TimeDisplayInChineseText(SysTime.TimeSpanFromUnixTicks(to - @from));

    public string TimeDisplayInChineseText(TimeSpan timeSpan = default)
    {
        if (timeSpan.TotalSeconds < 1) return string.Empty;
        if (timeSpan.TotalDays >= 1)
            return $"{(int) timeSpan.TotalDays}天" +
                   (timeSpan.Hours > 0 ? $"{timeSpan.Hours}时" : string.Empty);
        return timeSpan.Hours > 0 ? $"{timeSpan.Hours}时{timeSpan.Minutes}分" : $"{timeSpan.Minutes}分{timeSpan.Seconds}秒";
    }

    private void UpdateStamina()
    {
        if (PlayerDataForGame.instance.Stamina == null) return;
        var stamina = PlayerDataForGame.instance.Stamina;
        stamina.UpdateStamina();
        if (GameSystem.CurrentScene == GameSystem.GameScene.MainScene)
            UIManager.instance.UpdateShowTiLiInfo(stamina.IsStopIncrease
                ? TimeDisplayInChineseText()
                : TimeDisplayInChineseText(stamina.Countdown));
    }

    private void UpdateJiuTanTimer()
    {
        var playerData = PlayerDataForGame.instance.pyData;
        if (playerData == null) return;
        var nextOpenJiuTanTimeTicks = playerData.LastJiuTanRedeemTime + (JiuTanTimeGapSecs * 1000);
        var redeemCount = playerData.DailyJiuTanRedemptionCount;
        var limitNotReach = redeemCount < JiuTanRedeemCountPerDay;

        if (GameSystem.CurrentScene != GameSystem.GameScene.MainScene) return;
        var lastRedeemIsToday = SysTime.IsToday(PlayerDataForGame.instance.pyData.LastJiuTanRedeemTime);
        var jiuTanCount = lastRedeemIsToday ? JiuTanRedeemCountPerDay - redeemCount : JinNangRedeemCountPerDay;
        var displayText = string.Empty;
        if (limitNotReach)
            displayText =
                TimeDisplayInChineseText(TimeSpan.FromMilliseconds(nextOpenJiuTanTimeTicks - SystemTimer.NowUnixTicks));
        UIManager.instance.taoYuan.UpdateJiuTan(IsJiuTanAvailable(lastRedeemIsToday), jiuTanCount, displayText);
    }

    private void CountdownFreeFourDaysChest()
    {
        if (GameSystem.CurrentScene != GameSystem.GameScene.MainScene) return;

        var py = PlayerDataForGame.instance.pyData;

        var nextOpenTimeTick = (long) (py.LastFourDaysChestRedeemTime + Free198TimeSpan.TotalMilliseconds);
        if (nextOpenTimeTick < SysTime.UnixNow)
        {
            UIManager.instance.taoYuan.copperChest.UpdateChest(string.Empty, true);
            return;
        }

        UIManager.instance.taoYuan.copperChest.UpdateChest(TimeDisplayInChineseText(SysTime.UnixNow, nextOpenTimeTick),
            false);
    }

    private void CountdownFreeWeeklyChest()
    {
        if (GameSystem.CurrentScene != GameSystem.GameScene.MainScene) return;

        var py = PlayerDataForGame.instance.pyData;

        var nextOpenTimeTick = (long) (py.LastWeekChestRedeemTime + Free298TimeSpan.TotalMilliseconds);
        if (nextOpenTimeTick <= SysTime.UnixNow)
        {
            UIManager.instance.taoYuan.goldChest.UpdateChest(string.Empty, true);
            return;
        }

        UIManager.instance.taoYuan.goldChest.UpdateChest(TimeDisplayInChineseText(SysTime.UnixNow, nextOpenTimeTick),
            false);
    }

    private void UpdateJinNangTimer()
    {
        var playerData = PlayerDataForGame.instance.pyData;
        if (playerData == null) return;
        if (playerData.DailyJinNangRedemptionCount >= 10) return;
        if (GameSystem.CurrentScene != GameSystem.GameScene.MainScene) return;
        if (UIManager.instance) UIManager.instance.UpdateShowJinNangBtn(IsJinNangAvailable());
    }

    public bool IsFreeWeeklyChestAvailable() =>
        PlayerDataForGame.instance.pyData.LastWeekChestRedeemTime + Free298TimeSpan.TotalMilliseconds <
        SysTime.UnixNow;

    public bool IsFreeFourDaysChestAvailable() => PlayerDataForGame.instance.pyData.LastFourDaysChestRedeemTime +
                                                  Free198TimeSpan.TotalMilliseconds <
                                                  SysTime.UnixNow;

    public bool IsJinNangAvailable()
    {
        var lastRedeemTimeGapIsInRange = SysTime.UnixNow <
                                         PlayerDataForGame.instance.pyData.LastJinNangRedeemTime +
                                         JinNangTimeGapSecs * 1000;
        if (lastRedeemTimeGapIsInRange) return false;
        var lastRedeemIsToday = SysTime.IsToday(PlayerDataForGame.instance.pyData.LastJinNangRedeemTime);
        var jinNangRedeemLimitNotReach =
            PlayerDataForGame.instance.pyData.DailyJinNangRedemptionCount < JinNangRedeemCountPerDay;
        return !lastRedeemIsToday || jinNangRedeemLimitNotReach;
    }

    public bool IsJiuTanAvailable(bool lastRedeemIsToday)
    {
        var lastRedeemTimeGapIsInRange = SysTime.UnixNow <
                                         PlayerDataForGame.instance.pyData.LastJiuTanRedeemTime +
                                         JiuTanTimeGapSecs * 1000;
        if (lastRedeemTimeGapIsInRange) return false;
        var jiuTanRedeemLimitNotReach =
            PlayerDataForGame.instance.pyData.DailyJiuTanRedemptionCount < JiuTanRedeemCountPerDay;
        return !lastRedeemIsToday || jiuTanRedeemLimitNotReach;
    }
}