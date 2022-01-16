using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Utl;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;

public class ApiPanel : MonoBehaviour
{
    public static ApiPanel instance;
    public static bool IsBusy { get; private set; }
    private SignalRClient Client { get; set; }
#if UNITY_EDITOR
    public bool isSkipApi;
#endif
    // Start is called before the first frame update
    public void Init(SignalRClient client)
    {
        if (instance != null && instance != this)
            throw new InvalidOperationException();
        instance = this;
        Client = client;
        Client.SubscribeAction(EventStrings.SC_ReLogin, args => ClientReconnect());
        SetBusy(false);
    }


    public void InvokeVb(UnityAction<ViewBag> successAction, UnityAction<string> failedAction, string method,
        IViewBag bag = default) =>
        InvokeVb(successAction, failedAction, method, bag, true);

    public void InvokeVb(UnityAction<ViewBag> successAction, UnityAction<string> failedAction, string method,
        IViewBag bag,bool closeBusyAfterInvoke)
    {
        SetBusy(this);
#if UNITY_EDITOR
        if (isSkipApi)
        {
            successAction.Invoke(new ViewBag());
            return;
        }
#endif
        Client.Invoke(method, result =>
        {
            var viewBag = Json.Deserialize<ViewBag>(result);
            if (viewBag == null) failedAction?.Invoke(result);
            else successAction.Invoke(viewBag);
            if(closeBusyAfterInvoke) SetBusy(false);
        }, bag);
    }

    public void InvokeRk(UnityAction<DataBag> successAction, UnityAction<string> failedAction, string method,
        params object[] args) =>
        InvokeBag(successAction, failedAction, true, EventStrings.Req_Rk, method, args);

    public void InvokeBag(UnityAction<DataBag> successAction, UnityAction<string> failedAction, string controller,
        string method, params object[] args) =>
        InvokeBag(successAction, failedAction, true, controller, method, args);

    public void InvokeBag(UnityAction<DataBag> successAction, UnityAction<string> failedAction, bool closeBusyAfterInvoke, string controller,string method,
        params object[] args)
    {
        SetBusy(this);
#if UNITY_EDITOR
        if (isSkipApi)
        {
            successAction.Invoke(new DataBag());
            return;
        }
#endif
        var bag = DataBag.SerializeBag(method, args);
        Client.Invoke(controller, result =>
        {
            var viewBag = Json.Deserialize<DataBag>(result);
            if (viewBag == null) failedAction?.Invoke(result);
            else successAction.Invoke(viewBag);
            if(closeBusyAfterInvoke) SetBusy(false);
        }, bag);
    }

    public async void SyncSaved(UnityAction onCompleteAction)
    {
        SetBusy(this);
        await Client.SynchronizeSaved();
        onCompleteAction?.Invoke();
        SetBusy(false);
    }

    public void SetBusy(bool busy)
    {
        UnityMainThread.thread.RunNextFrame(() =>
        {
            gameObject.SetActive(busy);
            IsBusy = busy;
        });
    }

    private void ClientReconnect()
    {
        StopAllCoroutines();
        StartCoroutine(DelayedReLogin());
    }

    private IEnumerator DelayedReLogin()
    {
        yield return new WaitForSeconds(1.5f);
        var isDeviceLogin = GamePref.ClientLoginMethod == 1;
        if (isDeviceLogin)
        {
            Client.Disconnect(() =>
            {
                Client.DirectLogin(CallBack);
            });
        }else Client.Disconnect(() =>
        {
            Client.UserLogin(CallBack, GamePref.Username, GamePref.Password);
        });
    }

    private void CallBack(bool success, int code, SignalRClient.SignalRConnectionInfo info)
    {
#if UNITY_EDITOR
        XDebug.Log<ServerPanel>(
            $"Success = {success}, Code = {code}, user: {info.Username}, Token = {info.AccessToken}");
#endif
        SetBusy(false);
    }
}
