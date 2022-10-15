using System;
using System.Collections;
using Assets.Scripts.Utl;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AdAgent : AdAgentBase
{
    public Button loadButton;
    public Button showButton;
    public Button cancelButton;
    public Text message;
    public Text timer;
    public Text proceedMessage;
    public Image countdownWindow;
    public bool isAutoRequest;
    public int cancelSecs = 15;
    private bool isBusy { get; set; }
    private UnityAction<bool, string> callBackAction;
    private AdControllerBase controller;

    public override void Init(AdControllerBase adController)
    {
        controller = adController;
        instance = this;
        loadButton.gameObject.SetActive(false);
        showButton.gameObject.SetActive(false);
        //loadButton.onClick.AddListener(OnLoad);
        showButton.onClick.AddListener(OnShow);
        instance = this;
        cancelButton.onClick.AddListener(() =>
        {
            callBackAction(false, "cancel");
            OnReset();
        });
        gameObject.SetActive(false);
    }

    public override void BusyRetry(UnityAction<string> requestAction, UnityAction cancelAction)
    {
        CallAd((success,msg) =>
        {
            if (success) requestAction(msg);
            else cancelAction();
        });

    }

    public override void BusyRetry(UnityAction requestAction, UnityAction cancelAction)
    {
        CallAd((success,_) =>
        {
            if (success) requestAction();
            else cancelAction();
        });
    }

    public override void CallAd(UnityAction<bool,string> callBack)
    {
        gameObject.SetActive(true);
        callBackAction = callBack;
        //if(controller.Status != States.Loaded)
        //    OnLoad();
        //else
            OnShow();
    }

    //private void OnLoad()
    //{
    //    isBusy = true;
    //    proceedMessage.text = "请求广告中，请等待...";
    //    controller.RequestLoad((success, msg) => UnityMainThread.thread.RunNextFrame(() => OnLoadResponse(success, msg)));
    //    loadButton.gameObject.SetActive(false);
    //    StartCoroutine(StartTimer());
    //}

    private void OnShow()
    {
        isBusy = true;
        proceedMessage.text = "请求成功，请等待广告加载...";
        message.text = "加载广告!";
        controller.RequestShow((success, msg) => UnityMainThread.thread.RunNextFrame(() => OnShowResponse(success, msg)));
        showButton.gameObject.SetActive(false);
        StartCoroutine(StartTimer());
    }

    private IEnumerator StartTimer()
    {
        countdownWindow.gameObject.SetActive(true);
        var secs = 0;
        while (isBusy)
        {
            timer.text = secs.ToString();
            if (secs >= cancelSecs)
            {
                OnShowResponse(false,"time out");
                yield return null;
            }
            yield return new WaitForSeconds(1);
            secs++;
        }
        countdownWindow.gameObject.SetActive(false);
    }

    private void OnReset()
    {
        loadButton.gameObject.SetActive(false);
        showButton.gameObject.SetActive(false);
        message.text = string.Empty;
        proceedMessage.text = string.Empty;
        isBusy = false;
        gameObject.SetActive(false);
        StopAllCoroutines();
    }

    private void OnLoadResponse(bool isLoaded,string msg)
    {
        isBusy = false;
        if (!isLoaded)
        {
            message.text = "暂时没有广告提供噢！\n"+msg;
            showButton.gameObject.SetActive(false);
            loadButton.gameObject.SetActive(true);
            return;
        }
        loadButton.gameObject.SetActive(false);
        showButton.gameObject.SetActive(true);
        if (!isAutoRequest) return;
        StopAllCoroutines();
        OnShow();
    }

    private void OnShowResponse(bool success, string msg)
    {
        isBusy = false;
        countdownWindow.gameObject.SetActive(false);
        if (!success)
        {
            StopAllCoroutines();
            message.text = $"广告加载失败!\n重试？({msg})";
            showButton.gameObject.SetActive(false);
            loadButton.gameObject.SetActive(true);
            return;
        }

        message.text = "请求成功！";
        callBackAction(true, msg);
        OnReset();
    }
}