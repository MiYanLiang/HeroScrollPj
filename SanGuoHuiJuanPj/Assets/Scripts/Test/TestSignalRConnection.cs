using System.Collections;
using System.Collections.Generic;
using CorrelateLib;
using UnityEngine;

public class TestSignalRConnection : MonoBehaviour
{
    public string Username;
    public string Password;
    public SignalRClient Client;

    public void Login()
    {
        Client.UserLogin(
            (isSuccess, code, info) =>
                Debug.Log($"SignalR连接: {isSuccess}, StatusCode[{code}], User[{info?.Username}] ,Arrangement[{info?.Arrangement}], NewReg[{info?.IsNewRegistered}]!"),
            Username,
            Password);
        var player = new PlayerData {Exp = 100};
        PlayerDataForGame.instance.pyData = player;
    }

    public void TestRequest()
    {
        ApiPanel.instance.InvokeVb(vb =>
            {
                XDebug.Log(typeof(TestSignalRConnection), $"Api[{EventStrings.Req_OnlineCharacters}] Success!");
                XDebug.Log(typeof(TestSignalRConnection), Json.Serialize(vb));
            },
            msg => XDebug.Log(typeof(TestSignalRConnection),
                $"Api[{EventStrings.Req_OnlineCharacters}] Failed! : {msg}"),
            EventStrings.Req_OnlineCharacters);
    }

    public void Disconnect() => Client.Disconnect();

}
