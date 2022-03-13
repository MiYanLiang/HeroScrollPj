//------------------------------------------------------------------------------
// Copyright (c) 2018-2019 Beijing Bytedance Technology Co., Ltd.
// All Right Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//------------------------------------------------------------------------------

using System;
using ByteDance.Union;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// The example for the SDK.
/// </summary>
public sealed class Example : MonoBehaviour
{
    [SerializeField]
    private Text information;

    private InputField inputX;
    private InputField inputY;
    private InputField width;
    private InputField height;
    private InputField adsRit;
    private InputField screenOrientation;
    private InputField expressFullExpressRit;
    private InputField intervalTime;
    private InputField splashButtonType;

    private AdNative adNative;
    private RewardVideoAd rewardAd;
    private FullScreenVideoAd fullScreenVideoAd;
    private NativeAd bannerAd;
    private BUSplashAd splashAd;
    private BUExpressSplashAd expressSplashAd;
    private NativeAd feedAd;
    private NativeAd intersititialAd;

    private AndroidJavaObject mBannerAd;
    private AndroidJavaObject mIntersititialAd;
    private AndroidJavaObject activity;
    private AndroidJavaObject mNativeAdManager;
    private AndroidJavaObject mSplashAdManager;
    public const int SPLASH_TIME_OUT = 3000;

#if UNITY_IOS
    private ExpressRewardVideoAd expressRewardAd; // for iOS
    private ExpressFullScreenVideoAd expressFullScreenVideoAd; // for iOS
    private ExpressBannerAd iExpressBannerAd; // for iOS
    private ExpressInterstitialAd iExpressInterstitialAd; // for iOS
#else

#endif

    private ExpressAd mExpressFeedad;
    private ExpressAd mExpressBannerAd;
    private ExpressAd mExpressInterstitialAd;

    
    // Unity 主线程ID:
    private static int mainThreadId ;

