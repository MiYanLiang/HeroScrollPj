using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ServerListUi : SignInBaseUi
{
    [SerializeField] private ServerUi ServerPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button loginButton;
    public Button backButton;
    [SerializeField] private GameObject createNewWindow;
    [SerializeField] private Button createNewButton;
    private Dictionary<int, (ServerUi ui, string url)> Servers = new Dictionary<int, (ServerUi ui, string url)>();
    private int SelectedZone;

    public void Set(ServerInfo[] servers, UnityAction<int> onLoginAction)
    {
        foreach (var button in Servers.Values.ToArray()) Destroy(button.ui.gameObject);
        Servers.Clear();
        foreach (ServerInfo server in servers
#if !UNITY_EDITOR
                     .Where(s=>s.Zone>=0)
#endif                 
                     .ToArray())
        {
            var s = Instantiate(ServerPrefab, scrollRect.content.transform);
            s.SelectButton.onClick.AddListener(() => OnSelect(server.Zone));
            Servers.Add(server.Zone, (s, server.Api));
            s.ZoneText.text = server.Zone.ToString();
            s.NameText.text = server.Title;
            s.gameObject.SetActive(true);
        }

        OnSelect(GamePref.LastServiceZone);
        loginButton.onClick.AddListener(() => onLoginAction.Invoke(SelectedZone));
    }

    private void OnSelect(int zone)
    {
        SelectedZone = zone;
        foreach (var s in Servers)
        {
            var ui = s.Value.ui;
            ui.OnSelected(s.Key == zone);
        }
    }

    public override void ResetUi()
    {
        base.ResetUi();
        ServerPrefab.gameObject.SetActive(false);
        createNewWindow.gameObject.SetActive(false);
        backButton.onClick.RemoveAllListeners();
        loginButton.onClick.RemoveAllListeners();
    }

    public class ServerInfo
    {
        public int Zone { get; set; }
        public string Api { get; set; }
        public string Title { get; set; }
    }

    public void CreateNewWindow(UnityAction action)
    {
        createNewWindow.gameObject.SetActive(true);
        createNewButton.onClick.AddListener(() =>
        {
            createNewButton.onClick.RemoveAllListeners();
            createNewWindow.gameObject.SetActive(false);
            action();
        });
    }
}