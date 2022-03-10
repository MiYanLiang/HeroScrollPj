using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ByteDance.Union;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 广告源控制器
/// </summary>
public class AdManager : AdControllerBase
{
    [Serializable]public enum Ads
    {
        Unity,
        Pangle
        //DoNew,
        //IronSource,
        //Admob,
        //MoPub,
    }

    public AdAgentBase adAgent;
    private bool isInit;
    public AdAgentBase AdAgent => adAgent;
    //public DoNewAdController DoNewAdController { get; private set; }
    //public AdmobController AdmobController { get; private set; }
    public UnityAdController UnityAdController { get; private set; }
    //public MoPubController MoPubController { get; private set; }
    //public IronSourceController IronSourceController { get; private set; }
    public override AdAgentBase.States Status => AdAgentBase.States.Loaded;
    [Header("广告播放顺序")]public Ads[] Series;
    [Header("广告源比率")]
    public int UnityRatio = 1;
    public int PangleRatio = 2;
    //public int AdmobRatio = 1;
    //public int DoNewRatio = 2;
    //public int MoPubRatio = 1;
    //public int IronSourceRatio = 2;
    private QueueByRatio<AdControllerBase> Queue;

    private Dictionary<Ads, (int, AdControllerBase)> Controllers
    {
        get
        {
            if (_controllers == null)
            {
                _controllers = new Dictionary<Ads, (int, AdControllerBase)>
                {
                    { Ads.Unity, (UnityRatio, UnityAdController) },
                    { Ads.Pangle, (PangleRatio, PangleController) }
                    //{Ads.IronSource,(IronSourceRatio,IronSourceController)},
                    //{Ads.DoNew, (DoNewRatio, DoNewAdController)}
                    //{Ads.Admob, (AdmobRatio, AdmobController)},
                    //{Ads.MoPub, (MoPubRatio, MoPubController)},
                };
            }

            return _controllers;
        }
    }

    public PangleAdController PangleController { get; set; }

    private Dictionary<Ads, (int,AdControllerBase)> _controllers;

#if UNITY_EDITOR
    public EditorAdEmu adEmu;
#endif

    public void Init()
    {
        if (isInit) throw XDebug.Throw<AdManager>("Duplicate init!");
        //if (AdAgentBase.instance != null) return;
        isInit = true;
        //DoNewAdController = gameObject.AddComponent<DoNewAdController>();
        //DoNewAdController.Init();
        //IronSourceController = gameObject.AddComponent<IronSourceController>();
        //IronSourceController.Init();
        //AdmobController = gameObject.AddComponent<AdmobController>();
        //AdmobController.Init(AdmobRetryCallBack);
        UnityAdController = gameObject.AddComponent<UnityAdController>();
        UnityAdController.Init();
        //MoPubController = gameObject.AddComponent<MoPubController>();
        //MoPubController.Init();
        PangleController = gameObject.AddComponent<PangleAdController>();
        PangleController.Init();
        Queue = new QueueByRatio<AdControllerBase>(
            Series.Join(Controllers,ad=>ad,c=>c.Key,(_,c)=>c.Value).ToArray()
        );
        AdAgent?.Init(this);
#if !UNITY_EDITOR
        StartCoroutine(NextSecondRequestCache());
#endif
    }

    private IEnumerator NextSecondRequestCache()
    {
        yield return new WaitForSeconds(1);
        //ControllersAdResolve();
    }

    public override void RequestShow(UnityAction<bool, string> requestAction)
    {
#if UNITY_EDITOR
        adEmu.Set(requestAction);
        if(adEmu!=null)return;
#endif
        //admobRetryCount = 0;
        var controller = Queue.Dequeue();
        var count = 0;
        if (controller.Status != AdAgentBase.States.Loaded)
        {
            do
            {
                controller = Queue.Dequeue();
                count++;
                if (count < 10) continue;//如果广告控制器未重复会一直找
                PangleController.LoadExpressRewardAd();
                //DoNewAdController.RequestDirectShow(requestAction);
                //ControllersAdResolve();
                //requestAction.Invoke(false, "无广告源!");//广告控制器重复了
                return;
            } while (controller.Status != AdAgentBase.States.Loaded); //循环直到到下一个已准备的广告源
            //ControllersAdResolve();
        }
        //PlayerDataForGame.instance.ShowStringTips($"广告源:{Controllers.First(c=>c.Value.Item2 == controller).Key}");
        controller.RequestShow(requestAction);
    }

    public override void RequestLoad(UnityAction<bool, string> loadingAction) => loadingAction(true, string.Empty);

