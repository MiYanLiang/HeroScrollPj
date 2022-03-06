using System.Data;
using Plugins.AntiAddictionUIKit;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TapTap防沉迷系统
/// </summary>
public class TapTapAntiAddict : MonoBehaviour
{
    string gameIdentifier = "43NZKkKAASkH9Cm2jz";
    // 是否启用时长限制功能
    bool useTimeLimit = true;
    // 是否启用消费限制功能
    bool usePaymentLimit = true;
    // 是否显示切换账号按钮
    bool showSwitchAccount = false;
    bool isLogged = false;

    //如果使用单纯 TapTap 用户认证则可以用 openid 或 unionid。。
    //如果接入 TDS 内建账户系统，可以用玩家的 objectId；
    //如果不使用 TapTap 快速认证,需要传入的玩家唯一标识 userIdentifier，由游戏自己定义。
    
    private string userIdentifier = null;//"玩家唯一标识";
    public void Init(UnityAction<AntiAddictionCallbackData> callbackHandler)
    {
        AntiAddictionUIKit.Init(gameIdentifier, useTimeLimit, usePaymentLimit, showSwitchAccount,
            (antiAddictionCallbackData) => {
                int code = antiAddictionCallbackData.code;
                MsgExtraParams extras = antiAddictionCallbackData.extras;
                // 根据 code 不同提示玩家不同信息，详见下面的说明
                if (code == 500)
                {
                    isLogged = true;
                    // 开始计时
                    AntiAddictionUIKit.EnterGame();
                    Debug.Log("玩家登录后判断当前玩家可以进行游戏");
                }
                callbackHandler.Invoke(antiAddictionCallbackData);
            },
            ShowMessage
        );
    }

    public void StartUp(string userIdentifier) => AntiAddictionUIKit.Startup(false, userIdentifier);

    void OnApplicationPause(bool pauseStatus)
    {
        if (!isLogged) return;
        if (pauseStatus)
        {
            AntiAddictionUIKit.EnterGame();
        }
        else AntiAddictionUIKit.LeaveGame();
    }

    void OnApplicationQuit()
    {
        if(isLogged) AntiAddictionUIKit.Logout();
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"异常：{message}");
        PlayerDataForGame.instance.ShowStringTips(message);
    }
}