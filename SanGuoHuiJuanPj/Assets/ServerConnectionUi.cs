using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ConnectionStates = BestHTTP.SignalRCore.ConnectionStates;

public enum ConnMode
{
    Disconnected,
    Connected,
    Await
}

public class ServerConnectionUi : MonoBehaviour
{
    public SignalRClient SignalRClient => SignalRClient.instance;

    public Image Connected;
    public Image Disconnected;
    public Image Await;
    public Text Message;

    private Dictionary<Image, ConnMode> Map;

    private void Start()
    {
        Map = new Dictionary<Image, ConnMode>
        {
            { Connected, ConnMode.Connected },
            { Disconnected, ConnMode.Disconnected },
            { Await, ConnMode.Await }
        };
        SetMode(ConnMode.Disconnected);
    }

    // Update is called once per frame
    void Update()
    {
        if (Map == null) return;
        SetMode(SignalRClient.Status.ToConnMode());
    }

    private void SetMode(ConnMode mode)
    {
        foreach (var pair in Map) pair.Key.gameObject.SetActive(pair.Value == mode);
        SetMessage();
    }

    private void SetMessage()
    {
        var text = SignalRClient?.Status?.ToString() ?? "";
        Message.text = text;
    }
}

public static class ConnectionStateExtension
{
    public static ConnMode ToConnMode(this ConnectionStates? state) => state?.ToConnMode() ?? ConnMode.Disconnected;
    public static ConnMode ToConnMode(this ConnectionStates state)
    {
        return state switch
        {
            ConnectionStates.Authenticating => ConnMode.Await,
            ConnectionStates.Negotiating => ConnMode.Await,
            ConnectionStates.Redirected => ConnMode.Await,
            ConnectionStates.Reconnecting => ConnMode.Await,
            ConnectionStates.CloseInitiated => ConnMode.Await,
            ConnectionStates.Initial => ConnMode.Disconnected,
            ConnectionStates.Closed => ConnMode.Disconnected,
            ConnectionStates.Connected => ConnMode.Connected,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}