using System;
using UnityEngine.Advertisements;
using UnityEngine.Events;

public class UnityAdController : AdControllerBase
{
    private AdAgentBase.States status;
    public override string StatusDetail => Status.ToString();

    public override AdAgentBase.States Status => status;
    private const string GameId = "3997035";
    private const string PlacementId = "Android_Rewarded";
#if UNITY_EDITOR
    private bool isDevTest = true;
#else
    private bool isDevTest = false;
#endif
    private UnityAction<bool, string> recallAction;

    public void Init()
    {
#if !UNITY_EDITOR
        Advertisement.Initialize(GameId, isDevTest);
        LoadUnityAd(null);
#endif
    }

    public override void RequestShow(UnityAction<bool, string> requestAction)
    {
        recallAction = requestAction;
        if (status == AdAgentBase.States.Loaded)
        {
            Advertisement.Show(PlacementId);
            return;
        }
        OnActionRecallOnce(false, "Unity Ad Not ready!");
    }

    private void OnActionRecallOnce(bool isSuccess, string msg)
    {
        recallAction?.Invoke(isSuccess, msg);
        recallAction = null;
    }

    public override void RequestLoad(UnityAction<bool, string> loadingAction)
    {
        if (status is not (AdAgentBase.States.Loading or AdAgentBase.States.Loaded))
        {
            LoadUnityAd(loadingAction);
            return;
        }
        loadingAction?.Invoke(true, string.Empty);
    }

    private void LoadUnityAd(UnityAction<bool, string> loadingAction)
    {
        Advertisement.Load(PlacementId, new UnityAdLoadListener(CallbackAction));

        void CallbackAction(bool isSuccess, string message)
        {
            status = isSuccess ? AdAgentBase.States.Loaded : AdAgentBase.States.FailedToLoad;
            loadingAction?.Invoke(isSuccess, message);
        }
    }

    public void OnUnityAdsAdLoaded(string placementId) => status = AdAgentBase.States.Loaded;
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message) =>
        status = AdAgentBase.States.FailedToLoad;
    private class UnityAdLoadListener : IUnityAdsLoadListener
    {
        private UnityAction<bool, string> OnloadAction { get; }

        public UnityAdLoadListener(UnityAction<bool,string> onloadAction)
        {
            OnloadAction = onloadAction;
        }
        public void OnUnityAdsAdLoaded(string placementId)=> OnloadAction?.Invoke(true, string.Empty);

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message) =>
            OnloadAction?.Invoke(false, message);
    }
}