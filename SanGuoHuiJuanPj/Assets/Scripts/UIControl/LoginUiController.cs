using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Assets.Scripts.Utl;
using CorrelateLib;
using Plugins.AntiAddictionUIKit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Json = CorrelateLib.Json;

public class LoginUiController : MonoBehaviour
{
    const string Undefined = "undefined";
    [Serializable]
    public enum ActionWindows
    {
        Login,
        Register,
        Info,
        ChangePassword,
        ForgetPassword,
        ResetAccount,
        ServerList
    }
    public LoginUi login;
    public RegUi register;
    public AcInfoUi accountInfo;
    public ServerListUi serverList;
    public ChangePwdUi changePassword;
    public RetrievePasswordUi forgetPassword;
    public ResetAccountUi resetAccount;
    public UnityAction<string,string,int,int> OnLoggedInAction;
    public UnityAction OnSignInAction;
    public UnityAction OnResetAccountAction;
    public Image busyPanel;
    [SerializeField] private Image AddictPanel;
    public string DeviceIsBound = @"设备已绑定了账号，请用设备登录修改账号信息!";
    private static bool isDeviceLogin;
    public Dictionary<int,ServerListUi.ServerInfo> Servers { get; set; }
    [SerializeField] private TapTapAntiAddict TtAa;

#if UNITY_EDITOR
    void Start()
    {
        OnLoggedInAction += (username, password,arrangement, newReg) =>
            XDebug.Log<LoginUiController>($"{nameof(OnLoggedInAction)} Invoke({username},{password},{arrangement},{newReg})!");
        OnResetAccountAction += () => XDebug.Log<LoginUiController>($"{nameof(OnResetAccountAction)} Invoke()!");
        //OnAction(ActionWindows.Login);
    }

#endif

    private Dictionary<ActionWindows, SignInBaseUi> _windowsObjs;

    private Dictionary<ActionWindows, SignInBaseUi> windowObjs
    {
        get
        {
            if (_windowsObjs == null)
                _windowsObjs = new Dictionary<ActionWindows, SignInBaseUi>
                {
                    { ActionWindows.Login, login },
                    { ActionWindows.Register, register },
                    { ActionWindows.ServerList, serverList },
                    { ActionWindows.Info, accountInfo },
                    { ActionWindows.ChangePassword, changePassword },
                    { ActionWindows.ForgetPassword, forgetPassword },
                    { ActionWindows.ResetAccount, resetAccount }
                };
            return _windowsObjs;
        }
    }

    public void Init()
    {
        TtAa.Init(TapTapCallbackHandler);
    }

