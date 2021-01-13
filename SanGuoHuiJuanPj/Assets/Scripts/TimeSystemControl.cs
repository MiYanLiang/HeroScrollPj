﻿using System;
using System.Collections;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class TimeSystemControl : MonoBehaviour
{
    public static TimeSystemControl instance;

    public static string staminaStr = "staminaNum";

    private string timeWebPath = "http://www.hko.gov.hk/cgi-bin/gts/time5a.pr?a=1";
    private string timeWebPath0 = "http://api.m.taobao.com/rest/api3.do?api=mtop.common.getTimestamp";

    #region UnityEditor
    public SystemTimer SystemTimer;//系统时间的脚本引用(编辑器里引用)
    public int JinNangTimeGapSecs = 3600;//锦囊间隔时间
    public int JiuTanTimeGapSecs = 3600;//酒坛间隔时间
    public int JinNangRedeemCountPerDay = 10;//锦囊一天可获取次数
    public int JiuTanRedeemCountPerDay = 10;//酒坛一天可获取次数
    #endregion

    public static string NetworkTimestampStr = "NetworkTimestamp";

    //private string fBoxOpenNeedTimes = "fBONT"; //游戏内的计时器记录免费宝箱开启时间
    private string fBoxOpenNeedTimes1 = "fBONT1";
    private string fBoxOpenNeedTimes2 = "fBONT2";
    //private string freeBoxOpenTime = "fBOT"; //记录免费宝箱网络开启时间
    private string freeBoxOpenTime1 = "fBOT1";
    private string freeBoxOpenTime2 = "fBOT2";

    //int openNeedSeconds;   //宝箱开启时间
    int openNeedSeconds1;
    int openNeedSeconds2;

    //int secondsNetTime_FreeBox = 0; //记录免费宝箱网络开启剩余时间
    int secondsNetTime_FreeBox1 = 0;
    int secondsNetTime_FreeBox2 = 0;
    //int secsRemain_JinNang = 0; //记录锦囊网络开启剩余时间
    //long openFreeBoxTimeLong;   //记录宝箱开启的网络long
    long openFreeBoxTimeLong1;
    long openFreeBoxTimeLong2;
    //long openJinNangTimeLong;   //记录锦囊开启的网络long

    private string tiLiHuiFuNeedTimes = "tLHFNT"; //游戏内的计时器记录体力恢复时间 单位秒
    private string tiLiHuiFuTime = "tLHFT"; //记录体力恢复满的网络时间点
    int oneTiLiHfSeconds = 600;   //单个体力恢复时间12分钟
    int secondsNetTime_TiLiHf = 0; //记录体力恢复网络剩余时间
    long tiLiHfTimeLong;   //记录体力恢复满的网络long
    int maxStaminaNum;  //记录最大体力值

    //[HideInInspector]
    //public bool isGetNetworkTime;   //标记是否获取到网络时间

    [HideInInspector]
    public bool isOpenMainScene;    //是否在主城界面

    bool isJiuTanReady;   //记录是否能开启宝箱
    bool isCanGetBox1;
    bool isCanGetBox2;
    bool isJNTimeValid;    //记录是否锦囊开启时间合法

    bool isNeedHuiFuTiLi;   //记录是否需要恢复体力

    //DateTime timeNow;
    //DateTime startTime;

    float secondHandAccurate = 0;   //精准游戏毫秒针
    float secondHandAccurate1 = 0;
    float secondHandAccurate2 = 0;
    float secondHandAccurateJN = 0;

    float secondHandAccurate_TL = 0;   //精准游戏毫秒针0

    //long nowTimeLong;   //当前网络时间戳long

    //鸡坛相关存档字符
    public static string openCKTime0_str = "openCKTime0"; //12点
    public static string openCKTime1_str = "openCKTime1"; //17点
    public static string openCKTime2_str = "openCKTime2"; //21点

    [HideInInspector]
    public bool isFInGame;  //记录当天首次进入游戏
    private static string dayOfyearStr = "dayOfyearStr";        //存放上次进入游戏是哪一天
    private static string yearStr = "yearStr";                  //存放上次进入游戏是哪一年
    
    private void Awake()
    {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;
        DontDestroyOnLoad(gameObject);
        isOpenMainScene = false;
    }

    private void Start()
    {
        StartGameToInitOpenTime();
    }

    //初次存档对时间相关数据进行初始化
    public void InitTimeRelatedData()
    {
        isJiuTanReady = true;

        isCanGetBox1 = false;
        openNeedSeconds1 = LoadJsonFile.GetGameValue(1);
        PlayerPrefs.SetInt(fBoxOpenNeedTimes1, openNeedSeconds1);

        openFreeBoxTimeLong1 = 0;
        PlayerPrefs.SetString(freeBoxOpenTime1, "0");

        //openFreeBoxTimeLong1 = 0;

        isCanGetBox2 = true;
        openFreeBoxTimeLong2 = 0;
        PlayerPrefs.SetInt(fBoxOpenNeedTimes2, 0);
        PlayerPrefs.SetString(freeBoxOpenTime2, "0");

        //isNeedHuiFuTiLi = false;
        PlayerPrefs.SetInt(staminaStr, int.Parse(LoadJsonFile.assetTableDatas[2].startValue));
        //tiLiHfTimeLong = 0;
        PlayerPrefs.SetInt(tiLiHuiFuNeedTimes, 0);
        PlayerPrefs.SetString(tiLiHuiFuTime, "0");
        maxStaminaNum = int.Parse(LoadJsonFile.assetTableDatas[2].startValue);
        isNeedHuiFuTiLi = (PlayerPrefs.GetInt(staminaStr) < maxStaminaNum);
        tiLiHfTimeLong = long.Parse(PlayerPrefs.GetString(tiLiHuiFuTime));

        PlayerPrefs.SetInt(openCKTime0_str, 0);
        PlayerPrefs.SetInt(openCKTime1_str, 0);
        PlayerPrefs.SetInt(openCKTime2_str, 0);

        PlayerPrefs.SetInt(dayOfyearStr, 0);
        PlayerPrefs.SetInt(yearStr, 2020);
    }

    //每次进入游戏对开启时间进行初始化
    private void StartGameToInitOpenTime()
    {
        JiuTanTimeGapSecs = LoadJsonFile.GetGameValue(0);  //宝箱开启时间
        openNeedSeconds1 = LoadJsonFile.GetGameValue(1);
        openNeedSeconds2 = LoadJsonFile.GetGameValue(2);

        isCanGetBox1 = (PlayerPrefs.GetInt(fBoxOpenNeedTimes1) <= 0);
        openFreeBoxTimeLong1 = long.Parse(PlayerPrefs.GetString(freeBoxOpenTime1));

        isCanGetBox2 = (PlayerPrefs.GetInt(fBoxOpenNeedTimes2) <= 0);
        openFreeBoxTimeLong2 = long.Parse(PlayerPrefs.GetString(freeBoxOpenTime2));

        //openJinNangTimeLong = long.Parse(PlayerPrefs.GetString(jinNangOpenTime));

        maxStaminaNum = int.Parse(LoadJsonFile.assetTableDatas[2].startValue);
        isNeedHuiFuTiLi = (PlayerPrefs.GetInt(staminaStr) < maxStaminaNum);
        long.TryParse(PlayerPrefs.GetString(tiLiHuiFuTime),out tiLiHfTimeLong);
    }

    private void Update()
    {
        UpdateJinNangTimer();
        UpdateJiuTanTimer();
        UpdateFreeBoxTimer1();
        UpdateFreeBoxTimer2();
        UpdateStaminaTimer();

        UpdateChickenShoping();
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

    //尝试刷新主城体力商店状态
    private void UpdateChickenShoping()
    {
        //在主界面的话
        if (isOpenMainScene)
        {
            UIManager.instance.InitOpenChickenTime(true);
            //UIManager.instance.InitOpenChickenTime(true);
        }
    }

    //时间显示格式
    public string TimeDisplayText(int seconds)
    {
        string str = string.Empty;
        if (seconds <= 0)
        {
            str = "";
        }
        else
        {
            if (seconds < 3600)
            {
                str = seconds / 60 + "分" + seconds % 60 + "秒";
            }
            else
            {
                if (seconds < 86400)
                {
                    str = seconds / 3600 + "时" + (seconds % 3600) / 60 + "分";
                }
                else
                {
                    str = seconds / 86400 + "天" + (seconds % 86400) / 3600 + "时";
                }
            }
        }
        return str;
    }

    /// <summary>
    /// 添加体力
    /// </summary>
    /// <param name="addNums"></param>
    public void AddTiLiNums(int addNums)
    {
        int needTimes = PlayerPrefs.GetInt(tiLiHuiFuNeedTimes);

        int cutTimes = oneTiLiHfSeconds * addNums;

        if (cutTimes < needTimes)
        {
            needTimes = needTimes - cutTimes;
            secondsNetTime_TiLiHf = secondsNetTime_TiLiHf - (cutTimes * 1000);
            PlayerPrefs.SetInt(tiLiHuiFuNeedTimes, needTimes);
        }
        else
        {
            int nowStamina = PlayerPrefs.GetInt(staminaStr);
            PlayerPrefs.SetInt(staminaStr, nowStamina + addNums);   //存入增加后得体力值

            if (isNeedHuiFuTiLi)
            {
                isNeedHuiFuTiLi = false;    //记录不需要再倒计时恢复体力

                PlayerPrefs.SetInt(tiLiHuiFuNeedTimes, 0);  //游戏内恢复满总时间归零

                tiLiHfTimeLong = 0; //体力恢复满的网络long标签归零

                PlayerPrefs.SetString(tiLiHuiFuTime, tiLiHfTimeLong.ToString());  //体力恢复满的网络时间点归零

                secondsNetTime_TiLiHf = 0;  //体力恢复网络剩余时间标签归零
            }

            if (isOpenMainScene)
            {
                UIManager.instance.UpdateShowTiLiInfo(TimeDisplayText(0));
            }
        }

        tiLiHfTimeLong = 0;
    }

    private void UpdateStaminaTimer()
    {
        if (isNeedHuiFuTiLi)
        {
            if (tiLiHfTimeLong == 0)
            {
                tiLiHfTimeLong = SystemTimer.NowUnixTicks + PlayerPrefs.GetInt(tiLiHuiFuNeedTimes) * 1000;
                PlayerPrefs.SetString(tiLiHuiFuTime, tiLiHfTimeLong.ToString());
            }

            int secondsCha = (int) ((tiLiHfTimeLong - SystemTimer.NowUnixTicks) / 1000);
            if (secondsNetTime_TiLiHf > secondsCha || (secondsNetTime_TiLiHf <= 0 && secondsCha != 0))
            {
                secondsNetTime_TiLiHf = secondsCha;
                int totalTimes = PlayerPrefs.GetInt(tiLiHuiFuNeedTimes);
                UpdateTiLiHuiFuTime(totalTimes - secondsNetTime_TiLiHf, totalTimes);
            }
        }
        else
        {
            if (isOpenMainScene)
            {
                UIManager.instance.UpdateShowTiLiInfo(TimeDisplayText(0));
            }
        }
    }

    //体力恢复时间缩减
    private void UpdateTiLiHuiFuTime(int cutSeconds, int totalTimes)
    {
        //Debug.Log("cutSeconds: " + cutSeconds + "  totalTimes: " + totalTimes);
        int secondsRemaining = 0;
        int totalTimesNums = totalTimes;    //恢复满总需时间

        totalTimesNums -= cutSeconds;
        if (totalTimesNums <= 0)
        {
            totalTimesNums = 0;
            PlayerPrefs.SetInt(staminaStr, maxStaminaNum);
            secondsRemaining = 0;
            isNeedHuiFuTiLi = false;
            tiLiHfTimeLong = 0;
            PlayerPrefs.SetString(tiLiHuiFuTime, "0");
        }
        else
        {
            secondsRemaining = totalTimesNums % oneTiLiHfSeconds;
            int nowStaminaNum = (int.Parse(LoadJsonFile.assetTableDatas[2].startValue) * oneTiLiHfSeconds - totalTimesNums) / oneTiLiHfSeconds;
            PlayerPrefs.SetInt(staminaStr, nowStaminaNum);
        }
        if (isOpenMainScene)
        {
            UIManager.instance.UpdateShowTiLiInfo(TimeDisplayText(secondsRemaining));
        }
        PlayerPrefs.SetInt(tiLiHuiFuNeedTimes, totalTimesNums);
    }

    //消耗体力
    public void LetTiLiTimerTake(int usedNums)
    {
        int nowStamina = PlayerPrefs.GetInt(staminaStr);

        if ((nowStamina - usedNums) < maxStaminaNum)
        {
            isNeedHuiFuTiLi = true;
            if (nowStamina > maxStaminaNum)
            {
                usedNums = usedNums - (nowStamina - maxStaminaNum);
            }

            PlayerPrefs.SetInt(tiLiHuiFuNeedTimes,
                oneTiLiHfSeconds * usedNums + PlayerPrefs.GetInt(tiLiHuiFuNeedTimes));

            secondsNetTime_TiLiHf += (usedNums * oneTiLiHfSeconds * 1000);

            if (tiLiHfTimeLong == 0)
            {
                long huiFuTimeLong = SystemTimer.NowUnixTicks;
                huiFuTimeLong += (PlayerPrefs.GetInt(tiLiHuiFuNeedTimes) * 1000);
                tiLiHfTimeLong = huiFuTimeLong;
            }
            else
            {
                tiLiHfTimeLong += (oneTiLiHfSeconds * 1000 * usedNums);
            }

            PlayerPrefs.SetString(tiLiHuiFuTime, tiLiHfTimeLong.ToString());
        }
        else
        {
            isNeedHuiFuTiLi = false;
        }
    }

    private void UpdateJiuTanTimer()
    {
        var playerData = PlayerDataForGame.instance.pyData;
        var nextOpenJiuTanTimeTicks = playerData.lastJiuTanRedeemTime + (JiuTanTimeGapSecs * 1000);
        var redeemCount = playerData.dailyJiuTanRedemptionCount;
        var countAvailable = redeemCount < JiuTanRedeemCountPerDay;

        if (!SystemTimer.IsToday(playerData.lastJiuTanRedeemTime) && playerData.dailyJiuTanRedemptionCount > 0)
            PlayerDataForGame.instance.SetRedeemCount(PlayerDataForGame.RedeemTypes.JiuTan, 0);

        //如果当前时间大于下次开启时间
        isJiuTanReady = nextOpenJiuTanTimeTicks <= SystemTimer.NowUnixTicks;
        if (!isOpenMainScene) return;
        if (UIManager.instance.JiuTanQuota != null)
            UIManager.instance.JiuTanQuota.text = $"{JiuTanRedeemCountPerDay-redeemCount}";
        if (!isJiuTanReady)
        {
            var displayText = string.Empty;
            if (countAvailable)
                displayText =
                    $"灌满酒坛还需：{TimeDisplayText((int) (nextOpenJiuTanTimeTicks - SystemTimer.NowUnixTicks) / 1000)}";
            UIManager.instance.transform.GetComponent<GetOrOpenBox>().UpdateOpenTimeTips(displayText, 0, false);
        }
        else
            UIManager.instance.transform.GetComponent<GetOrOpenBox>().UpdateOpenTimeTips(string.Empty, 0, true);
    }

    private void UpdateFreeBoxTimer1()
    {
        if (!isCanGetBox1)
        {
            if (openFreeBoxTimeLong1 == 0)
            {
                openFreeBoxTimeLong1 = SystemTimer.NowUnixTicks + PlayerPrefs.GetInt(fBoxOpenNeedTimes1) * 1000;
                PlayerPrefs.SetString(freeBoxOpenTime1, openFreeBoxTimeLong1.ToString());
            }

            int secondCha = (int) ((openFreeBoxTimeLong1 - SystemTimer.NowUnixTicks) / 1000);
            if (secondsNetTime_FreeBox1 > secondCha || (secondsNetTime_FreeBox1 <= 0 && secondCha != 0))
            {
                secondsNetTime_FreeBox1 = secondCha;
                int totalTimes = PlayerPrefs.GetInt(fBoxOpenNeedTimes1);
                UpdateBoxOpenTimeFromGame1(totalTimes - secondsNetTime_FreeBox1, totalTimes);
            }
        }
        else
        {
            if (isOpenMainScene)
            {
                UIManager.instance.transform.GetComponent<GetOrOpenBox>().UpdateOpenTimeTips(string.Empty, 1, true);
            }
        }
    }

    private void UpdateFreeBoxTimer2()
    {
        if (!isCanGetBox2)
        {
            if (openFreeBoxTimeLong2 == 0)
            {
                openFreeBoxTimeLong2 = SystemTimer.NowUnixTicks + PlayerPrefs.GetInt(fBoxOpenNeedTimes2) * 1000;
                PlayerPrefs.SetString(freeBoxOpenTime2, openFreeBoxTimeLong2.ToString());
            }

            int secondCha = (int) ((openFreeBoxTimeLong2 - SystemTimer.NowUnixTicks) / 1000);
            if (secondsNetTime_FreeBox2 > secondCha || (secondsNetTime_FreeBox2 <= 0 && secondCha != 0))
            {
                secondsNetTime_FreeBox2 = secondCha;
                int totalTimes = PlayerPrefs.GetInt(fBoxOpenNeedTimes2);
                UpdateBoxOpenTimeFromGame2(totalTimes - secondsNetTime_FreeBox2, totalTimes);
            }
        }
        else
        {
            if (isOpenMainScene)
            {
                UIManager.instance.transform.GetComponent<GetOrOpenBox>().UpdateOpenTimeTips(string.Empty, 2, true);
            }
        }
    }

    private void UpdateJinNangTimer()
    {
        var playerData = PlayerDataForGame.instance.pyData;
        var nextOpenJNTimeTicks = playerData.lastJinNangRedeemTime + (JinNangTimeGapSecs * 1000);

        if (!SystemTimer.IsToday(playerData.lastJinNangRedeemTime) && playerData.dailyJinNangRedemptionCount > 0)//如果上次获取时间不是今天
            PlayerDataForGame.instance.SetRedeemCount(PlayerDataForGame.RedeemTypes.JinNang, 0);//如果获取次数大于0，重置获取次数

        //如果下次开启时间小于现在，(时间过期)
        isJNTimeValid = nextOpenJNTimeTicks < SystemTimer.NowUnixTicks;

        if (!isOpenMainScene) return;
        UIManager.instance.UpdateShowJinNangBtn(isJNTimeValid);
    }

    private void UpdateBoxOpenTimeFromGame1(int cutSeconds, int totalTimes)
    {
        int countDownNums = totalTimes;
        countDownNums -= cutSeconds;
        if (countDownNums <= 0)
        {
            countDownNums = 0;
            isCanGetBox1 = true;
            if (isOpenMainScene)
            {
                UIManager.instance.transform.GetComponent<GetOrOpenBox>().UpdateOpenTimeTips(string.Empty, 1, true);
            }
        }
        else
        {
            if (isOpenMainScene)
            {
                UIManager.instance.transform.GetComponent<GetOrOpenBox>().UpdateOpenTimeTips(TimeDisplayText(countDownNums), 1, false);
            }
        }
        PlayerPrefs.SetInt(fBoxOpenNeedTimes1, countDownNums);
    }

    private void UpdateBoxOpenTimeFromGame2(int cutSeconds, int totalTimes)
    {
        int countDownNums = totalTimes;
        countDownNums -= cutSeconds;
        if (countDownNums <= 0)
        {
            countDownNums = 0;
            isCanGetBox2 = true;
            if (isOpenMainScene)
            {
                UIManager.instance.transform.GetComponent<GetOrOpenBox>().UpdateOpenTimeTips(string.Empty, 2, true);
            }
        }
        else
        {
            if (isOpenMainScene)
            {
                UIManager.instance.transform.GetComponent<GetOrOpenBox>().UpdateOpenTimeTips(TimeDisplayText(countDownNums), 2, false);
            }
        }
        PlayerPrefs.SetInt(fBoxOpenNeedTimes2, countDownNums);
    }

    /// <summary>
    /// 开启免费宝箱
    /// </summary>
    /// <returns></returns>
    public bool OnClickToGetJiuTan()
    {
        if (!isJiuTanReady)
        {
            PlayerDataForGame.instance.ShowStringTips("尚未灌满酒坛");
            return false;
        }

        var player = PlayerDataForGame.instance.pyData;
        var jTRedeemCount = player.dailyJiuTanRedemptionCount;

        //如果酒坛获取已达限制次数，尝试执行是否时间过了0点重置次数
        if (jTRedeemCount >= JiuTanRedeemCountPerDay)
        {
            UIManager.instance.ItemsRedemptionFunc();
            jTRedeemCount = player.dailyJiuTanRedemptionCount;
        }

        if (jTRedeemCount >= JiuTanRedeemCountPerDay) return false;
        //Debug.Log("打开免费宝箱");
        isJiuTanReady = false;
        return true;
    }

    public bool OnClickToGetFreeBox1()
    {
        if (isCanGetBox1)
        {
            //Debug.Log("免费打开宝箱1");
            secondsNetTime_FreeBox1 = 0;
            isCanGetBox1 = false;
            PlayerPrefs.SetInt(fBoxOpenNeedTimes1, openNeedSeconds1);
            long nowTimeStr = SystemTimer.NowUnixTicks; //long.Parse(PlayerPrefs.GetString(NetworkTimestampStr));
            nowTimeStr += (openNeedSeconds1 * 1000);
            openFreeBoxTimeLong1 = nowTimeStr;
            PlayerPrefs.SetString(freeBoxOpenTime1, nowTimeStr.ToString());
            return true;
        }
        else
        {
            //Debug.Log("宝箱免费开启时间未到");
            return false;
        }
    }

    public bool OnClickToGetFreeBox2()
    {
        if (isCanGetBox2)
        {
            //Debug.Log("免费打开宝箱2");
            secondsNetTime_FreeBox2 = 0;
            isCanGetBox2 = false;
            PlayerPrefs.SetInt(fBoxOpenNeedTimes2, openNeedSeconds2);
            long nowTimeStr = SystemTimer.NowUnixTicks; //long.Parse(PlayerPrefs.GetString(NetworkTimestampStr));
            nowTimeStr += (openNeedSeconds2 * 1000);
            openFreeBoxTimeLong2 = nowTimeStr;
            PlayerPrefs.SetString(freeBoxOpenTime2, nowTimeStr.ToString());
            return true;
        }

        //Debug.Log("宝箱免费开启时间未到");
        return false;
    }

    //开启锦囊
    public bool OnClickToGetJinNang()
    {
        if (!isJNTimeValid) return false;
        var player = PlayerDataForGame.instance.pyData;
        var jNRedeemCount = player.dailyJinNangRedemptionCount;
        //如果锦囊获取已达限制次数，尝试执行是否时间过了0点重置次数
        if (jNRedeemCount >= JinNangRedeemCountPerDay)
        {
            UIManager.instance.ItemsRedemptionFunc();
            jNRedeemCount = player.dailyJinNangRedemptionCount;
        }

        Debug.Log(
            $"{nameof(TimeSystemControl)}:{nameof(OnClickToGetJinNang)} 锦囊获取次数[{player.dailyJinNangRedemptionCount+1}/{JinNangRedeemCountPerDay}]");
        if (jNRedeemCount >= JinNangRedeemCountPerDay) return false;
        //Debug.Log("打开锦囊");
        isJNTimeValid = false;
        if (UIManager.instance.JinNangQuota != null)
            UIManager.instance.JinNangQuota.text = $"今日次数：{player.dailyJinNangRedemptionCount+1}/{JinNangRedeemCountPerDay}";
        return true;

        //Debug.Log("宝箱免费开启时间未到");
    }
}