using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ByteDance.Union;
using UnityEngine;
using UnityEngine.Events;

public class PangleAdController : AdControllerBase
{
    private AdAgentBase.States _status = AdAgentBase.States.None;
    private AdNative _adNative;
    private RewardVideoAd rewardAd;
    private const string AndroidSlotID = "948109196";

    public override string StatusDetail => $"{Status}[{_waitingIndex}]:";
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

    private static void callbackmethod(bool success, string message) => Debug.Log("`````````````````初始化``````" + success + "-----" + message);

    public override void RequestShow(UnityAction<bool, string> requestAction)
    {
        _status = AdAgentBase.States.None;
        if (rewardAd == null)
        {
            LoadDirectRewardAd(requestAction);
            return;
        }

        var listener = new RewardAdInteractionListener();
        listener.CallbackAction = requestAction;
        rewardAd.SetRewardAdInteractionListener(listener);
        rewardAd.ShowRewardVideoAd();
    }

    public void LoadDirectRewardAd(UnityAction<bool, string> requestAction)
    {
        if (rewardAd != null)
        {
            rewardAd.Dispose();
            rewardAd = null;
        }

        var adSlot = new AdSlot.Builder()
            .SetCodeId(AndroidSlotID)
            .SetSupportDeepLink(true)
            .SetImageAcceptedSize(1080, 1920)
            .SetUserID(GamePref.Username) // 用户id,必传参数
            //.SetMediaExtra("media_extra") // 附加参数，可选
            .SetOrientation(AdOrientation.Vertical) // 必填参数，期望视频的播放方向
            .SetAdLoadType(AdLoadType.Load)
            .Build();

        var directCallback = new RewardVideoCallback();
        directCallback.CallbackAction += CallbackAction;
        AdNative.LoadRewardVideoAd(adSlot, directCallback);

        void CallbackAction(RewardVideoAd ad, string message)
        {
            if(ad==null)
            {
                requestAction?.Invoke(false, "无广告源.");
                return;
            }
            var listener = new RewardAdInteractionListener();
            listener.CallbackAction += requestAction;
            ad.SetRewardAdInteractionListener(listener);
            ad?.ShowRewardVideoAd();
        }
    }

    private class RewardAdInteractionListener : IRewardAdInteractionListener
    {
        public UnityAction<bool,string> CallbackAction;
        private bool isInvokeCallback = false;
        /// <summary>
        /// 广告是否成功!
        /// </summary>
        private bool isAdValid = false;
        public void OnAdShow() {}

        public void OnAdVideoBarClick() {}

        public void OnAdClose() => OnVerify();

        public void OnVideoComplete() => OnVerify();

        public void OnVideoSkip() => OnVerify();

        public void OnVideoError() => OnCallBack(false, "广告视频异常。");

        public void OnRewardVerify(bool rewardVerify, int rewardAmount, string rewardName, int rewardType = -1,
            float rewardPropose = -1) => isAdValid = rewardVerify;

        public void OnRewardArrived(bool isRewardValid, int rewardType, IRewardBundleModel extraInfo) =>
            isAdValid = isRewardValid;

        private void OnVerify()
        {
            var message = isAdValid ? string.Empty : "广告无效。";
            OnCallBack(isAdValid, message);
        }

        private void OnCallBack(bool success,string message)
        {
            if (isInvokeCallback) return;//避免重复执行
            isInvokeCallback = true;
            var action = CallbackAction;
            CallbackAction = null;
            action?.Invoke(success, message);
        }
    }

    private class RewardVideoCallback : IRewardVideoAdListener
    {
        public Action<RewardVideoAd, string> CallbackAction;
        
        public void OnError(int code, string message) => CallbackAction?.Invoke(null, $"({code}):{message}");

        public void OnRewardVideoAdLoad(RewardVideoAd ad) => CallbackAction?.Invoke(ad, string.Empty);

        public void OnRewardVideoCached()
        {
            
        }

        public void OnExpressRewardVideoAdLoad(ExpressRewardVideoAd ad)
        {
            
        }

        public void OnRewardVideoCached(RewardVideoAd ad)
        {
        }
    }

    public override void RequestLoad(UnityAction<bool, string> loadingAction)
    {
        if (rewardAd != null && _status != AdAgentBase.States.Loading)
        {
            rewardAd.Dispose();
            rewardAd = null;
        }

        _status = AdAgentBase.States.Loading;


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
            .SetAdLoadType(AdLoadType.PreLoad)
            .Build();
        var adListener = new RewardVideoCallback();
        adListener.CallbackAction += CallbackAction;
        AdNative.LoadRewardVideoAd(adSlot, adListener);

        void CallbackAction(RewardVideoAd ad, string msg)
        {
            rewardAd = ad;
            _status = rewardAd == null ? AdAgentBase.States.FailedToLoad : AdAgentBase.States.Loaded;
            loadingAction?.Invoke(ad != null, msg);
        }
    }

    private int[] waitingSecs = new[] { 1, 3, 5, 10, 15 };
    private int _waitingIndex = 0;
    /// <summary>
    /// 当AdController请求loading的时候，反馈的行动
    /// </summary>
    /// <param name="success"></param>
    /// <param name="message"></param>
    public async void OnRequestLoadResult(bool success, string message)
    {
        if (success)
        {
            _status = AdAgentBase.States.Loaded;
            _waitingIndex = 0;
            return;
        }

        if (_waitingIndex >= waitingSecs.Length)
        {
            _status = AdAgentBase.States.FailedToLoad;
            _waitingIndex = 0;
            return;
        }

        await Task.Delay(waitingSecs[_waitingIndex] * 1000);
        if (_status == AdAgentBase.States.Loading || 
            _status == AdAgentBase.States.Loaded) return;

        _status = AdAgentBase.States.Loading;

        RequestLoad(OnRequestLoadResult);
        _waitingIndex++;
    }

}