﻿using System;
using System.Collections;
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
#if UNITY_EDITOR
    public static string TestUserQuery(string username) =>
        $"username={username}&clientVersion={Application.version}&ServiceZone=-1&RUserId=test";
#endif
    public void CallTest(string username,UnityAction<DataBag> successAction, UnityAction<string> failedAction, string method,
        params object[] args)
    {
#if !UNITY_EDITOR
        SetBusy(true);
        var queryString = TestUserQuery(username);
        var jsonBag = DataBag.SerializeBag(method, args);
        Http.Post($"{Server.ApiServer}{EventStrings.Req_Call}Test?{queryString}", jsonBag, text =>
        {
            var bag = DataBag.DeserializeBag(text);
            SetBusy(false);
            if (bag != null)
                successAction(bag);
            else
                failedAction(text);
        }, method);
#else
        Call(successAction, failedAction, method, args);
#endif
    }

    public void Call(UnityAction<DataBag> successAction, UnityAction<string> failedAction, string method,
        params object[] args) =>
        InvokeCallerBag(successAction, failedAction, true, controller: EventStrings.Req_Call, method: method, args);
    public void InvokeRk(UnityAction<DataBag> successAction, UnityAction<string> failedAction, string method,
        params object[] args) =>
        InvokeBag(successAction, failedAction, true, controller: EventStrings.Req_Rk, method: method, args);

    public void InvokeBag(UnityAction<DataBag> successAction, UnityAction<string> failedAction, string controller,
        string method, params object[] args) =>
        InvokeBag(successAction, failedAction, true, controller, method, args);
    public void InvokeCallerBag(UnityAction<DataBag> successAction, UnityAction<string> failedAction, bool closeBusyAfterInvoke, string controller, string method,
        object[] args)
    {
        SetBusy(this);
#if UNITY_EDITOR
        if (isSkipApi)
        {
            successAction.Invoke(new DataBag());
            return;
        }
#endif
        var bag = new ClientDataBag(method, args);
        //bag.DataName = method;
        //bag.Data = args;
        //bag.Size = args.Length;
        var serializeBag = Json.Serialize(bag); //DataBag.SerializeBag(method, args);
        Client.InvokeCaller(controller, result =>
        {
            var dataBag = DataBag.DeserializeBag(result);
            if (!dataBag.IsValid()) failedAction?.Invoke(result);
            else successAction.Invoke(dataBag);
            if (closeBusyAfterInvoke) SetBusy(false);
        }, serializeBag);

    }

    public void InvokeBag(UnityAction<DataBag> successAction, UnityAction<string> failedAction, bool closeBusyAfterInvoke, string controller,string method,
        object[] args)
    {
        SetBusy(this);
#if UNITY_EDITOR
        if (isSkipApi)
        {
            successAction.Invoke(new DataBag());
            return;
        }
#endif
        var bag = new ClientDataBag(method, args);
        //bag.DataName = method;
        //bag.Data = args;
        //bag.Size = args.Length;
        var serializeBag = Json.Serialize(bag); //DataBag.SerializeBag(method, args);
        Client.Invoke(controller, result =>
        {
            var dataBag = DataBag.DeserializeBag(result);
            if (!dataBag.IsValid()) failedAction?.Invoke(result);
            else successAction.Invoke(dataBag);
            if(closeBusyAfterInvoke) SetBusy(false);
        }, serializeBag);

    }
    private class ClientDataBag : IDataBag
    {
        public string DataName { get; set; }
        public object[] Data { get; set; }
        public int Size { get; set; }

        public ClientDataBag(string dataName, params object[] data)
        {
            DataName = dataName;
            Data = data;
            Size = data.Length;
        }
    }

    public void SyncSaved(UnityAction onCompleteAction)
    {
        SetBusy(this);
        Client.SynchronizeSaved(() =>
        {
            onCompleteAction?.Invoke();
            SetBusy(false);
        });
    }

    public void SetBusy(bool busy)
    {
        UnityMainThread.thread.RunNextFrame(() =>
        {
            IsBusy = busy;
            gameObject.SetActive(busy);
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
        Client.ReconnectServer(CallBack);
    }

    private void CallBack(bool isSuccess)
    {
#if UNITY_EDITOR
        XDebug.Log<ServerPanel>(
            $"Connection success = {isSuccess}");
#endif
        SetBusy(false);
    }
}
