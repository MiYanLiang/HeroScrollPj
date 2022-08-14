using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Utl;
using Beebyte.Obfuscator;
using BestHTTP.Examples;
using BestHTTP.JSON.LitJson;
using BestHTTP.PlatformSupport.Memory;
using BestHTTP.SignalR;
using BestHTTP.SignalR.Hubs;
using BestHTTP.SignalRCore;
using BestHTTP.SignalRCore.Authentication;
using BestHTTP.SignalRCore.Encoders;
using CorrelateLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Events;
using UnityEngine.UIElements;
using ConnectionStates = BestHTTP.SignalRCore.ConnectionStates;
using Json = CorrelateLib.Json;

[Skip]
/// <summary>
/// Signal客户端
/// </summary>
public class SignalRClient : MonoBehaviour
{
    public ServerPanel ServerPanel;
    
    public static SignalRClient instance;

    public event UnityAction<ConnectionStates,string> OnStatusChanged;
    private Dictionary<string, UnityAction<string>> _actions;
    private bool isBusy;
    public ApiPanel ApiPanel;
    public string LoginToken { get; set; }
    public int Zone { get; private set; } = -1;
    [SerializeField] private int _signalRRequestRetries = 5;
    [SerializeField] private GameObject _signalRRequestPanel;

    private void DisplayPanel(bool display) => _signalRRequestPanel.SetActive(display);
    private SignalRClientConnection SignalRClientConnection { get; set; }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //Login();
        _actions = new Dictionary<string, UnityAction<string>>();
        if (SignalRClientConnection == null)
        {
            SignalRClientConnection = new SignalRClientConnection(_signalRRequestRetries);
            SignalRClientConnection.OnStatusChanged += OnStatusChange;
            SignalRClientConnection.OnStatusChanged += OnStatusChanged;
            SignalRClientConnection.OnServerCall += OnServerCall;
            SignalRClientConnection.OnRequestError += () => PlayerDataForGame.instance.ShowStringTips("请求失败，请确保网络稳定。重试中...");
        }

        if (ServerPanel != null) ServerPanel.Init(this);
        ApiPanel.Init(this);
    }

    private void OnStatusChange(ConnectionStates status, string msg)
    {
#if UNITY_EDITOR
        DebugLog($"链接状态更变：{status}\n{msg}");
#endif
        ApiPanel.SetBusy(status != ConnectionStates.Connected);
    }


    void OnApplicationQuit()
    {
        if (!IsLogged) return;
        SignalRClientConnection.CloseConnection(null);
    }

    void OnApplicationFocus(bool isFocus)
    {
        if (!IsLogged || !isFocus) return;
        if (SignalRClientConnection.Status != ConnectionStates.Connected) ReconnectServer();
    }

    public async Task<SigninResult> NegoToken(int zone, int createNew)
    {
        try
        {
            Zone = zone;
            var content =
                Json.Serialize(new TokenLoginModel(LoginToken, zone, float.Parse(Application.version), createNew));
            var response = await Http.PostAsync(Server.TokenLogin, content);
            return new SigninResult(response, await response.Content.ReadAsStringAsync());
        }
        catch (Exception)
        {
            return new SigninResult(SigninResult.SignInStates.Failed, 999, "登录失败，请检查网络或重新登录。", string.Empty);
        }
    }

    public async Task<bool> TokenLogin(SignalRConnectionInfo connectionInfo)
    {
        isBusy = true;
        if (connectionInfo == null)
        {
            isBusy = false;
            return false;
        }

        var result = await SignalRClientConnection.ConnectSignalRAsync(connectionInfo);
        isBusy = false;
        return result;
    }

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
            PlayerDataForGame.instance.pyData = PlayerData.Instance(playerData);
            PlayerDataForGame.instance.UpdateCharacter(character);
            PlayerDataForGame.instance.GenerateLocalStamina();
            PlayerDataForGame.instance.warsData.warUnlockSaveData = warCampaignList.Select(w => new UnlockWarCount
            {
                warId = w.WarId,
                isTakeReward = w.IsFirstRewardTaken,
                unLockCount = w.UnlockProgress
            }).ToList();
            PlayerDataForGame.instance.UpdateGameCards(troops, gameCardList);
            PlayerDataForGame.instance.gbocData.redemptionCodeGotList = redeemedList.ToList();
            PlayerDataForGame.instance.gbocData.fightBoxs = warChestList.ToList();
            PlayerDataForGame.instance.isNeedSaveData = true;
            LoadSaveData.instance.SaveGameData();
            onCompleteAction?.Invoke();
        }, ViewBag.Instance(), cancelToken);

        void Failed()
        {
            PlayerDataForGame.instance.ShowStringTips("登录超时，请重新登录游戏。");
        }
    }

    public void Disconnect(UnityAction callbackAction) => SignalRClientConnection.CloseConnection(callbackAction);

    public async void ReconnectServer(Action<bool> callBackAction = null)
    {
        if (isBusy) return;
        isBusy = true;
        var success = await SignalRClientConnection.HubReconnectTask();
        callBackAction?.Invoke(success);
        IsLogged = success;
        var msg = success ? "重连成功！" : "重连失败，请重新登录。";
        PlayerDataForGame.instance.ShowStringTips(msg);
        isBusy = false;
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

    public void Invoke(string method, UnityAction<string> recallAction, IViewBag bag = default,
        CancellationTokenSource tokenSource = default)
    {
        if (SignalRClientConnection.Status == ConnectionStates.Connected)
        {
            InvokeRequest();
            return;
        }

        ReconnectServer(isSuccess =>
        {
            if (isSuccess)
            {
                InvokeRequest();
                return;
            }

            throw new InvalidOperationException("登录异常，请重新登录！");
        });

        async void InvokeRequest()
        {
            var result = await HubRequestByViewBag(method, bag, tokenSource);
            UnityMainThread.thread.RunNextFrame(() => recallAction?.Invoke(result));
        }
    }


    public void Invoke(string method, UnityAction<string> recallAction, string serializedBag,
        CancellationTokenSource tokenSource = default)
    {
        if (SignalRClientConnection.Status == ConnectionStates.Connected)
        {
            InvokeRequest();
            return;
        }

        ReconnectServer(isSuccess =>
        {
            if (isSuccess)
            {
                InvokeRequest();
                return;
            }
            throw new InvalidOperationException("登录异常，请重新登录！");
        });

        async void InvokeRequest()
        {
            var result = await HubRequestByDataBag(method, serializedBag, tokenSource);
            UnityMainThread.thread.RunNextFrame(() => recallAction?.Invoke(result));
        }
    }
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

public class SignalRConnectionInfo
{
    public string Url { get; set; }
    public string AccessToken { get; set; }
    public int Arrangement { get; set; }
    public int IsNewRegistered { get; set; }
    public string Username { get; set; }

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