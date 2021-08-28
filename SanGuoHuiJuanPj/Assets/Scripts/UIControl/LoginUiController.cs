using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Assets.Scripts.Utl;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Json = CorrelateLib.Json;

public class LoginUiController : MonoBehaviour
{
    const string DeviceLoginString = "ClientIsDeviceLogin";
    [Serializable]
    public enum ActionWindows
    {
        Login,
        Register,
        Info,
        ChangePassword,
        ForgetPassword,
        ResetAccount
    }
    public LoginUi login;
    public RegUi register;
    public AcInfoUi accountInfo;
    public ChangePwdUi changePassword;
    public RetrievePasswordUi forgetPassword;
    public ResetAccountUi resetAccount;
    public UnityAction<string,string,int,int> OnLoggedInAction;
    public UnityAction OnResetAccountAction;
    public Image busyPanel;
    public string DeviceIsBound = @"设备已绑定了账号，请用设备登录修改账号信息!";
    private static bool isDeviceLogin;
#if UNITY_EDITOR
    void Start()
    {
        OnLoggedInAction += (username, password,arrangement, newReg) =>
            XDebug.Log<LoginUiController>($"{nameof(OnLoggedInAction)} Invoke({username},{password},{arrangement},{newReg})!");
        OnResetAccountAction += () => XDebug.Log<LoginUiController>($"{nameof(OnResetAccountAction)} Invoke()!");
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
                    {ActionWindows.Login, login},
                    {ActionWindows.Register, register},
                    {ActionWindows.Info, accountInfo},
                    {ActionWindows.ChangePassword, changePassword},
                    {ActionWindows.ForgetPassword, forgetPassword},
                    {ActionWindows.ResetAccount, resetAccount}
                };
            return _windowsObjs;
        }
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
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
        windowObjs[action].Open();
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
        forgetPassword.deviceLoginBtn.onClick.AddListener(DeviceLoginApi);
    }

    private void InitChangePassword()
    {
        changePassword.backBtn.onClick.AddListener(()=>OnAction(ActionWindows.Info));
        changePassword.confirmBtn.onClick.AddListener(ChangePasswordApi);
    }

    private void ChangePasswordApi()
    {
        if (!IsPassPasswordLogic(changePassword.password, changePassword.rePassword, changePassword.message))
            return;
        var viewBag = ViewBag.Instance().SetValues(GamePref.Username, GamePref.Password,
            SystemInfo.deviceUniqueIdentifier,
            changePassword.password.text);
        ApiPanel.instance.Invoke(vb =>
            {
                GamePref.SetUsername(vb.GetValue<string>(0));
                GamePref.SetPassword(changePassword.password.text);
                Close();
                PlayerDataForGame.instance.ShowStringTips("密码修改成功！");
                GamePref.FlagDeviceReg(changePassword.username.text);
            }, PlayerDataForGame.instance.ShowStringTips,
            EventStrings.Req_ChangePassword,
            viewBag);
    }

    private void InitAccountInfo()
    {
        //accountInfo.warningMessage.gameObject.SetActive(!GamePref.IsUserAccountCompleted);
        var isDefaultPwd = accountInfo.password.text == DeviceLoginString;
        accountInfo.password.contentType = isDefaultPwd ? InputField.ContentType.Password : InputField.ContentType.Standard;
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
        login.directLoginBtn.onClick.AddListener(DeviceLoginApi);
        login.loginBtn.onClick.AddListener(AccountLoginApi);
        login.forgetPasswordBtn.onClick.AddListener(()=>OnAction(ActionWindows.ForgetPassword));
        login.regBtn.onClick.AddListener(()=>AsyncInvoke(RequestUsernameToRegister));
        login.resetAccountBtn.onClick.AddListener(()=>OnAction(ActionWindows.ResetAccount));
    }

    private async Task RequestUsernameToRegister()
    {
        var response = await Http.PostAsync(Server.REQUEST_USERNAME_API,
            Json.Serialize(Server.GetUserInfo(null, null)));
        var uJson = await response.Content.ReadAsStringAsync();
        var isSuccess = response.IsSuccessStatusCode;

        if (!isSuccess && response.StatusCode != HttpStatusCode.Unauthorized)
        {
            OnLoginPageErrorDisplay((int)response.StatusCode);
            return;
        }

        var user = Json.Deserialize<UserInfo>(uJson);
        UnityMainThread.thread.RunNextFrame(() =>
        {
            OnAction(ActionWindows.Register);
            register.username.text = user.Username;
            register.message.text = isSuccess ? string.Empty : DeviceIsBound;
            register.ShowPasswordUi(isSuccess);
        });
    }

    private async void AsyncInvoke(Func<Task> task)
    {
        busyPanel.gameObject.SetActive(true);
        await task.Invoke();
        UnityMainThread.thread.RunNextFrame(()=> busyPanel.gameObject.SetActive(false));
    }

    private void AccountLoginApi()
    {
        busyPanel.gameObject.SetActive(true);
        login.message.text = string.Empty;
        SignalRClient.instance.UserLogin((success, code, info) => LoginAction(success, code, info, login.password.text),
            login.username.text, login.password.text);
    }

    private void DeviceLoginApi()
    {
        busyPanel.gameObject.SetActive(true);
        login.message.text = string.Empty;
        SignalRClient.instance.DirectLogin((success, code, info) => LoginAction(success, code, info, DeviceLoginString));
    }

    private void LoginAction(bool success, int code, SignalRClient.SignalRConnectionInfo info,string password)
    {
        isDeviceLogin = password == DeviceLoginString;
        GamePref.FlagClientLoginMethod(isDeviceLogin);
        busyPanel.gameObject.SetActive(false);
        if (success)
        {
            if (!isDeviceLogin)
            {
                GamePref.SetPassword(password);
                accountInfo.password.text = password;
            }
            UnityMainThread.thread.RunNextFrame(() =>
                OnLoggedInAction.Invoke(info.Username, password, info.Arrangement, info.IsNewRegistered));
            return;
        }
        OnLoginPageErrorDisplay(code);
    }

    private void OnLoginPageErrorDisplay(int code)
    {
        login.message.text = Server.ResponseMessage(code);
    }

    private void ResetWindows()
    {
        foreach (var obj in windowObjs) obj.Value.ResetUi();
    }

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
        if(message) message.text = msg;
    }

    public void ResetMessage()
    {
        if(message) message.text = string.Empty;
    }
}