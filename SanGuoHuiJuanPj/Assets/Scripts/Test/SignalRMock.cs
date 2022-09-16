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
        var url = $"{Server.ApiServer}SignalRTestLogin?{ApiPanel.TestUserQuery(username)}";
        Http.Post(url, string.Empty, text =>
        {
            var bag = DataBag.DeserializeBag(text);
            if (bag == null)
            {
                PlayerDataForGame.instance.ShowStringTips(text);
                throw new NotImplementedException($"连接不到测试服： {text}");
            }

            var info = bag.Get<SignalRConnectionInfo>(0);
            TokenLogin(info);
        }, "SignalRLoginTest");
    }

    private async void TokenLogin(SignalRConnectionInfo info)
    {
        await _signalRClient.TokenLogin(info);
        _apiPanel.SyncSaved(() =>
        {
            GameSystem.Instance.Init();
            gameObject.SetActive(false);
        });
    }
#endif
}
