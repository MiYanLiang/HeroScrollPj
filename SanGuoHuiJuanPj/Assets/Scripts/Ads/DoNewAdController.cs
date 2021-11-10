//using System;
//using System.Collections;
//using System.Threading;
//using System.Threading.Tasks;
//using Beebyte.Obfuscator;
//using Donews.mediation;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.UI;

//public class DoNewAdController : AdControllerBase
//{
//    private static DoNewAdController instance;

//    public override AdAgentBase.States Status => status;
//    public AdAgentBase.States status;

//    public AdModes Mode => mode;
//    public AdModes mode;
//    public bool isEditableActionSuccess;//仅用于unity编辑器

//    public int Timer { get; private set; }
//    public UnityEvent OnCached = new UnityEvent();
//    private bool isCachedTriggered;

//    public const string UnityPlayer = "com.unity3d.player.UnityPlayer";
//    public const string CurrentActivity = "currentActivity";
//    public const string RequestRewardVideo = "RequestRewardVideo";
//    public const string RunOnUiThread = "runOnUiThread";
//    public const string PlaceId = "5294";

//    private RewardedVideoAd cachedVideoAd;
//    private RewardAdListener rewardAdListener;

//    public void Init()
//    {
//        SDK.apiType = SDK.ApiType.Release;
//        status = AdAgentBase.States.None;
//    }

//    public override void RequestLoad(UnityAction<bool,string> requestAction)
//    {
//#if !UNITY_EDITOR
//        status = AdAgentBase.States.Loading;
//        rewardAdListener = new RewardAdListener((isSuccess,msg) =>
//        {
//            status = isSuccess ? AdAgentBase.States.Loaded : AdAgentBase.States.FailedToLoad;
//            requestAction?.Invoke(isSuccess,msg);
//        });
//        cachedVideoAd = RewardedVideoAd.LoadRewardedVideoAd(PlaceId,rewardAdListener);
//#endif
//        //requestAction?.Invoke(true, string.Empty);//直接播放，不需要load
//    }

//    public override void RequestShow(UnityAction<bool,string> onSuccessAction)
//    {
//        try
//        {
//            //DirectPlayRewardVideoAd.RequestAd(onSuccessAction);
//            rewardAdListener.onShowAction = onSuccessAction;
//            cachedVideoAd.ShowRewardedVideoAd();
//            status = AdAgentBase.States.Closed;
//        }
//        catch (Exception e)
//        {
//            onSuccessAction(false, e.ToString());
//            PlayerDataForGame.instance.ShowStringTips("视频加载失败!");
//        }
//    }

//    public void RequestDirectShow(UnityAction<bool, string> onCallBackAction)
//    {
//        try
//        {
//            DirectPlayRewardVideoAd.RequestAd(onCallBackAction);
//        }
//        catch (Exception e)
//        {
//            onCallBackAction(false, e.ToString());
//            PlayerDataForGame.instance.ShowStringTips("视频加载失败!");
//        }

//    }

//    private AndroidJavaObject currentActivity;

//    public class RewardAdListener : IRewardedVideoAdListener
//    {
//        public RewardedVideoAd videoAd;

//        public UnityAction<bool,string> onShowAction;
//        private UnityAction<bool,string> onLoadAction;
//        public RewardAdListener(UnityAction<bool,string> onLoad)
//        {
//            onLoadAction = onLoad;
//        }

//        // 广告数据加载成功回调
//        public void RewardVideoAdDidLoadSuccess(){}

//        // 视频广告播放达到激励条件回调
//        public void RewardVideoAdDidRewardEffective() => onShowAction.Invoke(true,"奖励条件已达。");
//        // 视频广告视频播放完成
//        public void RewardVideoAdDidPlayFinish() { }
//        // 视频广告曝光回调
//        public void RewardVideoAdExposured() { }

//        // 视频广告各种错误信息回调
//        public void RewardVideoAdDidLoadFaild(int errorCode, string errorMsg) =>
//            onShowAction.Invoke(false, $"{errorCode}:{errorMsg}");
//        // 视频数据下载成功回调，已经下载过的视频会直接回调
//        public void RewardVideoAdVideoDidLoad() => onLoadAction?.Invoke(true, "成功缓存广告资源！");
//        // 视频播放页即将展示回调
//        public void RewardVideoAdWillVisible(){}
//        // 视频播放页关闭回调
//        public void RewardVideoAdDidClose(){}
//        // 视频广告信息点击回调
//        public void RewardVideoAdDidClicked(){}
//    }
//}