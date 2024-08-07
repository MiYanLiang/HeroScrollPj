﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Utl;
using BestHTTP.JSON.LitJson;
using BestHTTP.PlatformSupport.Memory;
using BestHTTP.SignalRCore;
using CorrelateLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using static Assets.System.WarModule.RespondAct;
using ConnectionStates = BestHTTP.SignalRCore.ConnectionStates;
using Json = CorrelateLib.Json;

/// <summary>
/// Signal客户端
/// </summary>
public class SignalRClient : MonoBehaviour
{
    public ServerPanel ServerPanel;
    
    public static SignalRClient instance;

    private Dictionary<string, UnityAction<string>> _actions;
    public ApiPanel ApiPanel;
    public string LoginToken { get; set; }
    public int Zone { get; private set; } = -1;
    [SerializeField] private int _signalRRequestRetries = 5;
    [SerializeField] private GameObject _signalRRequestPanel;
    private RetryCaller _retryCaller = new RetryCaller();

    private void DisplayPanel(bool display) => _signalRRequestPanel.SetActive(display);
    private SignalRClientConnection SignalRClientConnection { get; set; }
    public ConnectionStates? Status => SignalRClientConnection?.Status;

    public void Init()
    {
        instance = this;
        //Login();
        _actions = new Dictionary<string, UnityAction<string>>();
        if (SignalRClientConnection == null)
        {
            SignalRClientConnection = new SignalRClientConnection();
            SignalRClientConnection.OnServerCall += OnServerCall;
            SignalRClientConnection.OnRequestError += () => PlayerDataForGame.instance.ShowStringTips("请求失败，请确保网络稳定。重试中...");
        }

        if (ServerPanel != null) ServerPanel.Init(this);
        ApiPanel.Init(this);
    }

    void OnApplicationQuit()
    {
        if (!IsLogged) return;
        SignalRClientConnection.CloseConnection(null);
    }

    void OnApplicationFocus(bool isFocus)
    {
        if (!IsLogged || !isFocus) return;
        switch (SignalRClientConnection.Status)
        {
            case ConnectionStates.Connected:
            case ConnectionStates.Reconnecting:
                return;
            default:
                ReconnectServerWithTips();
                break;
        }
    }

    public async Task<SigninResult> NegoToken(int zone, int createNew)
    {
        try
        {
            Zone = zone;
            var content =
                Json.Serialize(new TokenLoginModel(LoginToken, zone, float.Parse(Application.version), createNew));
            var res = await Http.PostAsync(Server.TokenLogin, content, true);
            return new SigninResult(res.isSuccess, res.code, res.data);
        }
        catch (Exception)
        {
            return new SigninResult(SigninResult.SignInStates.Failed, 999, "登录失败，请检查网络或重新登录。", string.Empty);
        }
    }

    public async void TokenLogin(SignalRConnectionInfo connectionInfo,Action<bool> callbackAction)
    {
        if (connectionInfo == null)
        {
            callbackAction?.Invoke(false);
            return;
        }
#if UNITY_EDITOR
        //测试用, 直接连接到本地服务器
        if (customSignalRServer)
        {
            var res = await Http.PostAsync(_connInfo.Url + "api/TestServerSignIn", Json.Serialize(new[]
            {
                connectionInfo.Username
            }), true);
            var infoText = res.data;
            connectionInfo = Json.Deserialize<SignalRConnectionInfo>(infoText);
        }
#endif
        var isSuccess = await SignalRClientConnection.ConnectSignalRAsync(connectionInfo);
        if (isSuccess)
            _connInfo = connectionInfo;
        callbackAction?.Invoke(isSuccess);
    }
#if UNITY_EDITOR
    [SerializeField]private bool customSignalRServer;
    private  Action<bool> loginCallbackAction;
    
#endif
    [SerializeField]private SignalRConnectionInfo _connInfo;

    public void SynchronizeSaved(UnityAction onCompleteAction)
    {
        var cancelToken = new CancellationTokenSource();
        //var jData = await HubRequestByViewBag(EventStrings.Req_Saved);
        Invoke(EventStrings.Req_Saved, jData =>
        {
            var bag = Json.Deserialize<ViewBag>(jData);
            if (bag == null)
            {
                Failed();
                return;
            }

            var playerData = bag.GetPlayerDataDto();
            var character = bag.GetPlayerCharacterDto();
            var warChestList = bag.GetPlayerWarChests();
            var redeemedList = bag.GetPlayerRedeemedCodes();
            var warCampaignList = bag.GetPlayerWarCampaignDtos();
            var gameCardList = bag.GetPlayerGameCardDtos();
            var troops = bag.GetPlayerTroopDtos();
            PlayerDataForGame.instance.SetDto(playerData, character, warChestList, redeemedList, warCampaignList,
                gameCardList, troops);
            onCompleteAction?.Invoke();
        }, ViewBag.Instance(), cancelToken);

        void Failed()
        {
            PlayerDataForGame.instance.ShowStringTips("登录超时，请重新登录游戏。");
        }
    }

