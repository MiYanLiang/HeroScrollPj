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
#if UNITY_EDITOR
    public void CallLogin()
    {
        if (isCalled) return;
        isCalled = true;
        var username = _inputField.text;
        Http.Post("http://localhost:7071/api/ServerSignIn", Json.Serialize(new object[]
            { 25, -1, "4.0"}), text =>
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