    private void Awake()
    {
        mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    private static void callbackmethod (bool success, string message) {
        Debug.Log("`````````````````初始化``````" + success + "-----" + message);
    }

    void Start () {
#if UNITY_IOS
        PangleConfiguration configuration = PangleConfiguration.CreateInstance();
        configuration.appID = "5000546";
#endif
        Pangle.InitializeSDK(callbackmethod
        //    , new CustomConfiguration
        //{
        //    CanUseLocation = false,
        //    CanReadAppList = false,
        //    CanUsePhoneState = false,
        //    CanUseWifiState = false,
        //    CanUseWriteExternal = false,
        //    MacAddress = "fake mac address",
        //    Latitude = 35.23,
        //    Longitude = 139.12,
        //    DevImei = "fake imei",
        //    DevOaid = "fake oaid"
        //}
            );
    }



    // 激励和全屏，开屏的横竖屏配置
    public int getScreenOrientationConfig()
    {
        this.screenOrientation = GameObject.Find("Canvas/Container/screenOrientation").GetComponent<InputField>();
        int screenOrientation;
        int.TryParse(this.screenOrientation.text, out screenOrientation);

        return screenOrientation;
    }

    private AdNative AdNative
    {
        get
        {
            if (this.adNative == null)
            {
                this.adNative = SDK.CreateAdNative();
            }
#if UNITY_ANDROID
            SDK.RequestPermissionIfNecessary();
#endif
            return this.adNative;
        }
    }

    public void LoadSplashAd()
    {
#if UNITY_IOS
        if (this.splashAd != null)
        {
            this.splashAd.Dispose();
            this.splashAd = null;
        }
        if (this.expressSplashAd != null)
        {
            this.expressSplashAd.Dispose();
            this.expressSplashAd = null;
        }
#endif
        string iosSlotID = "800546808";
        string AndroidSlotID = "801121648";
        // 横屏  开屏的横屏配置需要在服务端针对广告位进行设置
        if (this.getScreenOrientationConfig() == 1)
        {
            iosSlotID = "887341408";
            AndroidSlotID = "887341406";
        }
        
        Debug.Log($"splashClickByDownloadBar={splashClickByDownloadBar}");
        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
            .SetCodeId(iosSlotID)
#else
            .SetCodeId(AndroidSlotID)
#endif
            .SetImageAcceptedSize(1080, 1920)            
            .SetSplashButtonType(splashClickByDownloadBar?SplashButtonType.DownloadBar:SplashButtonType.FullScreen)
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
#if UNITY_IOS
        this.splashAd = (this.AdNative.LoadSplashAd_iOS(adSlot, new SplashAdListener(this), callbackOnMainThread));
#else
        this.AdNative.LoadSplashAd(adSlot, new SplashAdListener(this, GetActivity(), GetSplashAdManager()), SPLASH_TIME_OUT, callbackOnMainThread);
#endif
    }

    public void LoadExpressSplashAd()
    {
#if UNITY_IOS
        if (this.splashAd != null)
        {
            this.splashAd.Dispose();
            this.splashAd = null;
        }
        if (this.expressSplashAd != null)
        {
            this.expressSplashAd.Dispose();
            this.expressSplashAd = null;
        }
#endif
        string iosSlotID = "800546851";
        string AndroidSlotID = "801121974";
        // 横屏
        if (this.getScreenOrientationConfig() == 1)
        {
            iosSlotID = "887341405";
            AndroidSlotID = "887341399";
        }
        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
            .SetCodeId(iosSlotID)
#else
            .SetCodeId(AndroidSlotID)
#endif
            .SetImageAcceptedSize(1080, 1920)
            .SetExpressViewAcceptedSize(UnityEngine.Screen.width, UnityEngine.Screen.height)
            .SetSplashButtonType(splashClickByDownloadBar?SplashButtonType.DownloadBar:SplashButtonType.FullScreen)
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
#if UNITY_IOS
        this.expressSplashAd = (this.AdNative.LoadExpressSplashAd_iOS(adSlot, new ExpressSplashAdListener(this), callbackOnMainThread)) as BUExpressSplashAd;
#else
        this.AdNative.LoadSplashAd(adSlot, new SplashAdListener(this, GetActivity(), GetSplashAdManager()), callbackOnMainThread);
#endif

    }

    public void LoadNativeBannerAd()
    {
#if UNITY_IOS
        if (this.bannerAd != null)
        {
            this.bannerAd.Dispose();
            this.bannerAd = null;
        }
#else
        if (this.mBannerAd != null)
        {
            this.mBannerAd.Dispose();
            this.mBannerAd = null;
        }
#endif


        this.width = GameObject.Find("Canvas/Container/ExpressNativeSlot/Width").GetComponent<InputField>();
        this.height = GameObject.Find("Canvas/Container/ExpressNativeSlot/Height").GetComponent<InputField>();
        this.adsRit = GameObject.Find("Canvas/Container/AdsRit").GetComponent<InputField>();

        int width;
        int height;
        int.TryParse(this.width.text, out width);
        int.TryParse(this.height.text, out height);

        if (width == 0) {
            width = UnityEngine.Screen.width;
        }
        if (height == 0) {
            height = width / 600 * 257;
        }

        string adsRit = "901121423";
#if UNITY_IOS
        adsRit = this.adsRit.text;
        if (adsRit.Length == 0)
        {
            adsRit = "900546687";
        }
#else

#endif

        var adSlot = new AdSlot.Builder()
            .SetCodeId(adsRit)
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(width, height)
            .SetNativeAdType(AdSlotType.Banner)
            .SetAdCount(1)
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
        this.AdNative.LoadNativeAd(adSlot, new NativeAdListener(this), callbackOnMainThread);
    }

    public void LoadNativeIntersititialAd()
    {
#if UNITY_IOS
        if (this.intersititialAd != null)
        {
            this.intersititialAd.Dispose();
            this.intersititialAd = null;
        }
#else
        if (this.mIntersititialAd != null)
        {
            this.mIntersititialAd.Dispose();
            this.mIntersititialAd = null;
        }
#endif
        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
            .SetCodeId("900546829")
#else
            .SetCodeId("901121435")
#endif
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(600, 257)
            .SetNativeAdType(AdSlotType.InteractionAd)
            .SetAdCount(1)
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
        this.AdNative.LoadNativeAd(adSlot, new NativeAdListener(this), callbackOnMainThread);
    }

    /// <summary>
    /// Load the reward Ad.
    /// </summary>
    public void LoadRewardAd()
    {
        if (this.rewardAd != null)
        {
            this.rewardAd.Dispose();
            this.rewardAd = null;
        }

        string AndroidSlotID = "901121365";
        string iosSlotID = "900546826";
        // 横屏
        if (this.getScreenOrientationConfig() == 1)
        {
            iosSlotID = "900546319";
            AndroidSlotID = "901121430";
        }

        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
            .SetCodeId(iosSlotID)
#else
            .SetCodeId("948109196")
#endif
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(1080, 1920)
            .SetUserID("user123") // 用户id,必传参数
            //.SetMediaExtra("media_extra") // 附加参数，可选
            .SetOrientation(AdOrientation.Horizontal) // 必填参数，期望视频的播放方向
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();

        this.AdNative.LoadRewardVideoAd(adSlot, new RewardVideoAdListener(this), callbackOnMainThread);
    }

    /// <summary>
    /// Show the reward Ad.
    /// </summary>
    public void ShowRewardAd()
    {
        if (this.rewardAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        } else {
            this.rewardAd.ShowRewardVideoAd();
        }

    }

    /// <summary>
    /// Load the reward Ad.
    /// </summary>
    public void LoadExpressRewardAd()
    {
#if UNITY_IOS
        if (this.expressRewardAd != null)
        {
            this.expressRewardAd.Dispose();
            this.expressRewardAd = null;
        }
#else
        if (this.rewardAd != null)
        {
            this.rewardAd.Dispose();
            this.rewardAd = null;
        }
#endif


        string iosSlotID = "945113162";
        string AndroidSlotID = "901121593";
        // 横屏
        if (this.getScreenOrientationConfig() == 1)
        {
            iosSlotID = "945113163";
            AndroidSlotID = "901121543";
        }

        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
        // @"900546566";//竖屏
        // @"900546606";//横屏
            .SetCodeId(iosSlotID)
#else
            .SetCodeId(AndroidSlotID)
#endif
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(1080, 1920)
            .SetUserID("user123") // 用户id,必传参数
            .SetMediaExtra("media_extra") // 附加参数，可选
            .SetOrientation(AdOrientation.Horizontal) // 必填参数，期望视频的播放方向
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
#if UNITY_IOS
        this.AdNative.LoadExpressRewardAd(
            adSlot, new ExpressRewardVideoAdListener(this), callbackOnMainThread);
#else
        this.AdNative.LoadRewardVideoAd(
            adSlot, new RewardVideoAdListener(this), callbackOnMainThread);
#endif
    }

    /// <summary>
    /// Show the reward Ad.
    /// </summary>
    public void ShowExpressRewardAd()
    {
#if UNITY_IOS
        if (this.expressRewardAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        } else {
            this.expressRewardAd.ShowRewardVideoAd();
        }
#else
        if (this.rewardAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        } else {
            this.rewardAd.ShowRewardVideoAd();
        }
#endif
    }

    /// <summary>
    /// Loads the full screen video ad.
    /// </summary>
    public void LoadFullScreenVideoAd()
    {

        if (this.fullScreenVideoAd != null)
        {
            this.fullScreenVideoAd.Dispose();
            this.fullScreenVideoAd = null;
        }

        string iosSlotID = "900546299";
        string AndroidSlotID = "901121375";
        // 横屏
        if (this.getScreenOrientationConfig() == 1)
        {
            iosSlotID = "900546154";
            AndroidSlotID = "901121184";
        }

        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
                             .SetCodeId(iosSlotID)
#else
            .SetCodeId(AndroidSlotID)
#endif
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(1080, 1920)
            .SetOrientation(AdOrientation.Horizontal)
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
        this.AdNative.LoadFullScreenVideoAd(adSlot, new FullScreenVideoAdListener(this), callbackOnMainThread);

    }

    /// <summary>
    /// Show the reward Ad.
    /// </summary>
    public void ShowFullScreenVideoAd()
    {

        if (this.fullScreenVideoAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        } else {
            this.fullScreenVideoAd.ShowFullScreenVideoAd();
        }
    }

    /// <summary>
    /// Loads the full screen video ad.
    /// </summary>
    public void LoadExpressFullScreenVideoAd()
    {
#if UNITY_IOS
        if (this.expressFullScreenVideoAd != null)
        {
            this.expressFullScreenVideoAd.Dispose();
            this.expressFullScreenVideoAd = null;
        }
#endif
        this.expressFullExpressRit = GameObject.Find("Canvas/Container/ExpressFullScreenRit").GetComponent<InputField>();
        string rit = this.expressFullExpressRit.text;
        string iosSlotID = "";
        string AndroidSlotID = "";

        if (string.IsNullOrEmpty(rit))
        {
            iosSlotID = "945113164";
            AndroidSlotID = "901121073";
        }
        else
        {
            iosSlotID = rit;
            AndroidSlotID = rit;
        }
        // 横屏
        if (this.getScreenOrientationConfig() == 1)
        {
            iosSlotID = "945113165";
            AndroidSlotID = "901121516";
        }

        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
        // 900546551 竖屏
        // 900546831 横屏
                             .SetCodeId(iosSlotID)
#else
            .SetCodeId(AndroidSlotID)
#endif
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(1080, 1920)
            .SetOrientation(AdOrientation.Horizontal)
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
#if UNITY_IOS
        this.AdNative.LoadExpressFullScreenVideoAd(adSlot, new ExpressFullScreenVideoAdListener(this), callbackOnMainThread);
#else
        this.AdNative.LoadFullScreenVideoAd(adSlot, new FullScreenVideoAdListener(this), callbackOnMainThread);
#endif
    }

    /// <summary>
    /// Show the reward Ad.
    /// </summary>
    public void ShowExpressFullScreenVideoAd()
    {
#if UNITY_IOS
        if (this.expressFullScreenVideoAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        } else {
            this.expressFullScreenVideoAd.ShowFullScreenVideoAd();
        }
#else
        if (this.fullScreenVideoAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        } else {
            this.fullScreenVideoAd.ShowFullScreenVideoAd();
        }
#endif
    }

    public void LoadExpressBannerAd()
    {
#if UNITY_IOS
        if (this.iExpressBannerAd != null)
        {
            this.iExpressBannerAd.Dispose();
            this.iExpressBannerAd = null;
        }
#else
        if (this.mExpressBannerAd != null)
        {
            this.mExpressBannerAd.Dispose();
            this.mExpressBannerAd = null;
        }
#endif

        this.width = GameObject.Find("Canvas/Container/ExpressNativeSlot/Width").GetComponent<InputField>();
        this.height = GameObject.Find("Canvas/Container/ExpressNativeSlot/Height").GetComponent<InputField>();
        this.adsRit = GameObject.Find("Canvas/Container/AdsRit").GetComponent<InputField>();
        int width;
        int height;
        int.TryParse(this.width.text, out width);
        int.TryParse(this.height.text, out height);

        if (width == 0) {
            width = UnityEngine.Screen.width;
        }
        if (height == 0) {
            height = width / 600 * 90;
        }

        string adsRit = "";

        adsRit = this.adsRit.text;
        if (adsRit.Length == 0)
        {
#if UNITY_IOS
            adsRit = "900546269";
#else
            adsRit = "901121246";
#endif
        }

        this.intervalTime = GameObject.Find("Canvas/Container/ExpressNativeSlot/IntervalTime").GetComponent<InputField>();
        int intervalTime;
        int.TryParse(this.intervalTime.text, out intervalTime);

        Debug.Log("轮播时间--" + intervalTime);

        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
                             .SetCodeId(adsRit)
                             .SetSlideIntervalTime(intervalTime)
#else
            .SetCodeId(adsRit)
            ////期望模板广告view的size,单位dp，//高度按照实际rit对应宽高传入
#endif
            .SetExpressViewAcceptedSize(width, height)
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(1080, 1920)
            .SetAdCount(1)
            .SetOrientation(AdOrientation.Horizontal)
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();

        Debug.LogError("SetExpressViewAcceptedSize:" + float.Parse(UnityEngine.Screen.width.ToString()) / 600 * 90);
        this.AdNative.LoadExpressBannerAd(adSlot, new ExpressAdListener(this, 1), callbackOnMainThread);

    }

    /// <summary>
    /// Show the reward Ad.
    /// </summary>
    public void ShowExpressBannerAd()
    {
        this.intervalTime = GameObject.Find("Canvas/Container/ExpressNativeSlot/IntervalTime").GetComponent<InputField>();
        int intervalTime;
        int.TryParse(this.intervalTime.text, out intervalTime);
#if UNITY_IOS
        if (this.iExpressBannerAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }

        this.inputX = GameObject.Find("Canvas/Container/InputField/InputFieldX").GetComponent<InputField>();
        this.inputY = GameObject.Find("Canvas/Container/InputField/InputFieldY").GetComponent<InputField>();

        int x;
        int y;
        int.TryParse(this.inputX.text, out x);
        int.TryParse(this.inputY.text, out y);
        Debug.Log("Show x坐标---" + x);
        Debug.Log("Show y坐标---" + y);

        if (y == 0)
        {
            y = 100; // 防止Demo里刘海遮挡
        }

        this.iExpressBannerAd.ShowExpressAd(x, y);
#else
        if (this.mExpressBannerAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }
        //设置轮播间隔 30s--120s;不设置则不开启轮播
        if (intervalTime <= 0)
        {
            this.mExpressBannerAd.SetSlideIntervalTime(30 * 1000);
        }
        else
        {
            this.mExpressBannerAd.SetSlideIntervalTime(intervalTime * 1000);
        }

        ExpressAdInteractionListener expressAdInteractionListener = new ExpressAdInteractionListener(this, 1);
        ExpressAdDislikeCallback dislikeCallback = new ExpressAdDislikeCallback(this, 1);
        this.mExpressBannerAd.SetDownloadListener(
            new AppDownloadListener(this));
        NativeAdManager.Instance().ShowExpressBannerAd(GetActivity(), mExpressBannerAd.handle, expressAdInteractionListener, dislikeCallback);
#endif
    }

    public void LoadExpressInterstitialAd()
    {
#if UNITY_IOS
        if (this.iExpressInterstitialAd != null)
        {
            this.iExpressInterstitialAd.Dispose();
            this.iExpressInterstitialAd = null;
        }
#else
        if (this.mExpressInterstitialAd != null)
        {
            this.mExpressInterstitialAd.Dispose();
            this.mExpressInterstitialAd = null;
        }
#endif
        this.width = GameObject.Find("Canvas/Container/ExpressNativeSlot/Width").GetComponent<InputField>();
        this.height = GameObject.Find("Canvas/Container/ExpressNativeSlot/Height").GetComponent<InputField>();
        this.adsRit = GameObject.Find("Canvas/Container/AdsRit").GetComponent<InputField>();

        int width;
        int height;
        int.TryParse(this.width.text, out width);
        int.TryParse(this.height.text, out height);

        if (width == 0) {
            width = UnityEngine.Screen.width;
        }
        if (height == 0) {
            height = width / 200 * 300;
        }

        string adsRit = "901121133";
#if UNITY_IOS
        adsRit = this.adsRit.text;
        if (adsRit.Length == 0)
        {
            adsRit = "945870989";
        }
#endif
        var adSlot = new AdSlot.Builder()
            .SetCodeId(adsRit)
            .SetExpressViewAcceptedSize(width, height)
            ////期望模板广告view的size,单位dp，//高度设置为0,则高度会自适应
            .SetSupportDeepLink(true)
            .SetAdCount(1)
            .SetImageAcceptedSize(1080, 1920)
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
        this.AdNative.LoadExpressInterstitialAd(adSlot, new ExpressAdListener(this, 2), callbackOnMainThread);

    }

    /// <summary>
    /// Show the reward Ad.
    /// </summary>
    public void ShowExpressInterstitialAd()
    {
#if UNITY_IOS
        if (this.iExpressInterstitialAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }

        this.inputX = GameObject.Find("Canvas/Container/InputField/InputFieldX").GetComponent<InputField>();
        this.inputY = GameObject.Find("Canvas/Container/InputField/InputFieldY").GetComponent<InputField>();

        int x;
        int y;
        int.TryParse(this.inputX.text, out x);
        int.TryParse(this.inputY.text, out y);
        Debug.Log("Show x坐标---" + x);
        Debug.Log("Show y坐标---" + y);

        if (y == 0)
        {
            y = 100; // 防止Demo里刘海遮挡
        }

        this.iExpressInterstitialAd.ShowExpressAd(x, y);
#else
        if (this.mExpressInterstitialAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }
        ExpressAdInteractionListener expressAdInteractionListener = new ExpressAdInteractionListener(this, 1);
        this.mExpressInterstitialAd.SetDownloadListener(
            new AppDownloadListener(this));
        NativeAdManager.Instance().ShowExpressInterstitialAd(GetActivity(), mExpressInterstitialAd.handle, expressAdInteractionListener);
#endif
    }

    public void LoadExpressFeedAd()
    {
#if UNITY_IOS
        if (this.mExpressFeedad != null)
        {
            this.mExpressFeedad.Dispose();
            this.mExpressFeedad = null;
        }
#else
        if (this.mExpressFeedad != null)
        {
            this.mExpressFeedad.Dispose();
            this.mExpressFeedad = null;
        }
#endif
        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
                             .SetCodeId("945870984")
#else
            .SetCodeId("901121253")
            ////期望模板广告view的size,单位dp，//高度设置为0,则高度会自适应
            .SetExpressViewAcceptedSize(350, 0)
#endif
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(1080, 1920)
            .SetOrientation(AdOrientation.Horizontal)
            .SetAdCount(1) //请求广告数量为1到3条
#if UNITY_ANDROID
            .SetDownloadType(showDownloadConfirmDialog?DownloadType.DownloadTypePopup:DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
        this.AdNative.LoadNativeExpressAd(adSlot, new ExpressAdListener(this, 0), callbackOnMainThread);

    }
    /// <summary>
    /// Show the expressFeed Ad.
    /// </summary>
    public void ShowExpressFeedAd()
    {
#if UNITY_IOS
        if (this.mExpressFeedad == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }

        this.inputX = GameObject.Find("Canvas/Container/InputField/InputFieldX").GetComponent<InputField>();
        this.inputY = GameObject.Find("Canvas/Container/InputField/InputFieldY").GetComponent<InputField>();

        int x;
        int y;
        int.TryParse(this.inputX.text, out x);
        int.TryParse(this.inputY.text, out y);
        Debug.Log("Show x坐标---" + x);
        Debug.Log("Show y坐标---" + y);

        if (y == 0)
        {
            y = 100; // 防止Demo里刘海遮挡
        }

        this.mExpressFeedad.ShowExpressAd(x, y);
#else
        if (this.mExpressFeedad == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }
        ExpressAdInteractionListener expressAdInteractionListener = new ExpressAdInteractionListener(this, 0);
        ExpressAdDislikeCallback dislikeCallback = new ExpressAdDislikeCallback(this, 0);
        this.mExpressFeedad.SetExpressInteractionListener(
            expressAdInteractionListener, callbackOnMainThread);
        this.mExpressFeedad.SetDownloadListener(
            new AppDownloadListener(this));
        NativeAdManager.Instance().ShowExpressFeedAd(GetActivity(),mExpressFeedad.handle,expressAdInteractionListener,dislikeCallback);
#endif
    }

    public void ShowNativeBannerAd()
    {
#if UNITY_IOS
       if (bannerAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }

        this.inputX = GameObject.Find("Canvas/Container/InputField/InputFieldX").GetComponent<InputField>();
        this.inputY = GameObject.Find("Canvas/Container/InputField/InputFieldY").GetComponent<InputField>();

        int x;
        int y;
        int.TryParse(this.inputX.text, out x);
        int.TryParse(this.inputY.text, out y);
        Debug.Log("Show x坐标---" + x);
        Debug.Log("Show y坐标---" + y);

        if (y == 0)
        {
            y = 100; // 防止Demo里刘海遮挡
        }

        this.bannerAd.ShowNativeAd(AdSlotType.Banner, x, y);
#else
        if (mBannerAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }

        if (mNativeAdManager == null)
        {
            mNativeAdManager = GetNativeAdManager();
        }

        // mNativeAdManager.Call("showNativeBannerAd", activity, mBannerAd);
        object[] objs = {activity, mBannerAd};
        var signature = "(Landroid.content.Context;Lcom.bytedance.sdk.openadsdk.TTNativeAd;)V";
        CallJavaMethod(mNativeAdManager, "showNativeBannerAd", signature, objs);
#endif
    }

    public void ShowNativeIntersititialAd()
    {
#if UNITY_IOS
        if (intersititialAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }

        this.inputX = GameObject.Find("Canvas/Container/InputField/InputFieldX").GetComponent<InputField>();
        this.inputY = GameObject.Find("Canvas/Container/InputField/InputFieldY").GetComponent<InputField>();

        int x;
        int y;
        int.TryParse(this.inputX.text, out x);
        int.TryParse(this.inputY.text, out y);
        Debug.Log("Show x坐标---" + x);
        Debug.Log("Show y坐标---" + y);

        Debug.Log("ShowExpressAd WindowSafeAreaInsetsTop---" + PangleTools.getWindowSafeAreaInsetsTop());
        Debug.Log("ShowExpressAd WindowSafeAreaInsetsLeft---" + PangleTools.getWindowSafeAreaInsetsLeft());
        Debug.Log("ShowExpressAd WindowSafeAreaInsetsBottom---" + PangleTools.getWindowSafeAreaInsetsBottom());
        Debug.Log("ShowExpressAd WindowSafeAreaInsetsRight---" + PangleTools.getWindowSafeAreaInsetsRight());
        Debug.Log("ShowExpressAd ScreenWidth---" + PangleTools.getScreenWidth());
        Debug.Log("ShowExpressAd ScreenHeight---" + PangleTools.getScreenHeight());

        if (y == 0)
        {
            y = 100; // 防止Demo里刘海遮挡
        }

        this.intersititialAd.ShowNativeAd(AdSlotType.InteractionAd, x, y);
#else
        if (mIntersititialAd == null)
        {
            Debug.LogError("请先加载广告");
            this.information.text = "请先加载广告";
            return;
        }

        if (mNativeAdManager == null)
        {
            mNativeAdManager = GetNativeAdManager();
        }

        // mNativeAdManager.Call("showNativeIntersititialAd", activity, mIntersititialAd);
        
        object[] objs = {activity, mIntersititialAd};
        var signature = "(Landroid.content.Context;Lcom.bytedance.sdk.openadsdk.TTNativeAd;)V";
        Debug.Log(signature);
        CallJavaMethod(mNativeAdManager, "showNativeIntersititialAd", signature, objs);
#endif
    }

    public void setCoppaButtonClicked()
    {
        #if UNITY_IOS
        InputField coppaField = GameObject.Find("Canvas/Container/CoppaPanel/CoppaValue").GetComponent<InputField>();
        if (coppaField.text.Length == 0)
        {
            return;
        } 
        int coppa;
        int.TryParse(coppaField.text, out coppa);
        Pangle.setCoppa(coppa);
        #endif
    }

    public void getCoppaButtonClicked()
    {
#if UNITY_IOS
        int coppa = Pangle.getCoppa();
        Text coppaText = GameObject.Find("Canvas/Container/CoppaPanel/CoppaText").GetComponent<Text>();
        coppaText.text = "Coppa = "+coppa.ToString();
#endif
    }


    public void fetchSlotIdClicked()
    {
#if UNITY_EDITOR
        Text SlotIdText = GameObject.Find("Canvas/Container/ABSlotPannel/SlotIdText").GetComponent<Text>();
        InputField inputFiled = GameObject.Find("Canvas/Container/ABSlotPannel/codeGroup").GetComponent<InputField>();
        int codeGroupId;
        if (int.TryParse(inputFiled.text, out codeGroupId))
        {
            BUSlotABManager.FetchSlotWithCodeGroupId(codeGroupId, new BUABSlotManagerListener());
        }
        else
        {
            this.information.text = "请输入正确的code group id";
        }
#endif
    }
    public AndroidJavaObject GetNativeAdManager()
    {
        if (mNativeAdManager != null)
        {
            return mNativeAdManager;
        }

        if (activity == null)
        {
            var unityPlayer = new AndroidJavaClass(
                "com.unity3d.player.UnityPlayer");
            activity = unityPlayer.GetStatic<AndroidJavaObject>(
                "currentActivity");
        }

        var jc = new AndroidJavaClass(
            "com.bytedance.android.NativeAdManager");
        mNativeAdManager = jc.CallStatic<AndroidJavaObject>("getNativeAdManager");
        return mNativeAdManager;
    }

    public AndroidJavaObject GetSplashAdManager()
    {
        if (mSplashAdManager != null)
        {
            return mSplashAdManager;
        }

        var jc = new AndroidJavaClass(
            "com.bytedance.android.SplashAdManager");
        mSplashAdManager = jc.CallStatic<AndroidJavaObject>("getSplashAdManager");
        return mSplashAdManager;
    }

    public AndroidJavaObject GetActivity()
    {
        if (activity == null)
        {
            var unityPlayer = new AndroidJavaClass(
                "com.unity3d.player.UnityPlayer");
            activity = unityPlayer.GetStatic<AndroidJavaObject>(
                "currentActivity");
        }

        return activity;
    }

    /// <summary>
    /// Dispose the reward Ad.
    /// </summary>
    public void DisposeAds()
    {
#if UNITY_IOS
        if (this.rewardAd != null)
        {
            this.rewardAd.Dispose();
            this.rewardAd = null;
        }
        
        if (this.fullScreenVideoAd != null)
        {
            this.fullScreenVideoAd.Dispose();
            this.fullScreenVideoAd = null;
        }

        if (this.bannerAd != null)
        {
            this.bannerAd.Dispose();
            this.bannerAd = null;
        }

        if (this.intersititialAd != null)
        {
            this.intersititialAd.Dispose();
            this.intersititialAd = null;
        }

        if (this.expressRewardAd != null)
        {
            this.expressRewardAd.Dispose();
            this.expressRewardAd = null;
        }

        if (this.expressFullScreenVideoAd != null)
        {
            this.expressFullScreenVideoAd.Dispose();
            this.expressFullScreenVideoAd = null;
        }

        if (this.iExpressBannerAd != null)
        {
            this.iExpressBannerAd.Dispose();
            this.iExpressBannerAd = null;
        }

        if (this.feedAd != null)
        {
            this.feedAd.Dispose();
            this.feedAd = null;
        }

        if (this.splashAd != null)
        {
            this.splashAd.Dispose();
            this.splashAd = null;
        }

        if (this.expressSplashAd != null)
        {
            this.expressSplashAd.Dispose();
            this.expressSplashAd = null;
        }

#else
        if (this.rewardAd != null)
        {
            this.rewardAd = null;
        }

        if (this.fullScreenVideoAd != null)
        {
            this.fullScreenVideoAd = null;
        }

        if (this.mBannerAd != null)
        {
            this.mBannerAd = null;
        }

        if (this.mIntersititialAd != null)
        {
            this.mIntersititialAd = null;
        }

        if (this.mExpressFeedad != null)
        {
            NativeAdManager.Instance().DestoryExpressAd(mExpressFeedad.handle);
            mExpressFeedad = null;
        }

        if (this.mExpressBannerAd != null)
        {
            NativeAdManager.Instance().DestoryExpressAd(mExpressBannerAd.handle);
            mExpressBannerAd = null;
        }

        if (this.mExpressInterstitialAd != null)
        {
            NativeAdManager.Instance().DestoryExpressAd(mExpressInterstitialAd.handle);
            mExpressInterstitialAd = null;
        }

        if (this.splashAd != null)
        {
            this.splashAd = null;
        }
#endif
    }

    private sealed class RewardVideoAdListener : IRewardVideoAdListener
    {
        private Example example;

        public RewardVideoAdListener(Example example)
        {
            this.example = example;
        }

        public void OnError(int code, string message)
        {
            Debug.LogError($"OnRewardError: {message}  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnRewardError: " + message;
        }

        public void OnRewardVideoAdLoad(RewardVideoAd ad)
        {
#if UNITY_ANDROID
            var info = ad.GetMediaExtraInfo();
            Debug.Log($"OnRewardVideoAdLoad info:  expireTimestamp={info["expireTimestamp"]},materialMetaIsFromPreload={info["materialMetaIsFromPreload"]}");
#endif
            Debug.Log($"OnRewardVideoAdLoad on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnRewardVideoAdLoad";

            ad.SetRewardAdInteractionListener(
                new RewardAdInteractionListener(this.example), callbackOnMainThread);
            ad.SetAgainRewardAdInteractionListener(
                    new RewardAgainAdInteractionListener(this.example), callbackOnMainThread);
            ad.SetDownloadListener(
                new AppDownloadListener(this.example), callbackOnMainThread);

            this.example.rewardAd = ad;
        }

        public void OnExpressRewardVideoAdLoad(ExpressRewardVideoAd ad)
        {
        }

        public void OnRewardVideoCached()
        {
            Debug.Log($"OnRewardVideoCached  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnRewardVideoCached";
#if UNITY_IOS
            this.example.rewardAd.setAuctionPrice(1.0);
            this.example.rewardAd.win(1.0);
            this.example.rewardAd.Loss(1.0,"102",bidder:"ylh");
#endif
        }

        public void OnRewardVideoCached(RewardVideoAd ad)
        {
            Debug.Log($"OnRewardVideoCached RewardVideoAd ad on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
        }
    }

    private sealed class ExpressRewardVideoAdListener : IRewardVideoAdListener
    {
        private Example example;

        public ExpressRewardVideoAdListener(Example example)
        {
            this.example = example;
        }

        public void OnError(int code, string message)
        {
            Debug.LogError($"OnRewardError: {message}  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(callbackOnMainThread)
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnRewardError: " + message;
        }

        public void OnRewardVideoAdLoad(RewardVideoAd ad)
        {
            Debug.Log($"OnRewardVideoAdLoad on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(callbackOnMainThread)
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnRewardVideoAdLoad";
            ad.SetRewardAdInteractionListener(
                new RewardAdInteractionListener(this.example), callbackOnMainThread);
            ad.SetDownloadListener(
                new AppDownloadListener(this.example), callbackOnMainThread);

            this.example.rewardAd = ad;
        }

        // iOS
        public void OnExpressRewardVideoAdLoad(ExpressRewardVideoAd ad)
        {
#if UNITY_IOS
            Debug.Log($"OnRewardExpressVideoAdLoad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnRewardExpressVideoAdLoad";

            ad.SetRewardAdInteractionListener(
                new RewardAdInteractionListener(this.example), callbackOnMainThread);
            ad.SetAgainRewardAdInteractionListener(
                    new RewardAgainAdInteractionListener(this.example), callbackOnMainThread);
            ad.SetDownloadListener(
                new AppDownloadListener(this.example), callbackOnMainThread);
            this.example.expressRewardAd = ad;
#else
#endif
        }

        public void OnRewardVideoCached()
        {
            Debug.Log($"OnExpressRewardVideoCached  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnExpressRewardVideoCached";
#if UNITY_IOS
            this.example.rewardAd.setAuctionPrice(1.0);
            this.example.rewardAd.win(1.0);
            this.example.rewardAd.Loss(1.0,"102",bidder:"ylh");
#endif
        }

        public void OnRewardVideoCached(RewardVideoAd ad)
        {
            Debug.Log($"OnRewardVideoCached RewardVideoAd ad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
#if UNITY_IOS
            ad.setAuctionPrice(1.0);
            ad.win(1.0);
            ad.Loss(1.0,"102",bidder:"ylh");
#endif
        }
    }

    /// <summary>
    /// Full screen video ad listener.
    /// </summary>
    private sealed class FullScreenVideoAdListener : IFullScreenVideoAdListener
    {
        private Example example;

        public FullScreenVideoAdListener(Example example)
        {
            this.example = example;
        }

        public void OnError(int code, string message)
        {
            Debug.LogError($"OnFullScreenError: {message}  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}" );
            if(callbackOnMainThread)
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnFullScreenError: " + message;
        }

        public void OnFullScreenVideoAdLoad(FullScreenVideoAd ad)
        {
#if UNITY_ANDROID
            var info = ad.GetMediaExtraInfo();
            Debug.Log($"OnFullScreenAdLoad info:  expireTimestamp={info["expireTimestamp"]},materialMetaIsFromPreload={info["materialMetaIsFromPreload"]}");
#endif
            Debug.Log($"OnFullScreenAdLoad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnFullScreenAdLoad";

            ad.SetFullScreenVideoAdInteractionListener(
                new FullScreenAdInteractionListener(this.example), callbackOnMainThread);
            ad.SetDownloadListener(
                new AppDownloadListener(this.example), callbackOnMainThread);

            this.example.fullScreenVideoAd = ad;
        }

        // iOS
        public void OnExpressFullScreenVideoAdLoad(ExpressFullScreenVideoAd ad)
        {
            // rewrite
        }

        public void OnFullScreenVideoCached()
        {
            Debug.Log($"OnFullScreenVideoCached  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnFullScreenVideoCached";
#if UNITY_IOS
            this.example.fullScreenVideoAd.setAuctionPrice(1.0);
            this.example.fullScreenVideoAd.win(1.0);
            this.example.fullScreenVideoAd.Loss(1.0,"102",bidder:"ylh");
#endif
        }

        public void OnFullScreenVideoCached(FullScreenVideoAd ad)
        {
            Debug.Log($"OnFullScreenVideoCached FullScreenVideoAd ad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
#if UNITY_IOS
            ad.setAuctionPrice(1.0);
            ad.win(1.0);
            ad.Loss(1.0,"102",bidder:"ylh");
#endif
            
        }
    }

    /// <summary>
    /// Full screen video ad listener.
    /// </summary>
    private sealed class ExpressFullScreenVideoAdListener : IFullScreenVideoAdListener
    {
        private Example example;

        public ExpressFullScreenVideoAdListener(Example example)
        {
            this.example = example;
        }

        public void OnError(int code, string message)
        {
            Debug.LogError("OnFullScreenError: " + message);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnFullScreenError: " + message;
        }

        public void OnFullScreenVideoAdLoad(FullScreenVideoAd ad)
        {
            Debug.Log($"OnFullScreenAdLoad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnFullScreenAdLoad";

            ad.SetFullScreenVideoAdInteractionListener(
                new FullScreenAdInteractionListener(this.example), callbackOnMainThread);
            ad.SetDownloadListener(
                new AppDownloadListener(this.example), callbackOnMainThread);

            this.example.fullScreenVideoAd = ad;
        }

        // iOS
        public void OnExpressFullScreenVideoAdLoad(ExpressFullScreenVideoAd ad)
        {
#if UNITY_IOS
            Debug.Log($"OnExpressFullScreenAdLoad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnExpressFullScreenAdLoad";

            ad.SetFullScreenVideoAdInteractionListener(
                new FullScreenAdInteractionListener(this.example), callbackOnMainThread);
            ad.SetDownloadListener(
                new AppDownloadListener(this.example), callbackOnMainThread);

            this.example.expressFullScreenVideoAd = ad;
#else
#endif
        }

        public void OnFullScreenVideoCached()
        {
            Debug.Log($"OnFullScreenVideoCached  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnFullScreenVideoCached";
#if UNITY_IOS
            this.example.fullScreenVideoAd.setAuctionPrice(1.0);
            this.example.fullScreenVideoAd.win(1.0);
            this.example.fullScreenVideoAd.Loss(1.0,"102",bidder:"ylh");
#endif
        }

        public void OnFullScreenVideoCached(FullScreenVideoAd ad)
        {
            Debug.Log($"OnFullScreenVideoCached FullScreenVideoAd ad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
#if UNITY_IOS
            ad.setAuctionPrice(1.0);
            ad.win(1.0);
            ad.Loss(1.0,"102",bidder:"ylh");
#endif
        }
    }

    private sealed class ExpressAdListener : IExpressAdListener
    {
        private Example example;
        private int type; //0:feed   1:banner  2:interstitial

        public ExpressAdListener(Example example, int type)
        {
            this.example = example;
            this.type = type;
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "ExpressAdListener";
        }

        public void OnError(int code, string message)
        {
            Debug.LogError("onExpressAdError: " + message);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "onExpressAdError";
        }

        public void OnExpressAdLoad(List<ExpressAd> ads)
        {
            Debug.LogError("OnExpressAdLoad");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnExpressAdLoad";
            IEnumerator<ExpressAd> enumerator = ads.GetEnumerator();
            if (enumerator.MoveNext())
            {
                switch (type)
                {
                    case 0:
                        this.example.mExpressFeedad = enumerator.Current;
                        this.example.mExpressFeedad.SetExpressInteractionListener(
                            new ExpressAdInteractionListener(this.example, 0), callbackOnMainThread);
                        this.example.mExpressFeedad.SetDownloadListener(
                            new AppDownloadListener(this.example), callbackOnMainThread);
                        break;
                    case 1:
                        this.example.mExpressBannerAd = enumerator.Current;
                        break;
                    case 2:
                        this.example.mExpressInterstitialAd = enumerator.Current;
                        break;
                }
            }
        }
#if UNITY_IOS

        public void OnExpressBannerAdLoad(ExpressBannerAd ad)
        {
            Debug.Log($"OnExpressBannerAdLoad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnExpressBannerAdLoad";
            ad.SetExpressInteractionListener(
                new ExpressAdInteractionListener(this.example,1), callbackOnMainThread);
            ad.SetDownloadListener(
                new AppDownloadListener(this.example), callbackOnMainThread);
            this.example.iExpressBannerAd = ad;
        }

        public void OnAdViewRenderError(int code, string message)
        {
        }

        public void OnAdViewRenderSucc(ExpressBannerAd ad)
        {
        }

        public void OnAdRemoved(ExpressBannerAd ad)
        {
        }

        public void OnAdRemoved(ExpressAd ad)
        {
        }

        public void OnRenderFailed(int code, string message)
        {
        }

        public void OnExpressInterstitialAdLoad(ExpressInterstitialAd ad)
        {
            Debug.Log($"OnExpressInterstitialAdLoad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnExpressInterstitialAdLoad";
            ad.SetExpressInteractionListener(
                new ExpressAdInteractionListener(this.example, 2), callbackOnMainThread);
            ad.SetDownloadListener(
                new AppDownloadListener(this.example), callbackOnMainThread);
            this.example.iExpressInterstitialAd = ad;
        }

        public void OnAdViewRenderSucc(ExpressInterstitialAd ad)
        {
        }

        public void OnAdRemoved(ExpressInterstitialAd ad)
        {
        }
#else
#endif
    }

    private sealed class ExpressAdInteractionListener : IExpressAdInteractionListener
    {
        private Example example;
        int type; //0:feed   1:banner  2:interstitial

        public ExpressAdInteractionListener(Example example, int type)
        {
            this.example = example;
            this.type = type;
        }

        public void OnAdClicked(ExpressAd ad)
        {
            Debug.LogError("express OnAdClicked,type:" + type);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdClicked:" + type;
        }

        public void OnAdShow(ExpressAd ad)
        {
            Debug.LogError("express OnAdShow,type:" + type);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdShow:" + type;
        }

        public void OnAdViewRenderError(ExpressAd ad, int code, string message)
        {
            Debug.LogError("express OnAdViewRenderError,type:" + type);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdViewRenderError:" + message + ":" + type;
        }

        public void OnAdViewRenderSucc(ExpressAd ad, float width, float height)
        {
            Debug.LogError("express OnAdViewRenderSucc,type:" + type);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdViewRenderSucc:" + type;
#if UNITY_IOS
            ad.setAuctionPrice(1.0);
            ad.win(1.0);
            ad.Loss(1.0,"102",bidder:"ylh");
#endif
        }

        public void OnAdClose(ExpressAd ad)
        {
            Debug.LogError("express OnAdClose,type:" + type);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdClose:" + type;
        }

        public void onAdRemoved(ExpressAd ad)
        {
            Debug.LogError("express onAdRemoved,type:" + type);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "onAdRemoved:" + type;
        }
    }

    private sealed class ExpressAdDislikeCallback : IDislikeInteractionListener
    {
        private Example example;
        int type; //0:feed   1:banner

        public ExpressAdDislikeCallback(Example example, int type)
        {
            this.example = example;
            this.type = type;
        }

        public void OnCancel()
        {
            Debug.LogError("express dislike OnCancel");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "ExpressAdDislikeCallback cancle:" + type;
        }

        public void OnShow()
        {
            Debug.LogError("express dislike OnShow");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "ExpressAdDislikeCallback OnShow:" + type;
        }

        public void OnSelected(int var1, string var2, bool enforce)
        {
            Debug.LogError("express dislike OnSelected:" + var2);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "ExpressAdDislikeCallback OnSelected:" + type;
#if UNITY_IOS

#else
            //释放广告资源
            switch (type)
            {
                case 0:
                    if (this.example.mExpressFeedad != null)
                    {
                        NativeAdManager.Instance().DestoryExpressAd(this.example.mExpressFeedad.handle);
                        this.example.mExpressFeedad = null;
                    }

                    break;
                case 1:
                    if (this.example.mExpressBannerAd != null)
                    {
                        NativeAdManager.Instance().DestoryExpressAd(this.example.mExpressBannerAd.handle);
                        this.example.mExpressBannerAd = null;
                    }

                    break;
            }
#endif
        }
    }

    private sealed class NativeAdInteractionListener : IInteractionAdInteractionListener
    {
        private Example example;
        int type; //0:feed   1:banner

        public NativeAdInteractionListener(Example example, int type)
        {
            this.example = example;
        }

        public void OnAdShow()
        {
            Debug.Log($"NativeAd show  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "NativeAd show";
        }

        public void OnAdClicked()
        {
            Debug.Log($"NativeAd click  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "NativeAd click";
        }

        public void OnAdDismiss()
        {
            Debug.Log($"NativeAd close  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "NativeAd close";

            //释放广告资源
            switch (type)
            {
                case 0:
                    this.example.feedAd.Dispose();
                    this.example.feedAd = null;
                    break;
                case 1:
                    this.example.bannerAd.Dispose();
                    this.example.bannerAd = null;
                    break;

            }
        }

        public void onAdRemoved()
        {
            Debug.Log($"NativeAd onAdRemoved  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "NativeAd onAdRemoved";
        }
    }

    private sealed class NativeAdListener : INativeAdListener
    {
        private Example example;

        public NativeAdListener(Example example)
        {
            this.example = example;
        }

        public void OnError(int code, string message)
        {
            Debug.LogError("OnNativeAdError: " + message);
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnNativeAdError: " + message;
        }

        public void OnNativeAdLoad(AndroidJavaObject list, NativeAd ad)
        {
            Debug.LogError("OnNativeAdLoad: ");
#if UNITY_IOS
            if (ad.GetAdType() == AdSlotType.Banner)
            {
                this.example.bannerAd = ad;
            } else if (ad.GetAdType() == AdSlotType.InteractionAd)
            {
                this.example.intersititialAd = ad;
            }

            ad.SetNativeAdInteractionListener(
                new NativeAdInteractionListener(this.example, (int)(ad.GetAdType())),
                callbackOnMainThread
            );
#else

            var size = list.Call<int>("size");

            if (size > 0)
            {
                this.example.mBannerAd = list.Call<AndroidJavaObject>("get", 0);
                this.example.mIntersititialAd = list.Call<AndroidJavaObject>("get", 0);
            }

#endif
            //if (ads == null && ads.[0])
            //{
            //    return;
            //}
            ////this.example.bannerAd = ads.[0];
            //this.example.bannerAd = ads.[0];
            Debug.Log($"OnNativeAdLoad  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnNativeAdLoad";


            //bannerAd.;
            //bannerAd.SetDownloadListener(
            //new AppDownloadListener(this.example));

        }
    }


    /// <summary>
    /// Full screen ad interaction listener.
    /// </summary>
    private sealed class FullScreenAdInteractionListener : IFullScreenVideoAdInteractionListener
    {
        private Example example;

        public FullScreenAdInteractionListener(Example example)
        {
            this.example = example;
        }

        public void OnAdShow()
        {
            Debug.Log($"fullScreenVideoAd show  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "fullScreenVideoAd show";
        }

        public void OnAdVideoBarClick()
        {
            Debug.Log($"fullScreenVideoAd bar click  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "fullScreenVideoAd bar click";
        }

        public void OnAdClose()
        {
            Debug.Log($"fullScreenVideoAd close  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "fullScreenVideoAd close";
            if (this.example.fullScreenVideoAd != null)
            {
                this.example.fullScreenVideoAd.Dispose();
                this.example.fullScreenVideoAd = null;
            }
#if UNITY_IOS
            if (this.example.expressFullScreenVideoAd != null) {
                this.example.expressFullScreenVideoAd.Dispose();
                this.example.expressFullScreenVideoAd = null;
            }
#endif
        }

        public void OnVideoComplete()
        {
            Debug.Log($"fullScreenVideoAd complete  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "fullScreenVideoAd complete";
        }

        public void OnVideoError()
        {
            Debug.Log($"fullScreenVideoAd OnVideoError  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "fullScreenVideoAd OnVideoError";
        }

        public void OnSkippedVideo()
        {
            Debug.Log($"fullScreenVideoAd OnSkippedVideo  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "fullScreenVideoAd skipped";

        }
    }

    private sealed class RewardAdInteractionListener : IRewardAdInteractionListener
    {
        private Example example;

        public RewardAdInteractionListener(Example example)
        {
            this.example = example;
        }

        public void OnAdShow()
        {
            Debug.Log($"rewardVideoAd show  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "rewardVideoAd show";
        }

        public void OnAdVideoBarClick()
        {
            Debug.Log($"rewardVideoAd bar click  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "rewardVideoAd bar click";
        }

        public void OnAdClose()
        {
            Debug.Log($"rewardVideoAd close  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "rewardVideoAd close";
            if (this.example.rewardAd != null)
            {
                this.example.rewardAd.Dispose();
                this.example.rewardAd = null;
            }
#if UNITY_IOS
            if (this.example.expressRewardAd != null) {
                this.example.expressRewardAd.Dispose();
                this.example.expressRewardAd = null;
            }
#endif

        }

        public void OnVideoSkip()
        {
            Debug.Log($"rewardVideoAd skip  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "rewardVideoAd skip";
        }


        public void OnVideoComplete()
        {
            Debug.Log($"rewardVideoAd complete  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "rewardVideoAd complete";
        }

        public void OnVideoError()
        {
            Debug.LogError($"rewardVideoAd error  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "rewardVideoAd error";
        }

        public void OnRewardVerify(
            bool rewardVerify, int rewardAmount, string rewardName)
        {
            Debug.Log("rewardVideoAd verify:" + rewardVerify + " amount:" + rewardAmount +
                      " name:" + rewardName + $" on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text =
                "verify:" + rewardVerify + " amount:" + rewardAmount +
                " name:" + rewardName;
        }
    }

    private sealed class RewardAgainAdInteractionListener : IRewardAdInteractionListener
    {
        private Example example;

        public RewardAgainAdInteractionListener(Example example)
        {
            this.example = example;
        }

        public void OnAdShow()
        {
            Debug.Log($"again rewardVideoAd show  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "again rewardVideoAd show";
        }

        public void OnAdVideoBarClick()
        {
            Debug.Log($"again rewardVideoAd bar click  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "again rewardVideoAd bar click";
        }

        public void OnAdClose()
        {

        }
        
        public void OnVideoSkip()
        {
            Debug.Log($"again rewardVideoAd skip  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "again rewardVideoAd skip";
        }


        public void OnVideoComplete()
        {
            Debug.Log($"again rewardVideoAd complete  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "again rewardVideoAd complete";
        }

        public void OnVideoError()
        {
            Debug.LogError($"again rewardVideoAd error  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "again rewardVideoAd error";
        }

        public void OnRewardVerify(
            bool rewardVerify, int rewardAmount, string rewardName)
        {
            Debug.Log("again rewardVideoAd verify:" + rewardVerify + " amount:" + rewardAmount +
                      " name:" + rewardName + $" on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text =
                "again verify:" + rewardVerify + " amount:" + rewardAmount +
                " name:" + rewardName;
        }
    }

    private sealed class SplashAdListener : ISplashAdListener
    {
        private Example example;
        private AndroidJavaObject activity;
        private AndroidJavaObject splashAdManager;
        private const int INTERACTION_TYPE_DOWNLOAD = 4;
        public SplashAdListener(Example example)
        {
            this.example = example;
        }

        public SplashAdListener(Example example, AndroidJavaObject activity, AndroidJavaObject splashAdManager)
        {
            this.example = example;
            this.activity = activity;
            this.splashAdManager = splashAdManager;
        }

        public void OnError(int code, string message)
        {
            Debug.Log("splash load OnError:" + code + ":" + message + $" on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnError:" + code + ":" + message;
            if (this.example.splashAd != null)
            {
                this.example.splashAd.Dispose();
                this.example.splashAd = null;
            }
        }

        public void OnSplashAdLoad(BUSplashAd ad)
        {
#if UNITY_ANDROID
                this.example.splashAd = ad;
#endif
                Debug.Log($"splash load OnSuccess:  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnLoad";
#if UNITY_IOS
                this.example.splashAd.SetSplashInteractionListener(new SplashAdInteractionListener(this.example), callbackOnMainThread);
#else
                ad.SetSplashInteractionListener(new SplashAdInteractionListener(this.example, this.activity, this.splashAdManager), callbackOnMainThread);
                if (ad.GetInteractionType() == INTERACTION_TYPE_DOWNLOAD)
                {
                    Debug.Log("splash is download type ");
                    ad.SetDownloadListener(new AppDownloadListener(this.example), callbackOnMainThread);
                }
#endif

#if UNITY_ANDROID
            if (ad != null && this.splashAdManager != null && this.activity != null)
            {
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "Splash OnLoad";
                // this.splashAdManager.Call("showSplashAd", this.activity, ad.getCurrentSplshAd()); 无法直接通过Call方法调用
                object[] obs = {activity, ad.getCurrentSplshAd()};
                var signature = "(Landroid.content.Context;Lcom.bytedance.sdk.openadsdk.TTSplashAd;)V";
                CallJavaMethod(splashAdManager,"showSplashAd",signature, obs);
            }
#endif
        }
        public void OnTimeout()
        {
            Debug.Log($"splash OnTimeout  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
        }


        private sealed class SplashAdInteractionListener : ISplashAdInteractionListener
        {
            private Example example;
            private AndroidJavaObject activity;
            private AndroidJavaObject splashAdManager;
            public SplashAdInteractionListener(Example example)
            {
                this.example = example;
            }

            public SplashAdInteractionListener(Example example, AndroidJavaObject activity, AndroidJavaObject splashAdManager)
            {
                this.example = example;
                this.activity = activity;
                this.splashAdManager = splashAdManager;
            }

            private void DestorySplash()
            {
                Debug.Log($"Destory Splash  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
#if UNITY_ANDROID
        if (splashAdManager != null && this.activity != null)
        {
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "Splash destorySplashView";
            splashAdManager.Call("destorySplashView", this.activity);
        }
#else
            this.example.splashAd.Dispose();
            this.example.splashAd = null;
#endif
            }

            /// <summary>
            /// Invoke when the Ad is clicked.
            /// </summary>
            public void OnAdClicked(int type)
            {
                Debug.Log($"splash Ad OnAdClicked type {type}   on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "Splash OnAdClicked";
#if UNITY_ANDROID
                if (type != INTERACTION_TYPE_DOWNLOAD)
                {
                    DestorySplash();
                }
#endif
            }

            /// <summary>
            /// Invoke when the Ad is shown.
            /// </summary>
            public void OnAdShow(int type)
            {
                Debug.Log($"splash Ad OnAdShow  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "Splash OnAdShow";
            }

            /// <summary>
            /// Invoke when the Ad is skipped.
            /// </summary>
            public void OnAdSkip()
            {
                Debug.Log($"splash Ad OnAdSkip  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "Splash OnAdSkip";
                DestorySplash();
            }

            /// <summary>
            /// Invoke when the Ad time over.
            /// </summary>
            public void OnAdTimeOver()
            {
                Debug.Log($"splash Ad OnAdTimeOver  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "Splash OnAdTimeOver";
                DestorySplash();
            }

            public void OnAdClose()
            {
                Debug.Log($"splash Ad OnAdClose  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
                if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdClose";
                DestorySplash();
            }
        }
    }

private sealed class ExpressSplashAdListener : ISplashAdListener
{
    private Example example;

    public ExpressSplashAdListener(Example example)
    {
        this.example = example;
    }

    public void OnError(int code, string message)
    {
        Debug.Log("expressSplash load OnError:" + code + ":" + message + $" on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
        if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnError:" + code + ":" + message;
        if (this.example.expressSplashAd != null) {
            this.example.expressSplashAd.Dispose();
            this.example.expressSplashAd = null;
        }
    }

    public void OnSplashAdLoad(BUSplashAd ad)
    {
#if UNITY_IOS
        Debug.Log($"expressSplash load Onsucc:  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
        if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnLoad";
        this.example.expressSplashAd.SetSplashInteractionListener(new ExpressSplashAdInteractionListener(this.example),callbackOnMainThread);

        this.example.expressSplashAd.setAuctionPrice(1.0);
        this.example.expressSplashAd.win(1.0);
        this.example.expressSplashAd.Loss(1.0,"102",bidder:"ylh");
#endif
    }
    
    public void OnTimeout()
    {
        Debug.Log($"splash OnTimeout  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
    }
}

private sealed class ExpressSplashAdInteractionListener : ISplashAdInteractionListener
{
    private Example example;

    public ExpressSplashAdInteractionListener(Example example)
    {
        this.example = example;
    }

        /// <summary>
        /// Invoke when the Ad is clicked.
        /// </summary>
        public void OnAdClicked(int type)
    {
        Debug.Log($"expressSplash Ad OnAdClicked  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
        if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdClicked";
        this.example.expressSplashAd.Dispose();
        this.example.expressSplashAd = null;
    }

        /// <summary>
        /// Invoke when the Ad is shown.
        /// </summary>
        public void OnAdShow(int type)
    {
        Debug.Log($"expressSplash Ad OnAdShow  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
        if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdShow";
    }

        /// <summary>
        /// Invoke when the Ad is skipped.
        /// </summary>
        public void OnAdSkip()
    {
        Debug.Log($"expressSplash Ad OnAdSkip  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
        if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdSkip";
        this.example.expressSplashAd.Dispose();
        this.example.expressSplashAd = null;
    }

        /// <summary>
        /// Invoke when the Ad time over.
        /// </summary>
        public void OnAdTimeOver()
    {
        Debug.Log($"expressSplash Ad OnAdTimeOver  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdTimeOver";
            this.example.expressSplashAd.Dispose();
            this.example.expressSplashAd = null;
        }

        public void OnAdClose()
        {
            Debug.Log($"expressSplash Ad OnAdClose  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "OnAdClose";
            this.example.expressSplashAd.Dispose();
            this.example.expressSplashAd = null;
        }

    }

    private  sealed class BUABSlotManagerListener:IBUSlotABManagerListener
    {
        public void onComplete(string slotId, AdSlotType type, int errorCode, string errorMsg)
        {
            UnityDispatcher.PostTask(() =>
            {
                Text SlotIdText = GameObject.Find("Canvas/Container/ABSlotPannel/SlotIdText").GetComponent<Text>();
                SlotIdText.text = slotId + "\n" + type;
                Debug.Log($"BUABSlotManagerListener onComplete:slotId =  {slotId} , type =  {type.ToString()}, msg: {errorMsg}");
            });
           
        }
    }



     private sealed class AppDownloadListener : IAppDownloadListener
    {
        private Example example;

        public AppDownloadListener(Example example)
        {
            this.example = example;
        }

        public void OnIdle()
        {
        }

        public void OnDownloadActive(
            long totalBytes, long currBytes, string fileName, string appName)
        {
            Debug.Log($"下载中，点击下载区域暂停  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "下载中，点击下载区域暂停";
        }

        public void OnDownloadPaused(
            long totalBytes, long currBytes, string fileName, string appName)
        {
            Debug.Log($"下载暂停，点击下载区域继续  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId} ");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "下载暂停，点击下载区域继续";
        }

        public void OnDownloadFailed(
            long totalBytes, long currBytes, string fileName, string appName)
        {
            Debug.LogError($"下载失败，点击下载区域重新下载  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "下载失败，点击下载区域重新下载";
        }

        public void OnDownloadFinished(
            long totalBytes, string fileName, string appName)
        {
            Debug.Log($"下载完成  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "下载完成";
        }

        public void OnInstalled(string fileName, string appName)
        {
            Debug.Log($"安装完成，点击下载区域打开  on main thread: {Thread.CurrentThread.ManagedThreadId == mainThreadId}");
            if(Thread.CurrentThread.ManagedThreadId == mainThreadId)
                this.example.information.text = "安装完成，点击下载区域打开";
        }
    }

     private bool splashClickByDownloadBar = false;
     private bool showDownloadConfirmDialog = false;
     public void OnClickTypeValueChanged(Toggle value)
     {
         splashClickByDownloadBar = value.isOn;
         Debug.Log($"splashClickByDownloadBar = {splashClickByDownloadBar}");
     }

     public void OnConfirmDialogValueChanged(Toggle value)
     {
         showDownloadConfirmDialog = value.isOn;
         Debug.Log($"showDownloadConfirmDialog = {showDownloadConfirmDialog}");

     }
     
     public static bool callbackOnMainThread = true;
     public void OnCallbackThreadChange(Toggle value)
     {
         callbackOnMainThread = value.isOn;
         Debug.Log($"callbackOnMainThread = {showDownloadConfirmDialog}");
     }
     


     public void OnTestClientBiddingButtonPress()
     {
         TestClientBidding(rewardAd);
         TestClientBidding(fullScreenVideoAd);
         TestClientBidding(bannerAd);
         TestClientBidding(splashAd);
         TestClientBidding(expressSplashAd);
         TestClientBidding(feedAd);
         TestClientBidding(intersititialAd);
         TestClientBidding(mExpressFeedad);
         TestClientBidding(mExpressBannerAd);
         TestClientBidding(mExpressInterstitialAd);


         TestClientBidding(mBannerAd);
         TestClientBidding(mIntersititialAd);
     }

     public void TestClientBidding(AndroidJavaObject javaObject)
     {
#if (DEV || !UNITY_EDITOR) && UNITY_ANDROID
         var values = getPrice();
         ClientBiddingUtils.Win(javaObject,values.Item1);
         ClientBiddingUtils.Loss(javaObject,values.Item2,values.Item3,values.Item4);
         ClientBiddingUtils.SetPrice(javaObject,values.Item5);
#endif
     }

     public void TestClientBidding(object clintBidding)
     {
#if (DEV || !UNITY_EDITOR) && UNITY_ANDROID
         if (clintBidding == null || !(clintBidding is IClintBidding bidding)) return;
         var values = getPrice();
         bidding.Win(values.Item1);
         bidding.Loss(values.Item2,values.Item3,values.Item4);
         bidding.SetPrice(values.Item5);
#endif
     }
     Tuple<double, double, string,string,double> getPrice()
     {
        InputField winInputText =  GameObject.Find("Canvas/Container/bidding/win").GetComponent<InputField>();
        InputField lossInputText =  GameObject.Find("Canvas/Container/bidding/loss").GetComponent<InputField>();
        InputField setPriceInputField =  GameObject.Find("Canvas/Container/bidding/setPrice").GetComponent<InputField>();
        double winPrice = double.NaN;
        var lossPrice = double.NaN;
        var setPrice = double.NaN;
        
        string lossReason =null;
        string winBidder = null;
        if (!string.IsNullOrEmpty(winInputText.text))
        {
            if (!double.TryParse(winInputText.text, out winPrice))
            {
                winPrice=double.NaN;
            }
        }
            
        var lossText = lossInputText.text;
        
        string[] lossTexts = lossText.Split(',');

        if (lossTexts.Length >= 1)
        {
            if(!string.IsNullOrEmpty(lossTexts[0]))
                if (!double.TryParse(lossTexts[0], out lossPrice))
                {
                    lossPrice=double.NaN;
                }
            if (lossTexts.Length >= 2)
            {
                lossReason = lossTexts[1];
            }

            if (lossTexts.Length >= 3)
            {
                winBidder = lossTexts[2];
            }
        }

        if (!string.IsNullOrEmpty(setPriceInputField.text))
        {
            if (!double.TryParse(setPriceInputField.text, out setPrice))
            {
                setPrice=double.NaN;
            }
        }
        return new Tuple<double, double, string, string, double>(winPrice, lossPrice, lossReason, winBidder, setPrice);
     }




#if !UNITY_IOS
     private static void CallJavaMethod(AndroidJavaObject javaObject,string methodName,string signature, object[] obs)
     {
#if UNITY_ANDROID && !UNITY_EDITOR
       var methodID =
             AndroidJNIHelper.GetMethodID(javaObject.GetRawClass(), methodName, signature);
         var jniArgArray = AndroidJNIHelper.CreateJNIArgArray(obs);
         try
         {
             AndroidJNI.CallVoidMethod(javaObject.GetRawObject(), methodID, jniArgArray);
         }
         finally
         {
             AndroidJNIHelper.DeleteJNIArgArray(obs, jniArgArray);
         }   
#endif
     }
#endif
}