    public void Disconnect(UnityAction callbackAction) => SignalRClientConnection.CloseConnection(callbackAction);

    public async Task<bool> ReconnectServerWithTips()
    {
        var success = await SignalRClientConnection.HubReconnectTask();
        IsLogged = success;
        var msg = success ? "重连成功！" : "重连失败，请重新登录。";
        PlayerDataForGame.instance.ShowStringTips(msg);
        return success;
    }

    private bool IsLogged { get; set; }

    public void SubscribeAction(string method, UnityAction<string> action)
    {
        if (!_actions.ContainsKey(method))
            _actions.Add(method, default);
        _actions[method] += action;
    }

    public void UnSubscribeAction(string method, UnityAction<string> action)
    {
        if (!_actions.ContainsKey(method))
            throw XDebug.Throw<SignalRClient>($"Method[{method}] not registered!");
        _actions[method] -= action;
    }

#if UNITY_EDITOR
    public bool RejectServer;
#endif
    private void OnServerCall(string method, string content)
    {
#if UNITY_EDITOR
        DebugLog($"服务器请求{method}: {content}");
        if (RejectServer)
        {
            DebugLog($"已拒绝服务器调用：{method}: {content}");
            return;
        }
#endif
        if (!_actions.TryGetValue(method, out var action)) return;
        action?.Invoke(content);
    }

    private void Invoke(string method, UnityAction<string> recallAction, IViewBag bag = default,
        CancellationTokenSource tokenSource = default) =>
        SetRetryAwaitOnMainThread(() => HubRequestByViewBag(method, bag, tokenSource), recallAction, method);

    private async void SetRetryAwaitOnMainThread(Func<Task<string>> call, UnityAction<string> successResult,string methodName)
    {
        await _retryCaller.RetryAwait(call
            , result => UnityMainThread.thread.RunNextFrame(() => successResult(result)),
            ReconnectRetry);
        return;

        async Task<bool> ReconnectRetry(string message)
        {
            var retry = await RetryPanel.AwaitRetryAsync(methodName);
            //注意这里返回false，表示不再重试
            if (!retry || SignalRClientConnection.Status == ConnectionStates.Reconnecting) return false;
            //中间安插一个重连服务器的操作再重试
            var isSuccess = await ReconnectServerWithTips();
            if (isSuccess) return true;
            PlayerDataForGame.instance.ShowStringTips("服务器已失联...");
            return true;
        }
    }

    public void Invoke(string method, UnityAction<string> recallAction, string serializedBag,
        CancellationTokenSource tokenSource = default) =>
        SetRetryAwaitOnMainThread(() => HubRequestByDataBag(method, serializedBag, tokenSource), recallAction, method);

    public void HttpInvoke(string method, string serializedBag,Action<string> callback,
        CancellationTokenSource tokenSource = default)
    {
        SetRetryAwaitOnMainThread(HttpRequestAsync, m => callback(m), method);
        return;

        async Task<string> HttpRequestAsync()
        {
            var response = await Http.SendAsync(HttpMethod.Post,
                $"{Server.ApiServer}{method}", serializedBag,
                new (string, string)[]
                {
                    ("bearer", _connInfo.AccessToken)
                });
            var text = response.data;
            return text;
        }
    }

    //public void InvokeCaller(string method, UnityAction<string> recallAction, string serializedBag,
    //    CancellationTokenSource tokenSource = default)
    //{
    //    SetRetryAwaitOnMainThread(InvokeRequest, recallAction, method);
    //    return;

    //    async Task<string> InvokeRequest()
    //    {
    //        CallerGuid = Guid.NewGuid();
    //        var result = await HubRequestByCallerBag(method, serializedBag, CallerGuid.ToString(), tokenSource);
    //        return result;
    //    }
    //}
    private static Guid CallerGuid { get; set; }

    private readonly TimeSpan _requestTimeOut = TimeSpan.FromMinutes(1);

    private async Task<string> HubRequestByViewBag(string method, IViewBag bag = default,
        CancellationTokenSource tokenSource = default)
    {
        DisplayPanel(true);
        if (bag == default) bag = ViewBag.Instance();
        if (tokenSource == null) tokenSource = new CancellationTokenSource(_requestTimeOut);
        var result = await SignalRClientConnection.HubInvokeAsync<string>(method, tokenSource.Token,
            bag == null ? Array.Empty<object>() : new object[] { Json.Serialize(bag) });
        DisplayPanel(false);
        return result;
    }

    private async Task<string> HubRequestByDataBag(string method, string serialized,
        CancellationTokenSource tokenSource = default)
    {
        DisplayPanel(true);
        if (tokenSource == null) tokenSource = new CancellationTokenSource(_requestTimeOut);
        var result = await SignalRClientConnection.HubInvokeAsync<string>(method, tokenSource.Token,
            string.IsNullOrWhiteSpace(serialized) ? Array.Empty<object>() : new object[] { serialized });
        DisplayPanel(false);
        return result;
    }
    private async Task<string> HubRequestByCallerBag(string method, string serialized,string guid,
        CancellationTokenSource tokenSource = default)
    {
        DisplayPanel(true);
        if (tokenSource == null) tokenSource = new CancellationTokenSource(_requestTimeOut);
        var result = await SignalRClientConnection.HubInvokeAsync<string>(method, tokenSource.Token,
            string.IsNullOrWhiteSpace(serialized) ? Array.Empty<object>() : new object[] { serialized, guid });
        DisplayPanel(false);
        return result;
    }

