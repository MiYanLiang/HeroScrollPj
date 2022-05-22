using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using Assets.Editor;
using UnityEngine;
using UnityEditor;
using CorrelateLib;

public class Configuration : MonoBehaviour
{
    public TextAsset gameConfig;

    public void Init()
    {
        var json = EncryptDecipherTool.DESDecrypt(gameConfig.text);
        var serverFields = Json.Deserialize<ServerFields>(json);
        Server.Initialize(serverFields);
    }
}

public class ServerFields
{
    public ServerFields(ConfigAsset configAsset)
    {
        ServerUrl = configAsset.ServerUrl;
        INSTANCE_ID_API = configAsset.INSTANCE_ID_API;
        PLAYER_UPLOAD_COUNT_API = configAsset.PLAYER_UPLOAD_COUNT_API;
        PLAYER_REG_ACCOUNT_API = configAsset.PLAYER_REG_ACCOUNT_API;
        PLAYER_SAVE_DATA_UPLOAD_API = configAsset.PLAYER_SAVE_DATA_UPLOAD_API;
        USER_LOGIN_API = configAsset.USER_LOGIN_API;
        SIGNALR_LOGIN_API = configAsset.SIGNALR_LOGIN_API;
        REQUEST_USERNAME_API = configAsset.REQUEST_USERNAME_API;
        DEVICE_LOGIN_API = configAsset.DEVICE_LOGIN_API;
        RESET_GAMEPLAY_API = configAsset.RESET_GAMEPLAY_API;
        TOKEN_LOGIN_API = configAsset.TOKEN_LOGIN_API;
        GetWarsV1 = configAsset.GetWarsV1;
        GetStageV1 = configAsset.GetStageV1;
        GetGetFormationV1 = configAsset.GetGetFormationV1;
        GetCheckPointResultV1 = configAsset.GetCheckPointResultV1;
        GetCheckPointResultV2 = configAsset.GetCheckPointResultV2;
        StartChallengeV1 = configAsset.StartChallengeV1;
        SubmitFormationV1 = configAsset.SubmitFormationV1;
        CancelChallengeV1 = configAsset.CancelChallengeV1;
    }

    public ServerFields() { }

    public string ServerUrl { get; set; }
    public string PLAYER_SAVE_DATA_UPLOAD_API { get; set; }
    public string INSTANCE_ID_API { get; set; }
    public string PLAYER_REG_ACCOUNT_API { get; set; }
    public string PLAYER_UPLOAD_COUNT_API { get; set; }
    public string USER_LOGIN_API { get; set; }
    public string SIGNALR_LOGIN_API { get; set; }
    public string REQUEST_USERNAME_API { get; set; }
    public string DEVICE_LOGIN_API { get; set; }
    public string RESET_GAMEPLAY_API { get; set; }
    public string TOKEN_LOGIN_API { get; set; }

    #region Rk
    public string CancelChallengeV1 { get; set; }
    public string SubmitFormationV1 { get; set; }
    public string StartChallengeV1 { get; set; }
    public string GetCheckPointResultV1 { get; set; }
    public string GetCheckPointResultV2 { get; set; }
    public string GetGetFormationV1 { get; set; }
    public string GetStageV1 { get; set; }
    public string GetWarsV1 { get; set; }
    #endregion
}