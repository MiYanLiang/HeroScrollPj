using System;
using UnityEngine.Advertisements;
using UnityEngine.Events;

public class UnityAdController : AdControllerBase, IUnityAdsListener
{
    private AdAgentBase.States status;
    public override string StatusDetail => Status.ToString();

    public override AdAgentBase.States Status
    {
        get
        {
            status = Advertisement.IsReady(PlacementId) ? AdAgentBase.States.Loaded : AdAgentBase.States.None;
            return status;
        }
    }
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
#endif
        Advertisement.AddListener(this);
    }

    //Unity暂时无法(回调)知道当前是否已经把广告预备好。只能每次都检查是否已经可以播放广告。
    public bool IsReady() => Advertisement.IsReady(PlacementId);

    public override void RequestShow(UnityAction<bool, string> requestAction)
    {
        recallAction = requestAction;
        if (Advertisement.IsReady(PlacementId))
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
        if (!Advertisement.IsReady(PlacementId))
        {
            Advertisement.Load(PlacementId);
            return;
        }
        loadingAction?.Invoke(true, string.Empty);
    }

    public void OnUnityAdsReady(string placementId) { }

    public void OnUnityAdsDidError(string message) => XDebug.Log<UnityAdController>(message);

    public void OnUnityAdsDidStart(string placementId) { }

    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
        switch (showResult)
        {
            case ShowResult.Failed:
                OnActionRecallOnce(false, "Failed!");
                break;
            case ShowResult.Skipped:
                OnActionRecallOnce(false, "玩家跳过广告。");
                break;
            case ShowResult.Finished:
                OnActionRecallOnce(true, "请求成功。");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(showResult), showResult, null);
        }
    }

}