    private void TapTapCallbackHandler(AntiAddictionCallbackData arg)
    {
        AddictPanel.gameObject.SetActive(false);
        var message = string.Empty;
        switch (arg.code)
        {
            case 500:
            {
                DisplayServerList();
                return;
            }
            case 1095: message = "未成年玩家限制。"; break;
            case 1030: message = "未成年玩家当前无法进入游戏。"; break;
            case 1000: message = "退出账号。"; break;
            case 9002: message = "实名未完成。"; break;
            case 1001: message = "账号点击了切换。"; break;
            default: message = $"未知错误：{arg.code}"; break;
        }
        OnAction(ActionWindows.Login);
        PlayerDataForGame.instance.ShowStringTips(message);
    }
    public void OnAction(ActionWindows action)
    {
        if(!gameObject.activeSelf)
            gameObject.SetActive(true);
        ResetWindows();
        switch (action)
        {
            case ActionWindows.Login:
                InitLogin();
                break;
            case ActionWindows.Register:
                InitRegister();
                break;
            case ActionWindows.Info:
                InitAccountInfo();
                break;
            case ActionWindows.ChangePassword:
                InitChangePassword();
                break;
            case ActionWindows.ForgetPassword:
                InitForgetPassword();
                break;
            case ActionWindows.ResetAccount:
                InitResetAccount();
                break;
            case ActionWindows.ServerList:
                InitServerList();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
        windowObjs[action].Open();
    }

    private void InitServerList()
    {
        OnSignInAction?.Invoke();
        serverList.gameObject.SetActive(true);
        serverList.backButton.onClick.AddListener(()=>OnAction(ActionWindows.Login));
        serverList.Set(Servers.Values.ToArray(), zone =>
        {
            AsyncInvoke(LoginZone);

            async Task LoginZone()
            {
                var result = await SignalRClient.instance.RequestToken(zone, 0);
                await UiResponse(result);
            }

            async Task CreateNewUserData()
            {
                var result = await SignalRClient.instance.RequestToken(zone, 1);
                await UiResponse(result);
            }

            async Task UiResponse(SignalRClient.SigninResult result)
            {
                switch (result.State)
                {
                    case SignalRClient.SigninResult.SignInStates.Failed:
                        serverList.SetMessage(result.Message);
                        return;
                    case SignalRClient.SigninResult.SignInStates.NoContent:
                        serverList.OpenConfirmWindow(result.Message, () => AsyncInvoke(CreateNewUserData));
                        return;
                    case SignalRClient.SigninResult.SignInStates.Success:
                        var info = Json.Deserialize<SignalRClient.SignalRConnectionInfo>(result.Content);
                        if (info == null)
                        {
                            serverList.SetMessage("客户端请求异常，请联系管理员。");
                            return;
                        }
                        var isSuccess = await SignalRClient.instance.TokenLogin(info);
                        if (!isSuccess)
                        {
                            serverList.SetMessage("登录失败，请重新登录。");
                            return;
                        }
                        OnLoggedInAction?.Invoke(login.username.text, login.password.text, 1, 0);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

        }, () => serverList.OpenConfirmWindow(
            $"重置【{serverList.SelectedZone}】服\n【{Servers[serverList.SelectedZone].Title}】玩家数据，\n请确定？",
            OnResetAccount), () => OnAction(ActionWindows.ChangePassword));
    }

    private void OnResetAccount()
    {
        AsyncInvoke(RequestApiReset);

        async Task RequestApiReset()
        {
            var response =
                await Http.PostAsync($"{Server.RESET_GAMEPLAY_API}?zone={serverList.SelectedZone}", SignalRClient.instance.LoginToken);
            var message = await response.Content.ReadAsStringAsync();
            serverList.SetMessage(message);
        }
    }

    public void Close()
    {
        ResetWindows();
        gameObject.SetActive(false);
    }

    private void InitResetAccount()
    {
        resetAccount.backBtn.onClick.AddListener(()=>OnAction(ActionWindows.Login));
        resetAccount.resetBtn.onClick.AddListener(()=>AsyncInvoke(ResetAccountApi));
    }

    private async Task ResetAccountApi()
    {
        var response = await Http.PostAsync(Server.RESET_GAMEPLAY_API,Json.Serialize(Server.GetUserInfo(resetAccount.username.text, resetAccount.password.text)));
        var message = "账号重置成功！";
        if (!response.IsSuccessStatusCode) message = $"请求失败[{(int)response.StatusCode}]!";
        UnityMainThread.thread.RunNextFrame(() =>
        {
            OnResetAccountAction?.Invoke();
            OnAction(ActionWindows.Login);
            login.SetMessage(message);
        });
    }

    private void InitForgetPassword()
    {
        forgetPassword.backBtn.onClick.AddListener(()=>OnAction(ActionWindows.Login));
        forgetPassword.deviceLoginBtn.onClick.AddListener(() => AsyncInvoke(OneClickLogin));
    }

    private void InitChangePassword()
    {
        changePassword.backBtn.onClick.AddListener(() => OnAction(ActionWindows.ServerList));
        changePassword.confirmBtn.onClick.AddListener(ChangePasswordApi);
    }

    private void ChangePasswordApi()
    {
        if (!IsPassPasswordLogic(changePassword.password, changePassword.rePassword, changePassword.message))
            return;
        var bag = DataBag.SerializeBag(nameof(ChangePasswordApi), SignalRClient.instance.LoginToken, changePassword.password.text);
        AsyncInvoke(ReqChangePassword);

        async Task ReqChangePassword()
        {
            var response = await Http.PostAsync(nameof(ChangePasswordApi), bag);
            var message = await response.Content.ReadAsStringAsync();
            var databag = DataBag.DeserializeBag(message);
            if (!databag.IsValid())
            {
                changePassword.SetMessage(message);
                return;
            }

            changePassword.password.text = string.Empty;
            changePassword.rePassword.text = string.Empty;
            changePassword.SetMessage("密码更换成功！");
        }
    }


    private void InitAccountInfo()
    {
        accountInfo.password.contentType = isDeviceLogin ? InputField.ContentType.Password : InputField.ContentType.Standard;
        accountInfo.backBtn.onClick.AddListener(Close);
        accountInfo.changePasswordBtn.onClick.AddListener(() => OnAction(ActionWindows.ChangePassword));
    }

    private void InitRegister()
    {
        register.backBtn.onClick.AddListener(()=>OnAction(ActionWindows.Login));
        register.regBtn.onClick.AddListener(()=>AsyncInvoke(RegisterAccountApi));
    }

    private static bool IsPassPasswordLogic(InputField password, InputField rePassword, Text message)
    {
        if (password.text.Length < 6)
        {
            message.text = "密码最少6个字符。";
            return false;
        }

        if (password.text != rePassword.text)
        {
            message.text = "密码不匹配！";
            return false;
        }

        return true;
    }

    private async Task RegisterAccountApi()
    {
        if (!IsPassPasswordLogic(register.password, register.rePassword, register.message))
            return;
        UserInfo userInfo = null;
        try
        {
            userInfo = await Http.PostAsync<UserInfo>(Server.PLAYER_REG_ACCOUNT_API,
                Json.Serialize(Server.GetUserInfo(register.username.text, register.password.text)));
        }
        catch (Exception)//为了防止任务报错
        {
            register.message.text = "注册失败!";
            return;

        }

        if (userInfo == null)
        {
            register.message.text = "注册失败!";
            return;
        }

        UnityMainThread.thread.RunNextFrame(() =>
        {
            PlayerDataForGame.instance.ShowStringTips("注册成功!");
            GamePref.SetUsername(register.username.text);
            GamePref.SetPassword(register.password.text);
            OnAction(ActionWindows.Login);
        });
    }

    private void InitLogin()
    {
        login.directLoginBtn.onClick.AddListener(()=>AsyncInvoke(OneClickLogin));
        login.loginBtn.onClick.AddListener(()=>AsyncInvoke(AccountLogin));
        login.forgetPasswordBtn.onClick.AddListener(()=>OnAction(ActionWindows.ForgetPassword));
        login.regBtn.onClick.AddListener(()=>AsyncInvoke(RequestUsernameToRegister));
        login.resetAccountBtn.onClick.AddListener(()=>OnAction(ActionWindows.ResetAccount));
    }

    private void UpdateUsername(string username)
    {
        login.username.text = username;
        register.username.text = username;
        accountInfo.username.text = username;
        changePassword.username.text = username;
        forgetPassword.username.text = username;
        resetAccount.username.text = username;
        GamePref.SetUsername(login.username.text);
    }

    private async Task OneClickLogin()
    {
        var response = await Http.PostAsync(Server.DEVICE_LOGIN_API,
            Json.Serialize(Server.GetUserInfo(login.username.text, login.password.text)));
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

            OnLoginPageErrorDisplay((int)severBackCode);
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var bag = DataBag.DeserializeBag(content);
        if (!bag.IsValid())
        {
            OnLoginPageErrorMessage(content);
            return;
        }
        ProceedAntiAddict(bag);
        GamePref.SetUsername(login.username.text);
        GamePref.SetPassword(string.Empty);
    }

    private async Task AccountLogin()
    {
        var response = await Http.PostAsync(Server.SIGNALR_LOGIN_API,
            Json.Serialize(Server.GetUserInfo(login.username.text, login.password.text)));
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

            OnLoginPageErrorDisplay((int)severBackCode);
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var bag = DataBag.DeserializeBag(content);
        if (!bag.IsValid())
        {
            OnLoginPageErrorMessage(content);
            return;
        }
        GamePref.SetPassword(login.password.text);
        ProceedAntiAddict(bag);
    }

    private void ProceedAntiAddict(DataBag bag)
    {
        AddictPanel.gameObject.SetActive(true);
        SignalRClient.instance.LoginToken = bag.Get<string>(0);
        var list = bag.Get<ServerListUi.ServerInfo[]>(1);
        var username = bag.Get<string>(2);
        UpdateUsername(username);
        Servers = list.ToDictionary(s => s.Zone, s => s);
        TtAa.StartUp(username);
    }

    private void DisplayServerList()
    {
        AddictPanel.gameObject.SetActive(false);
        OnAction(ActionWindows.ServerList);
    }


    private async Task RequestUsernameToRegister()
    {
        var content = Json.Serialize(Server.GetUserInfo(null, null));
        var response = await Http.PostAsync(Server.REQUEST_USERNAME_API, content);

        var uJson = await response.Content.ReadAsStringAsync();
        var user = Json.Deserialize<UserInfo>(uJson);
        var isSuccess = response.IsSuccessStatusCode;
        if (!isSuccess && response.StatusCode != HttpStatusCode.Unauthorized)
        {
            OnLoginPageErrorDisplay((int)response.StatusCode);
            return;
        }

        UnityMainThread.thread.RunNextFrame(() =>
        {
            OnAction(ActionWindows.Register);
            register.username.text = user.Username;
            register.message.text = isSuccess ? string.Empty : DeviceIsBound;
            register.ShowPasswordUi(isSuccess);
            login.username.text = user.Username;
        });
    }

    private async void AsyncInvoke(Func<Task> task)
    {
        busyPanel.gameObject.SetActive(true);
        await task.Invoke();
        UnityMainThread.thread.RunNextFrame(()=> busyPanel.gameObject.SetActive(false));
    }

    private void LoginAction(bool success, int code, SignalRClient.SignalRConnectionInfo info, string password)
    {
        isDeviceLogin = password == Undefined;
        GamePref.FlagClientLoginMethod(isDeviceLogin);
        busyPanel.gameObject.SetActive(false);
        if (success)
        {
            UnityMainThread.thread.RunNextFrame(() =>
                OnLoggedInAction.Invoke(info.Username, password, info.Arrangement, info.IsNewRegistered));
            return;
        }

        OnLoginPageErrorDisplay(code);
    }

    private void OnLoginPageErrorDisplay(int code) => OnLoginPageErrorMessage(Server.ResponseMessage(code));
    private void OnLoginPageErrorMessage(string message) => login.message.text = message;

    private void ResetWindows()
    {
        foreach (var obj in windowObjs) obj.Value.ResetUi();
    }

    private void DebugLog(string message)
    {
#if DEBUG
        XDebug.Log<SignalRClient>(DebugMsg(message));
#endif
    }
    private string DebugMsg(string message) => $"SignalR客户端: {message}";
}

public abstract class SignInBaseUi : MonoBehaviour
{
    public Text message;

    public virtual void ResetUi()
    {
        ResetMessage();
        gameObject.SetActive(false);
    }

    public virtual void Open()
    {
        gameObject.SetActive(true);
    }

    public void SetMessage(string msg)
    {
        if (message) message.text = msg;
    }

    public void ResetMessage()
    {
        if (message) message.text = string.Empty;
    }

}