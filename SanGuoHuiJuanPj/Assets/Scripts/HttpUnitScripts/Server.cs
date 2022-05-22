using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Beebyte.Obfuscator;
using CorrelateLib;
using UnityEngine;

public static class Server
{
#if !UNITY_EDITOR
    public static string PLAYER_SAVE_DATA_UPLOAD_API { get; private set; } 
    public static string INSTANCE_ID_API { get; private set; } 
    public static string REQUEST_USERNAME_API { get; private set; } 
    public static string PLAYER_REG_ACCOUNT_API { get; private set; } 
    public static string PLAYER_UPLOAD_COUNT_API { get; private set; } 
    public static string USER_LOGIN_API { get; private set; } 
    public static string SIGNALR_LOGIN_API { get; private set; } 
    public static string DEVICE_LOGIN_API { get; private set; } 
    public static string RESET_GAMEPLAY_API { get; private set; }
    public static string GameServer { get; private set; }
    public static string TokenLogin { get; private set; }
#else
    // todo: CDN正式服
    //private static string ServerUrl { get; set; } = "https://HeroScrollPj.azureedge.net/api/";
    // Todo: 正式服
    //private static string ServerUrl { get; set; } = "https://heroscrollpjapi1.azurewebsites.net/api/";
    //todo: CDN测试服
    //private static string ServerUrl { get; set; } = "https://heroscroll.azureedge.net/api/";
    // Todo: 测试服
    //private static string ServerUrl { get; set; } = "https://herotestfuncapi.azurewebsites.net/api/";
    //private static string ServerUrl { get; set; } = "http://localhost:7071/api/";
    private static string ServerUrl { get; set; } = "http://localhost:8081/api/";
    public static string PLAYER_SAVE_DATA_UPLOAD_API { get; private set; } = "UploadSaveData";
    public static string INSTANCE_ID_API { get; private set; } = "GenerateUserId";
    public static string REQUEST_USERNAME_API { get; private set; } = "RequestUsername";
    public static string PLAYER_REG_ACCOUNT_API { get; private set; } = "RegUser";
    public static string PLAYER_UPLOAD_COUNT_API { get; private set; } = "UploadCount";
    public static string USER_LOGIN_API { get; private set; } = "Login";
    public static string SIGNALR_LOGIN_API { get; private set; } = "UserSignin"; //"SignalRLogin";
    public static string DEVICE_LOGIN_API { get; private set; } = "OneClickSignIn"; //"DeviceSignIn";
    public static string RESET_GAMEPLAY_API { get; private set; } = "ResetGamePlay";
    public static string TokenLogin { get; private set; } = "TokenLogin";
    public static string GameServer { get; private set; } = 
    "https://motahero.azurewebsites.net/api/";
    //"http://localhost:8081/api/";
#endif

    private static bool isInitialized;
    public static HttpClient InstanceClient()
    {
        return new HttpClient()
        {
            BaseAddress = new Uri(GameServer)
        };
    }

    public static void Initialize(ServerFields fields)
    {
        if (isInitialized) return;
        isInitialized = true;
#if !UNITY_EDITOR
        GameServer = fields.ServerUrl;
        PLAYER_SAVE_DATA_UPLOAD_API = fields.PLAYER_SAVE_DATA_UPLOAD_API;
        INSTANCE_ID_API = fields.INSTANCE_ID_API;
        REQUEST_USERNAME_API = fields.REQUEST_USERNAME_API;
        PLAYER_REG_ACCOUNT_API = fields.PLAYER_REG_ACCOUNT_API;
        PLAYER_UPLOAD_COUNT_API = fields.PLAYER_UPLOAD_COUNT_API;
        USER_LOGIN_API = fields.USER_LOGIN_API;
        SIGNALR_LOGIN_API = fields.SIGNALR_LOGIN_API;
        DEVICE_LOGIN_API = fields.DEVICE_LOGIN_API;
        RESET_GAMEPLAY_API = fields.RESET_GAMEPLAY_API;
        TokenLogin = fields.TOKEN_LOGIN_API;
        Versus.GetWarsV1 = fields.GetWarsV1;
        Versus.GetStageV1 = fields.GetStageV1;
        Versus.GetGetFormationV1 = fields.GetGetFormationV1;
        Versus.GetCheckPointResultV1 = fields.GetCheckPointResultV1;
        Versus.GetCheckPointResultV2 = fields.GetCheckPointResultV2;
        Versus.StartChallengeV1 = fields.StartChallengeV1;
        Versus.SubmitFormationV1 = fields.SubmitFormationV1;
        Versus.CancelChallengeV1 = fields.CancelChallengeV1;
#endif
    }

    public static string ResponseMessage(int code)
    {
        var message = string.Empty;
        switch ((ServerBackCode) code)
        {
            case ServerBackCode.SUCCESS:
                message = "SUCCESS";
                break;
            case ServerBackCode.ERR_NAME_EXIST:
                message = "ERR_NAME_EXIST";
                break;
            case ServerBackCode.ERR_NAME_SHORT:
                message = "ERR_NAME_SHORT";
                break;
            case ServerBackCode.ERR_PASS_SHORT:
                message = "密码长度过短";
                break;
            case ServerBackCode.ERR_NAME_NOT_EXIST:
                message = "账号不存在";
                break;
            case ServerBackCode.ERR_DATA_NOT_EXIST:
                message = "ERR_DATA_NOT_EXIST";
                break;
            case ServerBackCode.ERR_PHONE_SHORT:
                message = "ERR_PHONE_SHORT";
                break;
            case ServerBackCode.ERR_ACCOUNT_BIND_OTHER_PHONE:
                message = "重复绑定手机";
                break;
            case ServerBackCode.ERR_NAME_ILLEGAL:
                message = "ERR_NAME_ILLEGAL";
                break;
            case ServerBackCode.ERR_PHONE_ILLEGAL:
                message = "手机号错误";
                break;
            case ServerBackCode.ERR_PW_ERROR:
                message = "密码错误";
                break;
            case ServerBackCode.ERR_PHONE_BIND_OTHER_ACCOUNT:
                message = "该手机号绑定了其他账号";
                break;
            case ServerBackCode.ERR_PHONE_ALREADY_BINDED:
                message = "已经绑定过了";
                break;
            case ServerBackCode.ERR_SERVERSTATE_ZERO:
                message = "服务器正维护中";
                break;
            case ServerBackCode.ERR_INVALIDOPERATION:
            default:
                message = "服务器响应错误";
                break;
        }

        return message;
    }

    public static UserInfo GetUserInfo(string username,string password)
    {
        var phone = string.Empty;
        if (PlayerDataForGame.instance != null && PlayerDataForGame.instance.acData != null)
            phone = PlayerDataForGame.instance.acData.Phone;
        return new UserInfo
        {
            DeviceId = SystemInfo.deviceUniqueIdentifier,
            Password = password,
            Phone = phone,
            Username = username,
            GameVersion = float.Parse(Application.version)
        };
    }
}