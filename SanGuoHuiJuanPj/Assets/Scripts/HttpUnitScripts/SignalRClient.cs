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
using BestHTTP.SignalRCore.Messages;
using CorrelateLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Events;
using ConnectionStates = BestHTTP.SignalRCore.ConnectionStates;
using Json = CorrelateLib.Json;
using TransportTypes = BestHTTP.SignalRCore.TransportTypes;

[Skip]
/// <summary>
/// Signal客户端
/// </summary>
public class SignalRClient : MonoBehaviour
{
    /// <summary>
    /// SignalR 网络状态
    /// </summary>
    public ConnectionStates Status;

    public int ServerTimeOutMinutes = 10;
    public int KeeAliveIntervalSecs = 600;
    public int HandShakeTimeoutSecs = 10;
    public ServerPanel ServerPanel;
    public event UnityAction<ConnectionStates> OnStatusChanged;
    public event UnityAction OnConnected;
    public static SignalRClient instance;
    private CancellationTokenSource cancellationTokenSource;

    private static HubConnection _hub;
    private Dictionary<string, UnityAction<string>> _actions;
    private static readonly Type _stringType = typeof(string);
    private bool isBusy;
    public ApiPanel ApiPanel;
    public string LoginToken { get; set; }
    public int Zone { get; private set; } = -1;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //Login();
        _actions = new Dictionary<string, UnityAction<string>>();
#if UNITY_EDITOR
        OnStatusChanged += msg => DebugLog($"链接状态更变：{msg}");
#endif
        if (ServerPanel != null) ServerPanel.Init(this);
        ApiPanel.Init(this);
    }


    async void OnApplicationQuit()
    {
        if (_hub == null) return;
        await _hub.CloseAsync();
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
        cancellationTokenSource = new CancellationTokenSource();
        if (connectionInfo == null)
        {
            isBusy = false;
            return false;
        }

        var result = await ConnectSignalRAsync(connectionInfo, cancellationTokenSource.Token);
        isBusy = false;
        return result;
    }

    public async Task SynchronizeSaved()
    {
        var jData = await InvokeVb(EventStrings.Req_Saved);
        var bag = Json.Deserialize<ViewBag>(jData);
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
    }

    private async Task<bool> ConnectSignalRAsync(SignalRConnectionInfo connectionInfo,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_hub != null)
            {
                _hub.OnClosed -= OnConnectionClose;
                _hub.OnReconnected -= OnReconnected;
                _hub.OnReconnecting -= OnReconnecting;
                Application.quitting -= OnDisconnect;
                _hub = null;
            }

            _hub = InstanceHub(connectionInfo.Url, connectionInfo.AccessToken, cancellationToken);
            await OnHubStartAsync(cancellationToken);
            Application.quitting += OnDisconnect;
        }
        catch (Exception e)
        {
            StatusChanged(_hub?.State ?? ConnectionStates.Closed, $"连接失败！{e}");
            return false;
        }

        return true;
    }

    private async Task OnHubStartAsync(CancellationToken cancellationToken)
    {
        if (_hub.State == ConnectionStates.Connected) throw new InvalidOperationException("Hub is connected!");
        await _hub.ConnectAsync();
        StatusChanged(_hub.State, "SignalRHost:连接成功！");
        cancellationTokenSource = null;
    }

    private HubConnection InstanceHub(string url, string token, CancellationToken cancellationToken = default)
    {
        var conn = new HubConnection(new Uri(url), new JsonProtocol(new JsonNetEncoder()), new HubOptions
        {
            ConnectTimeout = TimeSpan.FromMinutes(ServerTimeOutMinutes),
            PingInterval = TimeSpan.FromMinutes(1),
            SkipNegotiation = true,
            PreferedTransport = TransportTypes.WebSocket
        });
        conn.CustomNegotiationResult(NegotiationResult.Instance(url, token));
        conn.AuthenticationProvider = new AzureSignalRServiceAuthenticator(conn, token);
        cancellationToken.Register(() => conn.CloseAsync());
        conn.OnClosed += OnConnectionClose;
        conn.OnReconnected += OnReconnected;
        conn.OnReconnecting += OnReconnecting;
        conn.On<string, string>(EventStrings.ServerCall, OnServerCall);
        return conn;
    }

    private void OnDisconnect() => Disconnect();

    public async void ReconnectServer(Action<bool> callBackAction = null)
    {
        if (isBusy) return;
        isBusy = true;
        await HubReconnectTask(callBackAction);
        isBusy = false;
    }

    private async Task HubReconnectTask(Action<bool> callBackAction)
    {
        if (_hub != null && _hub.State != ConnectionStates.Closed)
            await _hub.CloseAsync();

        try
        {
            isBusy = true;
            var isSuccess = await RetryConnectToServer();
            isBusy = false;
            callBackAction?.Invoke(isSuccess);
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            DebugLog(e.ToString());
#endif
            PlayerDataForGame.instance.ShowStringTips("服务器尝试链接失败，请联系管理员！");
        }

        async Task<bool> RetryConnectToServer()
        {
            var result = await NegoToken(Zone, 0);
            if (result.State != SigninResult.SignInStates.Success)
            {
                ReloginResult(false, result.Code);
                return false;
            }

            var connectionInfo = Json.Deserialize<SignalRConnectionInfo>(result.Content);
            if (connectionInfo == null)
            {
                ReloginResult(false, -1);
                return false;
            }

            var isSuccess = await TokenLogin(connectionInfo);
            ReloginResult(isSuccess, 100);
            return isSuccess;
        }

        void ReloginResult(bool success, int code)
        {
            var msg = success ? "重连成功！" : $"连接失败,错误码：{code}\n";
            PlayerDataForGame.instance.ShowStringTips(msg);
        }
    }

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
        if (_hub.State == ConnectionStates.Connected)
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
            var result = await InvokeVb(method, bag, tokenSource);
            UnityMainThread.thread.RunNextFrame(() => recallAction?.Invoke(result));
        }
    }


    public void Invoke(string method, UnityAction<string> recallAction, string serializedBag,
        CancellationTokenSource tokenSource = default)
    {
        if (_hub.State == ConnectionStates.Connected)
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
            var result = await InvokeBag(method, serializedBag, tokenSource);
            UnityMainThread.thread.RunNextFrame(() => recallAction?.Invoke(result));
        }

    }

    private async Task<string> InvokeVb(string method, IViewBag bag = default,
        CancellationTokenSource tokenSource = default)
    {
        try
        {
            if (bag == default) bag = ViewBag.Instance();
            if (tokenSource == null) tokenSource = new CancellationTokenSource();
            var result = await _hub.InvokeAsync<string>(method, tokenSource.Token,
                bag == null ? Array.Empty<object>() : new object[] { Json.Serialize(bag) });
            return result;
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            XDebug.LogError<SignalRClient>($"Error on interpretation {method}:{e.Message}");
#endif
            GameSystem.ServerRequestException(method, e.Message);
            if (!tokenSource.IsCancellationRequested) tokenSource.Cancel();
            return $"请求服务器异常: {e}";
        }
    }

    private async Task<string> InvokeBag(string method, string serialized,
        CancellationTokenSource tokenSource = default)
    {
        try
        {
            if (tokenSource == null) tokenSource = new CancellationTokenSource();
            var result = await _hub.InvokeAsync<string>(method, tokenSource.Token,
                string.IsNullOrWhiteSpace(serialized) ? Array.Empty<object>() : new object[] { serialized });
            return result;
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            XDebug.LogError<SignalRClient>($"Error on interpretation {method}:{e.Message}");
#endif
            GameSystem.ServerRequestException(method, e.Message);
            if (!tokenSource.IsCancellationRequested) tokenSource.Cancel();
            return $"请求服务器异常: {e}";
        }
    }

    /// <summary>
    /// 强制离线
    /// </summary>
    public async void Disconnect(UnityAction onActionDone = null)
    {
        if (_hub.State == ConnectionStates.Closed || _hub.State == ConnectionStates.CloseInitiated)
            return;

        if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested == false)
        {
            cancellationTokenSource.Cancel();
            return;
        }

        if (_hub.State == ConnectionStates.Closed || _hub.State == ConnectionStates.CloseInitiated) return;
        await _hub.CloseAsync();
        onActionDone?.Invoke();
    }

    #region Upload

    private async void OnServerCalledUpload(string args)
    {
        var param = Json.DeserializeList<ViewBag>(args);
        var saved = PlayerDataForGame.instance;
        var playerData = saved.pyData;
        var warChest = saved.gbocData.fightBoxs.ToArray();
        var redeemedCodes = new string[0]; //saved.gbocData.redemptionCodeGotList.ToArray();
        var token = param[0];
        var campaign = saved.warsData.warUnlockSaveData.Select(w => new WarCampaignDto
                { WarId = w.warId, IsFirstRewardTaken = w.isTakeReward, UnlockProgress = w.unLockCount })
            .Where(w => w.UnlockProgress > 0).ToArray();
        var cards = saved.hstData.heroSaveData
            .Join(DataTable.Hero, c => c.id, h => h.Key, (c, h) => new { ForceId = h.Value.ForceTableId, c })
            .Concat(saved.hstData.towerSaveData.Join(DataTable.Tower, c => c.id, t => t.Key,
                (c, t) => new { t.Value.ForceId, c })).Concat(saved.hstData.trapSaveData.Join(DataTable.Trap, c => c.id,
                t => t.Key, (c, t) => new { t.Value.ForceId, c })).Where(c => c.c.chips > 0 || c.c.level > 0).ToList();
        var troops = cards.GroupBy(c => c.ForceId, c => c).Select(c =>
        {
            var list = c.GroupBy(o => o.c.typeIndex, o => o.c)
                .ToDictionary(o => (GameCardType)o.Key, o => o.Select(a => a).ToArray());
            return new TroopDto
            {
                ForceId = c.Key,
                Cards = list.ToDictionary(l => l.Key, l => l.Value.Select(o => o.id).ToArray()),
                EnList = list.ToDictionary(l => l.Key,
                    l => l.Value.Where(o => o.isFight > 0).Select(o => o.id).ToArray())
            };
        }).ToArray();
        var viewBag = ViewBag.Instance()
            .SetValue(token)
            .PlayerDataDto(playerData.ToDto())
            .PlayerRedeemedCodes(redeemedCodes)
            .PlayerWarChests(warChest)
            .PlayerWarCampaignDtos(campaign)
            .PlayerGameCardDtos(cards.Select(c => c.c.ToDto()).ToArray())
            .PlayerTroopDtos(troops);
        await InvokeVb(EventStrings.Req_UploadPy, viewBag);
    }

    #endregion

    #region Event

    /// <summary>
    /// 当客户端尝试重新连接服务器
    /// </summary>
    private void OnReconnecting(HubConnection hub, string message) => StatusChanged(hub.State, message);

    /// <summary>
    /// 当客户端重新连线
    /// </summary>
    private void OnReconnected(HubConnection hub) => StatusChanged(hub.State, hub.State.ToString());

    /// <summary>
    /// 当客户端断线的处理方法
    /// </summary>
    private void OnConnectionClose(HubConnection hub) => StatusChanged(hub.State, hub.State.ToString());

    private void StatusChanged(ConnectionStates status, string message)
    {
        ApiPanel.SetBusy(status != ConnectionStates.Connected);
        Status = status;
        OnStatusChanged?.Invoke(status);
        DebugLog(message);
    }

    #endregion

    #region DebugLog

    private string DebugMsg(string message) => $"SignalR客户端: {message}";

    private void DebugLog(string message)
    {
#if DEBUG
        XDebug.Log<SignalRClient>(DebugMsg(message));
#endif
    }

    #endregion

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

    private sealed class AzureSignalRServiceAuthenticator : IAuthenticationProvider
    {
        public string AccessToken { get; set; }

        /// <summary>
        /// No pre-auth step required for this type of authentication
        /// </summary>
        public bool IsPreAuthRequired
        {
            get { return false; }
        }

#pragma warning disable 0067
        /// <summary>
        /// Not used event as IsPreAuthRequired is false
        /// </summary>
        public event OnAuthenticationSuccededDelegate OnAuthenticationSucceded;

        /// <summary>
        /// Not used event as IsPreAuthRequired is false
        /// </summary>
        public event OnAuthenticationFailedDelegate OnAuthenticationFailed;

#pragma warning restore 0067

        private HubConnection _connection;

        public AzureSignalRServiceAuthenticator(HubConnection connection, string accessToken)
        {
            this._connection = connection;
            AccessToken = accessToken;
        }

        /// <summary>
        /// Not used as IsPreAuthRequired is false
        /// </summary>
        public void StartAuthentication()
        {
        }

        /// <summary>
        /// Prepares the request by adding two headers to it
        /// </summary>
        public void PrepareRequest(BestHTTP.HTTPRequest request)
        {
            if (this._connection.NegotiationResult == null)
                return;

            // Add Authorization header to http requests, add access_token param to the uri otherwise
            if (BestHTTP.Connections.HTTPProtocolFactory.GetProtocolFromUri(request.CurrentUri) ==
                BestHTTP.Connections.SupportedProtocols.HTTP)
                request.SetHeader("Authorization", "Bearer " + AccessToken);
            else
                request.Uri = PrepareUriImpl(request.Uri);
        }

        public Uri PrepareUri(Uri uri)
        {
            if (uri.Query.StartsWith("??"))
            {
                UriBuilder builder = new UriBuilder(uri);
                builder.Query = builder.Query.Substring(2);

                return builder.Uri;
            }

            return uri;
        }

        public void Cancel() => _connection.StartClose();

        private Uri PrepareUriImpl(Uri uri)
        {
            string query = string.IsNullOrEmpty(uri.Query) ? "" : uri.Query + "&";
            UriBuilder uriBuilder = new UriBuilder(uri.Scheme, uri.Host, uri.Port, uri.AbsolutePath,
                query + "access_token=" + AccessToken);
            return uriBuilder.Uri;
        }
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