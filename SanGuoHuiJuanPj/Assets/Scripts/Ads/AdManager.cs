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
    public int UnityRatio = 5;
    //public int AdmobRatio = 1;
    public int DoNewRatio = 2;
    //public int MoPubRatio = 1;
    public int IronSourceRatio = 2;
    private QueueByRatio<AdControllerBase> Queue;

    private Dictionary<Ads, (int, AdControllerBase)> Controllers
    {
        get
        {
            if (_controllers == null)
            {
                _controllers = new Dictionary<Ads, (int, AdControllerBase)>
                {
                    {Ads.Unity, (UnityRatio, UnityAdController)},
                    //{Ads.IronSource,(IronSourceRatio,IronSourceController)},
                    //{Ads.DoNew, (DoNewRatio, DoNewAdController)}
                    //{Ads.Admob, (AdmobRatio, AdmobController)},
                    //{Ads.MoPub, (MoPubRatio, MoPubController)},
                };
            }

            return _controllers;
        }
    }

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
                //DoNewAdController.RequestDirectShow(requestAction);
                //ControllersAdResolve();
                //requestAction.Invoke(false, "无广告源!");//广告控制器重复了
                return;
            } while (controller.Status != AdAgentBase.States.Loaded); //循环直到到下一个已准备的广告源
            //ControllersAdResolve();
        }
        //PlayerDataForGame.instance.ShowStringTips($"广告源:{Controllers.First(c=>c.Value.Item2 == controller).Key}");
        controller.RequestShow((success, msg) =>
        {
            requestAction(success, msg);
            //ControllersAdResolve();
        });
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