using CorrelateLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ServerListUi : SignInBaseUi
{
    [SerializeField] private ServerUi ServerPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button changePwdButton;
    public Button backButton;
    [SerializeField] private ConfirmWindowUi confirmWindowUi;
    private List<ServerUi> Servers { get; } = new List<ServerUi>();
    public ServerUi SelectedServer => Servers[SelectedServerIndex];
    public int SelectedServerIndex { get; private set; }

    private UnityAction ResetAction;
    private UnityAction ChangePwdAction;
    public void Set(ServerInfo[] servers, UnityAction<int> onLoginAction, UnityAction onClickReset,
        UnityAction onClickChangePwd)
    {
        foreach (var ui in Servers.ToArray()) Destroy(ui.gameObject);
        Servers.Clear();
#if !UNITY_EDITOR
        servers = servers.Where(s => s.Zone >= 0).ToArray();
#endif
        for (var i = 0; i < servers.Length; i++)
        {
            var server = servers[i];
            var s = Instantiate(ServerPrefab, scrollRect.content.transform);
            var i1 = i;
            s.SelectButton.onClick.AddListener(() => OnSelect(i1));
            Servers.Add(s);
            s.Init(server.Zone, server.Title, server == servers[^1], server.StartDate, server.CloseDate, server.ApiUrl);
        }

        OnSelect(GamePref.LastServiceZone);
        loginButton.onClick.AddListener(() => onLoginAction.Invoke(SelectedServer.Zone));
        resetButton.onClick.RemoveAllListeners();
        resetButton.onClick.AddListener(onClickReset);
        changePwdButton.onClick.RemoveAllListeners();
        changePwdButton.onClick.AddListener(onClickChangePwd);
    }

    private void OnSelect(int index)
    {
        if (index < 0 || (Servers.Count > index && !Servers[index].IsActive))
            index = Servers.Count - 1;//默认服务选项
        ServerUi selectedServerUi = null;
        for (var i = 0; i < Servers.Count; i++)
        {
            var ui = Servers[i];
            var isSelect = i == index;
            ui.OnSelected(isSelect);
            if (isSelect)
                selectedServerUi = ui;
        }
        if (selectedServerUi == null)
            throw new NullReferenceException("Selected ServerUi Not Found!");
        Server.ApiServer = selectedServerUi.ApiUrl+"/api/";
        SelectedServerIndex = index;
    }

    public override void ResetUi()
    {
        base.ResetUi();
        ServerPrefab.gameObject.SetActive(false);
        confirmWindowUi.gameObject.SetActive(false);
        backButton.onClick.RemoveAllListeners();
        loginButton.onClick.RemoveAllListeners();
    }

    public class ServerInfo
    {
        public int Zone { get; set; }
        public string Title { get; set; }
        public string ApiUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime CloseDate { get; set; }
    }

    public void OpenConfirmWindow(string msg,UnityAction action) => confirmWindowUi.Open(msg, action);
}