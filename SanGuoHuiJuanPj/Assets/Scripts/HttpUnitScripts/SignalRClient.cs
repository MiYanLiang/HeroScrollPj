using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Assets;
using Assets.Scripts.Utl;
using Beebyte.Obfuscator;
using CorrelateLib;
using Microsoft.AspNetCore.SignalR.Client;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = System.Object;
using Json = CorrelateLib.Json;
using Newtonsoft.Json;

[Skip]
/// <summary>
/// Signal客户端
/// </summary>
public class SignalRClient : MonoBehaviour
{
    /// <summary>
    /// SignalR 网络状态
    /// </summary>
    public HubConnectionState Status;

    public int ServerTimeOutMinutes = 10;
    public int KeeAliveIntervalSecs = 600;
    public int HandShakeTimeoutSecs = 10;
    public ServerPanel ServerPanel;
    public event UnityAction<HubConnectionState> OnStatusChanged;
    public event UnityAction OnConnected; 
    public static SignalRClient instance;
    private CancellationTokenSource cancellationTokenSource;

    private static HubConnection _hub;
    private Dictionary<string, UnityAction<string>> _actions;
    private static readonly Type _stringType = typeof(string);
    private bool isBusy;
    public ApiPanel ApiPanel;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    private void Start()
    {
        //Login();
        _actions = new Dictionary<string, UnityAction<string>>();
#if UNITY_EDITOR
        OnStatusChanged += msg => DebugLog($"链接状态更变：{msg}");
#endif
        if(ServerPanel!=null) ServerPanel.Init(this);
        SubscribeAction(EventStrings.SR_UploadPy, OnServerCalledUpload);
        ApiPanel.Init(this);
    }


    async void OnApplicationQuit()
    {
        if (_hub == null)return;
        await _hub.StopAsync();
        await _hub.DisposeAsync();
    }

