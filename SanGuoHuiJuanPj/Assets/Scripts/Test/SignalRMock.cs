using System;
using System.Collections;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class SignalRMock : MonoBehaviour
{
    [SerializeField] private SignalRClient _signalRClient;
    [SerializeField] private ApiPanel _apiPanel;
    [SerializeField] private InputField _inputField;
    private bool isCalled = false;
    [SerializeField] private string _serverSignInUrl = "https://localhost:5001/api/ServerSignIn";
    [SerializeField] private int _userId = 25;
    [SerializeField] private int _serverId = -1;
#if UNITY_EDITOR
    public void CallLogin()
    {
        if (isCalled) return;
        isCalled = true;
        Http.Post(_serverSignInUrl, Json.Serialize(new object[]
            { _userId, _serverId, "4.0"}), text =>
        {
            var info = Json.Deserialize<SignalRConnectionInfo>(text);
            if (info == null)
            {
                PlayerDataForGame.instance.ShowStringTips(text);
                throw new NotImplementedException($"测试服链接错误： {text}");
            }

            TokenLogin(info);
        }, "SignalRLoginTest");
        //var url = $"{Server.ApiServer}SignalRTestLogin?{ApiPanel.TestUserQuery(username)}";
        //Http.Post(url, string.Empty, text =>
        //{
        //    var bag = DataBag.DeserializeBag(text);
        //    if (bag == null)
        //    {
        //        PlayerDataForGame.instance.ShowStringTips(text);
        //        throw new NotImplementedException($"连接不到测试服： {text}");
        //    }
        //
        //    var info = bag.Get<SignalRConnectionInfo>(0);
        //    TokenLogin(info);
        //}, "SignalRLoginTest");
    }

    private void TokenLogin(SignalRConnectionInfo info)
    {
        _signalRClient.TokenLogin(info, success =>
        {
            if (success)
                _apiPanel.SyncSaved(() =>
                {
                    GameSystem.Instance.Init();
                    GameSystem.Instance.SetScene(GameSystem.GameScene.MainScene);
                    gameObject.SetActive(false);
                });
        });
    }

    public void SyncSaved()
    {
        _apiPanel.SyncSaved(() =>
        {
            XDebug.Log<SignalRMock>("SyncSaved!");
            GameSystem.Instance.Init();
            GameSystem.Instance.SetScene(GameSystem.GameScene.MainScene);
            gameObject.SetActive(false);
        });
    }
#endif
}
