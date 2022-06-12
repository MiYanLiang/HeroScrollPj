using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BestHTTP.SignalRCore;
using BestHTTP.SignalRCore.Messages;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;

public class SignalRClientHub
{
    public event UnityAction<ConnectionStates,string> OnStatusChanged;
    public event Action<string, string> OnServerCall;
    private HubConnection _hub;
    /// <summary>
    /// SignalR 网络状态
    /// </summary>
    public ConnectionStates Status { get; private set; }
    private SignalRConnectionInfo ConnectionInfo { get; set; }
    private readonly int _hubInvokeRetries;
    private readonly int _retryIntervalMilliseconds;

    public SignalRClientHub(int hubInvokeRetries, int retryIntervalMilliseconds = 1000)
    {
        _hubInvokeRetries = hubInvokeRetries;
        _retryIntervalMilliseconds = retryIntervalMilliseconds;
        Application.quitting += OnDisconnect;
    }

    private HubConnection InstanceHub(string url, string token)
    {
        var conn = new HubConnection(new Uri(url), new JsonProtocol(new JsonNetEncoder()), new HubOptions
        {
            //ConnectTimeout = TimeSpan.FromMinutes(ServerTimeOutMinutes),
            //PingInterval = TimeSpan.FromMinutes(5),
            SkipNegotiation = true,
            PreferedTransport = TransportTypes.WebSocket
        });
        conn.CustomNegotiationResult(NegotiationResult.Instance(url, token));
        conn.AuthenticationProvider = new AzureSignalRServiceAuthenticator(conn, token);
        conn.OnClosed += OnConnectionClose;
        conn.OnReconnected += OnReconnected;
        conn.OnReconnecting += OnReconnecting;
        conn.On(EventStrings.ServerCall, OnServerCall);
        return conn;
    }
    //Hub connection
    public async Task<bool> ConnectSignalRAsync(SignalRConnectionInfo connectionInfo)
    {
        if (_hub != null && _hub.State != ConnectionStates.Closed)
            await CloseConnectionAsync();

        var cancellationToken = new CancellationTokenSource(30);
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

            _hub = InstanceHub(connectionInfo.Url, connectionInfo.AccessToken);
            await OnHubStartAsync();
        }
        catch (Exception e)
        {
            StatusChanged(_hub?.State ?? ConnectionStates.Closed, $"连接失败！{e}");
            if (!cancellationToken.IsCancellationRequested)
                cancellationToken.Cancel();
            return false;
        }

        ConnectionInfo = connectionInfo;
        return true;
    }
    private async Task OnHubStartAsync()
    {
        if (_hub.State == ConnectionStates.Connected) throw new InvalidOperationException("Hub is connected!");
        await _hub.ConnectAsync();
        StatusChanged(_hub.State, "SignalRHost:连接成功！");
    }
    
    public async Task<bool> HubReconnectTask()
    {
        try
        {
            if (ConnectionInfo == null)
                throw new NullReferenceException("ConnectionInfo = null!");
            Status = ConnectionStates.Reconnecting;
            var isSuccess = await ConnectSignalRAsync(ConnectionInfo);
            return isSuccess;
        }
        catch (Exception)
        {
            PlayerDataForGame.instance.ShowStringTips("服务器尝试链接失败，请稍后重试。");
            return false;
        }
    }

    private void StatusChanged(ConnectionStates status, string message)
    {
        Status = status;
        OnStatusChanged?.Invoke(status, message);
    }

    public async Task<TResult> HubInvokeAsync<TResult>(string method, CancellationToken cancellationToken,
        params object[] args) where TResult : class
    {
        var retries = 0;
        for (var i = 0; i < _hubInvokeRetries; i++)
        {
            try
            {
                retries = i;
                var result = await _hub.InvokeAsync<TResult>(method, cancellationToken, args);
                return result;
            }
            catch (Exception e)
            {
                if (i < _hubInvokeRetries)
                {
                    await HubReconnectTask();
                    await Task.Delay(i * _retryIntervalMilliseconds, cancellationToken);
                    continue;
                }
#if UNITY_EDITOR
                XDebug.LogError<SignalRClient>($"Error in interpretation {method}:{e}");
#endif
                var now = SysTime.Now;
                var timeText = $"{now.Year - 2000}.{now.Month}.{now.Day}";
                var username = GamePref.Username;
                var filtered = username.Split("yx").LastOrDefault();
                filtered = filtered?.Split("hj").LastOrDefault();
                var userid = int.TryParse(filtered, out var id) ? id - 10000 : 0;
                GameSystem.ServerRequestException(method,
                    $"v{Application.version}.{timeText}.{userid}.Retried:{retries}\n{e}");
            }
        }

        return null;
    }

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


    private void OnDisconnect() => Disconnect();
    private async void Disconnect(UnityAction onActionDone = null)
    {
        if (_hub.State == ConnectionStates.Closed || _hub.State == ConnectionStates.CloseInitiated) return;
        await CloseConnectionAsync();
        onActionDone?.Invoke();
    }
    #endregion

    public async void CloseConnection(UnityAction callbackAction)
    {
        if (_hub?.State is ConnectionStates.Closed or ConnectionStates.CloseInitiated)
        {
            callbackAction?.Invoke();
            return;
        }
        await CloseConnectionAsync();
        callbackAction?.Invoke();
    }

    private bool _isClosing = false;

    private async Task CloseConnectionAsync()
    {
        if(_isClosing) return;
        _isClosing = true;
        await _hub.CloseAsync();
        _isClosing = false;
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