    /// <summary>
    /// login
    /// </summary>
    /// <param name="recallAction">( isSuccess, statusCode, arrangement )</param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    public async void UserLogin(UnityAction<bool,int ,SignalRConnectionInfo> recallAction, string username,
        string password)
    {
        if (isBusy) return;
        cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Token.Register(() => OnConnectionClose(XDebug.Throw<SignalRClient>("取消连接！")));
        var response = await Http.PostAsync(Server.SIGNALR_LOGIN_API,
            Json.Serialize(Server.GetUserInfo(username, password)), cancellationTokenSource.Token);
        if (!response.IsSuccessStatusCode)
        {
            DebugLog($"连接失败！[{response.StatusCode}]");
            var severBackCode = ServerBackCode.ERR_INVALIDOPERATION;
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    severBackCode = ServerBackCode.ERR_PW_ERROR;
                    break;
                case HttpStatusCode.HttpVersionNotSupported:
                    severBackCode = ServerBackCode.ERR_SERVERSTATE_ZERO;
                    break;
            }

            isBusy = false;
            recallAction.Invoke(false, (int) severBackCode, null);
            return;
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var connectionInfo = JsonConvert.DeserializeObject<SignalRConnectionInfo>(jsonString);
        var result = await ConnectSignalRAsync(connectionInfo, cancellationTokenSource.Token);
        
        isBusy = false;
        recallAction?.Invoke(result, (int) response.StatusCode, connectionInfo);
    }

    public async void DirectLogin(UnityAction<bool, int,SignalRConnectionInfo> recallAction)
    {
        if (isBusy) return;
        cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Token.Register(() => OnConnectionClose(XDebug.Throw<SignalRClient>("取消连接！")));
        var response = await Http.PostAsync(Server.DEVICE_LOGIN_API,
            Json.Serialize(Server.GetUserInfo(GamePref.Username, GamePref.Password)), cancellationTokenSource.Token);
        if (!response.IsSuccessStatusCode)
        {
            DebugLog($"连接失败！[{response.StatusCode}]");
            var severBackCode = ServerBackCode.ERR_INVALIDOPERATION;
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    severBackCode = ServerBackCode.ERR_PW_ERROR;
                    break;
                case HttpStatusCode.HttpVersionNotSupported:
                    severBackCode = ServerBackCode.ERR_SERVERSTATE_ZERO;
                    break;
            }

            isBusy = false;
            recallAction.Invoke(false, (int) severBackCode,null);
            return;
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var connectionInfo = JsonConvert.DeserializeObject<SignalRConnectionInfo>(jsonString);
        var result = await ConnectSignalRAsync(connectionInfo, cancellationTokenSource.Token);
        isBusy = false;
        recallAction?.Invoke(result, (int) response.StatusCode, connectionInfo);
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

    private async Task<bool> ConnectSignalRAsync(SignalRConnectionInfo connectionInfo,CancellationToken cancellationToken)
    {
        try
        {
            if (_hub != null)
            {
                _hub.Closed -= OnConnectionClose;
                _hub.Reconnected -= OnReconnected;
                _hub.Reconnecting -= OnReconnecting;
                Application.quitting -= OnDisconnect;
                _hub = null;
            }

            _hub = new HubConnectionBuilder()
                .WithUrl(connectionInfo.Url, cfg =>
                    cfg.AccessTokenProvider = () => Task.Run(() => connectionInfo.AccessToken, cancellationToken))
                .WithAutomaticReconnect(new TimeSpan[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                })
                .Build();
            _hub.ServerTimeout = TimeSpan.FromMinutes(ServerTimeOutMinutes);
            _hub.KeepAliveInterval = TimeSpan.FromSeconds(KeeAliveIntervalSecs);
            _hub.HandshakeTimeout = TimeSpan.FromSeconds(HandShakeTimeoutSecs);
            _hub.Closed += OnConnectionClose;
            _hub.Reconnected += OnReconnected;
            _hub.Reconnecting += OnReconnecting;
            _hub.On<string, string>(EventStrings.ServerCall, OnServerCall);

            if (_hub.State == HubConnectionState.Connected) throw new InvalidOperationException("Hub is connected!");
            await _hub.StartAsync(cancellationToken);
            StatusChanged(_hub.State,$"Host:{connectionInfo.Url},\nToken:{connectionInfo.AccessToken}\n连接成功！");
            cancellationTokenSource = null;
            Application.quitting += OnDisconnect;
        }
        catch (Exception e)
        {
            StatusChanged(_hub?.State ?? HubConnectionState.Disconnected, $"连接失败！{e}");
            return false;
        }
        return true;
    }

    private void OnDisconnect() => Disconnect();

    public async void ReconnectServer(UnityAction<bool> onReconnectAction)
    {
        if(_hub.State != HubConnectionState.Disconnected)return;
        try
        {
            await _hub.StartAsync();
            onReconnectAction?.Invoke(true);
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            DebugLog(e.ToString());
#endif
            onReconnectAction?.Invoke(false);
            PlayerDataForGame.instance.ShowStringTips("服务器尝试链接失败，请联系管理员！");
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
        if(RejectServer)
        {
            DebugLog($"已拒绝服务器调用：{method}: {content}");
            return;
        }
#endif
        if(! _actions.TryGetValue(method,out var action))return;
        action?.Invoke(content);
    }

    public async void Invoke(string method, UnityAction<string> recallAction , IViewBag bag = default, CancellationTokenSource tokenSource = default)
    {
        var result = await InvokeVb(method, bag, tokenSource);
        UnityMainThread.thread.RunNextFrame(()=>recallAction?.Invoke(result));
    }
    public async void Invoke(string method, UnityAction<string> recallAction , string databagSerialized, CancellationTokenSource tokenSource = default)
    {
        var result = await InvokeBag(method, databagSerialized, tokenSource);
        UnityMainThread.thread.RunNextFrame(()=>recallAction?.Invoke(result));
    }

    private async Task<string> InvokeVb(string method, IViewBag bag = default, CancellationTokenSource tokenSource = default)
    {
        try
        {
            if (bag == default) bag = ViewBag.Instance();
            if (tokenSource == null) tokenSource = new CancellationTokenSource();
            var result = await _hub.InvokeCoreAsync(method, _stringType,
                bag == null ? Array.Empty<object>() : new object[] { Json.Serialize(bag) },
                tokenSource.Token);
            return result?.ToString();
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
    private async Task<string> InvokeBag(string method, string serialized, CancellationTokenSource tokenSource = default)
    {
        try
        {
            if (tokenSource == null) tokenSource = new CancellationTokenSource();
            var result = await _hub.InvokeCoreAsync(method, _stringType,
                string.IsNullOrWhiteSpace(serialized) ? Array.Empty<object>() : new object[] { serialized },
                tokenSource.Token);
            return result?.ToString();
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
        if (_hub.State == HubConnectionState.Disconnected)
            return;

        if (cancellationTokenSource!=null && cancellationTokenSource.IsCancellationRequested == false)
        {
            cancellationTokenSource.Cancel();
            return;
        }
        if (_hub.State == HubConnectionState.Disconnected) return;
        await _hub.StopAsync();
        onActionDone?.Invoke();
    }

    #region Upload

    private async void OnServerCalledUpload(string args)
    {
        var param = Json.DeserializeList<ViewBag>(args);
        var saved = PlayerDataForGame.instance;
        var playerData = saved.pyData;
        var warChest = saved.gbocData.fightBoxs.ToArray();
        var redeemedCodes = new string[0];//saved.gbocData.redemptionCodeGotList.ToArray();
        var token = param[0];
        var campaign = saved.warsData.warUnlockSaveData.Select(w => new WarCampaignDto
                {WarId = w.warId, IsFirstRewardTaken = w.isTakeReward, UnlockProgress = w.unLockCount})
            .Where(w => w.UnlockProgress > 0).ToArray();
        var cards = saved.hstData.heroSaveData
            .Join(DataTable.Hero, c => c.id, h => h.Key, (c, h) => new {ForceId = h.Value.ForceTableId, c})
            .Concat(saved.hstData.towerSaveData.Join(DataTable.Tower, c => c.id, t => t.Key,
                (c, t) => new {t.Value.ForceId, c})).Concat(saved.hstData.trapSaveData.Join(DataTable.Trap, c => c.id,
                t => t.Key, (c, t) => new {t.Value.ForceId, c})).Where(c => c.c.chips > 0 || c.c.level > 0).ToList();
        var troops = cards.GroupBy(c => c.ForceId, c => c).Select(c =>
        {
            var list = c.GroupBy(o => o.c.typeIndex, o => o.c)
                .ToDictionary(o => (GameCardType) o.Key, o => o.Select(a => a).ToArray());
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
    private Task OnReconnecting(Exception exception)
    {
        StatusChanged(_hub.State, exception.ToString());
        return Task.CompletedTask;
    }

    /// <summary>
    /// 当客户端重新连线
    /// </summary>
    private Task OnReconnected(string msg)
    {
        StatusChanged(_hub.State, msg);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 当客户端断线的处理方法
    /// </summary>
    private Task OnConnectionClose(Exception exception)
    {
        StatusChanged(_hub.State, exception.ToString());
        return Task.CompletedTask;
    }
    private void StatusChanged(HubConnectionState status, string message)
    {
        ApiPanel.SetBusy(status != HubConnectionState.Connected);
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

        public SignalRConnectionInfo(string url, string accessToken, string username, int arrangement, int isNewRegistered)
        {
            Url = url;
            AccessToken = accessToken;
            Arrangement = arrangement;
            IsNewRegistered = isNewRegistered;
            Username = username;
        }
    }
}
