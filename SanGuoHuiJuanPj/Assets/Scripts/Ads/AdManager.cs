using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    }

    public AdAgentBase adAgent;
    private bool isInit;
    public bool IsInit => isInit;

    public Ads Current => PangleController.Status == AdAgentBase.States.Loaded ? Ads.Pangle : Ads.Unity;

    public AdAgentBase AdAgent => adAgent;
    //public DoNewAdController DoNewAdController { get; private set; }
    //public AdmobController AdmobController { get; private set; }
    public UnityAdController UnityAdController { get; private set; }
    //public MoPubController MoPubController { get; private set; }
    //public IronSourceController IronSourceController { get; private set; }
    public override string StatusDetail => Status.ToString();

    public override AdAgentBase.States Status => AdAgentBase.States.Loaded;
    //[Header("广告播放顺序")]public Ads[] Series;
    [Header("广告源比率")] [SerializeField] private AdField[] AdFields;
    //public int UnityRatio = 1;
    //public int PangleRatio = 2;

    [Serializable]private class AdField
    {
        public Ads Ad;
        public int Ratio;
    }
    private AdControllerBase InstanceAdControllerType(Ads fieldAd)
    {
        switch (fieldAd)
        {
            case Ads.Unity:
            {
                var ad = gameObject.AddComponent<UnityAdController>();
                UnityAdController = ad;
                ad.Name = Ads.Unity.ToString();
                ad.Init();
                return ad;
            }
            case Ads.Pangle:
            {
                var ad = gameObject.AddComponent<PangleAdController>();
                PangleController = ad;
                ad.Name = Ads.Pangle.ToString();
                ad.Init();
                return ad;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(fieldAd), fieldAd, null);
        }
    }

    public PangleAdController PangleController { get; set; }

#if UNITY_EDITOR
    public EditorAdEmu adEmu;
#endif

    public void Init()
    {
        if (isInit) return;
        //if (AdAgentBase.instance != null) return;
        isInit = true;
        InstanceAdControllerType(Ads.Pangle);
        InstanceAdControllerType(Ads.Unity);
        AdAgent?.Init(this);
#if !UNITY_EDITOR
        StartCoroutine(NextSecondRequestCache());
#endif
    }

    private IEnumerator NextSecondRequestCache()
    {
        yield return new WaitForSeconds(1);
        ControllersAdResolve();
    }

    public override void RequestShow(UnityAction<bool, string> requestAction)
    {
#if UNITY_EDITOR
        adEmu.Set(requestAction);
        if (adEmu != null) return;
#endif

        AdControllerBase controller = PangleController;
        if (controller.Status == AdAgentBase.States.Loaded)
            controller.RequestShow(PangleRequestCallback);
        else PangleController.LoadDirectRewardAd(PangleRequestCallback);


        void PangleRequestCallback(bool success, string msg)
        {
            if (success)
            {
                requestAction?.Invoke(true, string.Empty);
                ControllersAdResolve();
                return;
            }

            controller = UnityAdController;
            controller.RequestShow((s, m) =>
            {
                requestAction?.Invoke(s, m);
                ControllersAdResolve();
            });
        }
    }


    public override void RequestLoad(UnityAction<bool, string> loadingAction) => loadingAction(true, string.Empty);

    private void ControllersAdResolve()
    {
        PangleResolve(PangleController);
    }

    private void PangleResolve(AdControllerBase controller)
    {
        //var adController = (PangleAdController)controller;
        //if (adController.Status == AdAgentBase.States.None ||
        //    adController.Status == AdAgentBase.States.Closed ||
        //    adController.Status == AdAgentBase.States.FailedToLoad)
        //    adController.RequestLoad(adController.OnRequestLoadResult);
    }
}