    //private void ControllersAdResolve()
    //{
    //    if (DoNewAdController.Status == AdAgentBase.States.Closed ||
    //        DoNewAdController.Status == AdAgentBase.States.FailedToLoad ||
    //        DoNewAdController.Status == AdAgentBase.States.None) DoNewAdController.RequestLoad(null);
    //    // if(AdmobController.Status == AdAgentBase.States.Closed ||
    //    //    AdmobController.Status == AdAgentBase.States.FailedToLoad ||
    //    //    AdmobController.Status == AdAgentBase.States.None)
    //    //     AdmobController.OnLoadAd(AdmobRetryCallBack);
    //    // if(MoPubController.Status == AdAgentBase.States.Closed ||
    //    //    MoPubController.Status == AdAgentBase.States.FailedToLoad ||
    //    //    MoPubController.Status == AdAgentBase.States.None) MoPubController.RequestLoad(null);
    //}
}

public class PangleAdController : AdControllerBase, IRewardVideoAdListener
{
    private AdAgentBase.States _status = AdAgentBase.States.None;
    private AdNative _adNative;
    private RewardVideoAd rewardAd;
    private const string AndroidSlotID = "948109196";
    
    public override AdAgentBase.States Status => _status;
    private AdNative AdNative
    {
        get
        {
            if (_adNative == null) _adNative = SDK.CreateAdNative();
#if UNITY_ANDROID
            SDK.RequestPermissionIfNecessary();
#endif
            return _adNative;
        }
    }

    public void Init() => Pangle.InitializeSDK(callbackmethod);

    private static void callbackmethod(bool success, string message) =>
        Debug.Log("`````````````````初始化``````" + success + "-----" + message);

    public override void RequestShow(UnityAction<bool, string> requestAction)
    {
        LoadRewardAd();
    }
    /// <summary>
    /// Load the reward Ad.
    /// </summary>
    private void LoadRewardAd()
    {
        if (rewardAd == null)
        {
            LoadExpressRewardAd();
            return;
        }
        rewardAd.ShowRewardVideoAd();
    }

    public void LoadExpressRewardAd()
    {
#if UNITY_IOS
        if (this.expressRewardAd != null)
        {
            this.expressRewardAd.Dispose();
            this.expressRewardAd = null;
        }
#else
        if (rewardAd != null)
        {
            rewardAd.Dispose();
            rewardAd = null;
        }
#endif


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
            .SetUserID(GamePref.Username) // 用户id,必传参数
            //.SetMediaExtra("media_extra") // 附加参数，可选
            .SetOrientation(AdOrientation.Vertical) // 必填参数，期望视频的播放方向
#if UNITY_ANDROID
            .SetDownloadType(DownloadType.DownloadTypeNoPopup)
#endif
            .Build();
#if UNITY_IOS
        this.AdNative.LoadExpressRewardAd(
            adSlot, new ExpressRewardVideoAdListener(this), callbackOnMainThread);
#else
        AdNative.LoadRewardVideoAd(adSlot, this);
#endif
    }

    public override void RequestLoad(UnityAction<bool, string> loadingAction)
    {
        if (rewardAd != null)
        {
            _status = AdAgentBase.States.Loading;
            rewardAd.Dispose();
            rewardAd = null;
        }

        
        var adSlot = new AdSlot.Builder()
#if UNITY_IOS
            .SetCodeId(iosSlotID)
#else
            .SetCodeId(AndroidSlotID)
#endif
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(1080, 1920)
            .SetUserID(GamePref.Username) // 用户id,必传参数
            //.SetMediaExtra("media_extra") // 附加参数，可选
            .SetOrientation(AdOrientation.Vertical) // 必填参数，期望视频的播放方向
#if UNITY_ANDROID
            .SetDownloadType(
                //showDownloadConfirmDialog?
                //    DownloadType.DownloadTypePopup :
                DownloadType.DownloadTypeNoPopup)
#endif
            .Build();

        AdNative.LoadRewardVideoAd(adSlot, this);
    }

    public void OnError(int code, string message) => _status = AdAgentBase.States.FailedToLoad;

    public void OnRewardVideoAdLoad(RewardVideoAd ad) => _status = AdAgentBase.States.Loaded;

    public void OnRewardVideoCached() => _status = AdAgentBase.States.Loaded;

    public void OnExpressRewardVideoAdLoad(ExpressRewardVideoAd ad)
    {
        
    }

    public void OnRewardVideoCached(RewardVideoAd ad)
    {
        _status = AdAgentBase.States.Loaded;
    }
}

/// <summary>
/// 根据比率排队类
/// </summary>
/// <typeparam name="T"></typeparam>
public class QueueByRatio<T>
{
    private Dictionary<T, int> data;
    private Queue<T> queue;
    public int Count => queue.Count;
    public T Current { get; private set; }

    public QueueByRatio(params (int, T)[] controllers)
    {
        data = controllers.ToDictionary(c => c.Item2, c => c.Item1);
        queue = new Queue<T>(controllers.Select(c => c.Item2));
    }

    private List<T> SetQueue()
    {
        var max = data.Values.Max();
        var list = new List<T>();
        for (var i = 0; i < max; i++)
            list.AddRange(data.Where(item => item.Value > i).Select(item => item.Key));
        return list;
    }

    public T Dequeue(bool forceChange = false)
    {
        if (queue.Count == 0)
            queue = new Queue<T>(SetQueue());
        Current = queue.Dequeue();
        return Current;
    }
}