    #region DebugLog

    private string DebugMsg(string message) => $"SignalR客户端: {message}";

    private void DebugLog(string message)
    {
#if DEBUG
        XDebug.Log<SignalRClient>(DebugMsg(message));
#endif
    }

    #endregion

    public class SigninResult
    {
        public enum SignInStates
        {
            Success,
            NoContent,
            Failed
        }

        public SignInStates State { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public string Content { get; set; }

        public SigninResult(SignInStates state, int code, string message, string content)
        {
            State = state;
            Code = code;
            Message = message;
            Content = content;
        }

        public SigninResult(HttpResponseMessage response, string content)
        {
            if (!response.IsSuccess())
            {
                State = SignInStates.Failed;
                Message = response.StatusCode == HttpStatusCode.HttpVersionNotSupported
                    ? "服务器维护中..."
                    : "登录失败。";
            }
            else if (response.StatusCode == HttpStatusCode.NoContent)
            {
                State = SignInStates.NoContent;
                Message = "该区并无数据，\n是否创建新角色";
            }
            else
            {
                State = SignInStates.Success;
                Message = "登录成功!";
            }

            Code = (int)response.StatusCode;
            Content = content;
        }

        public SigninResult(bool isSuccess, HttpStatusCode code, string data)
        {
            if (!isSuccess)
            {
                State = SignInStates.Failed;
                Message = code == HttpStatusCode.HttpVersionNotSupported
                    ? "服务器维护中..."
                    : "登录失败。";
            }
            else if (code == HttpStatusCode.NoContent)
            {
                State = SignInStates.NoContent;
                Message = "该区并无数据，\n是否创建新角色";
            }
            else
            {
                State = SignInStates.Success;
                Message = "登录成功!";
            }

            Code = (int)code;
            Content = data;
        }
    }

    private class TokenLoginModel
    {
        public string Token { get; set; }
        public int Zone { get; set; }
        public float GameVersion { get; set; }
        public int New { get; set; }

        public TokenLoginModel(string token, int zone, float gameVersion, int createNew)
        {
            Token = token;
            Zone = zone;
            GameVersion = gameVersion;
            New = createNew;
        }
    }
}

[Serializable]
public class SignalRConnectionInfo
{
    public string Url;
    public string AccessToken;
    public int Arrangement;
    public int IsNewRegistered;
    public string Username;

    public SignalRConnectionInfo()
    {
        
    }
    public SignalRConnectionInfo(string url, string accessToken, string username, int arrangement,
        int isNewRegistered)
    {
        Url = url;
        AccessToken = accessToken;
        Arrangement = arrangement;
        IsNewRegistered = isNewRegistered;
        Username = username;
    }
}

public class JsonNetEncoder : IEncoder
{
    private Type StringType = typeof(string);
    private Type ObjectArrayType = typeof(object[]);
    public JsonNetEncoder()
    {
        JsonMapper.RegisterImporter<int, long>(input => input);
        JsonMapper.RegisterImporter<long, int>(input => (int)input);
        JsonMapper.RegisterImporter<double, int>(input => (int)(input + 0.5));
        JsonMapper.RegisterImporter<string, DateTime>(input => Convert.ToDateTime(input).ToUniversalTime());
        JsonMapper.RegisterImporter<double, float>(input => (float)input);
        JsonMapper.RegisterImporter<string, byte[]>(Convert.FromBase64String);
        JsonMapper.RegisterExporter<float>((f, writer) => writer.Write((double)f));
    }
    public BufferSegment Encode<T>(T value)
    {
        var json = Json.Serialize(value);
        int len = Encoding.UTF8.GetByteCount(json);
        byte[] buffer = BufferPool.Get(len + 1, true);
        Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 0);
        buffer[len] = (byte)JsonProtocol.Separator;
        return new BufferSegment(buffer, 0, len + 1);
    }

    public T DecodeAs<T>(BufferSegment buffer)
    {
        var text = Encoding.UTF8.GetString(buffer.Data, buffer.Offset, buffer.Count);
        return JsonConvert.DeserializeObject<T>(text);
    }

    public object ConvertTo(Type toType, object obj)
    {
        var token = JToken.FromObject(obj);
        Debug.Log($"转码：{toType.Name}\n{obj}");
        if (toType.IsPrimitive || toType == StringType)
            return ((JValue)token).Value;
        if (toType==ObjectArrayType)
            return Json.DeserializeList<object>(token.ToString());
        return JsonConvert.DeserializeObject(token.ToString(), toType);
    }
}