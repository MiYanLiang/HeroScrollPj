using System.Collections.Generic;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class ServerPanel : MonoBehaviour
{
    enum States
    {
        None,
        ServerMaintenance,
        TimeOut,
        Other
    }
    public Button reconnectButton;
    public Button exitButton;
    public Text serverMaintenance;
    public Text requestTimeOut;
    public Text Message;
    public Text exceptionMsg;
    private SignalRClient SignalR;

    private Dictionary<Text, States> StateSet
    {
        get
        {
            if (stSet == null)
            {
                stSet = new Dictionary<Text, States>
                {
                    {Message, States.Other},
                    {serverMaintenance, States.ServerMaintenance},
                    {requestTimeOut, States.TimeOut}
                };
            }
            return stSet;
        }
    }
    private Dictionary<Text, States> stSet; 

    public void Init(SignalRClient signalR)
    {
        SignalR = signalR;
        SignalR.SubscribeAction(EventStrings.SC_Disconnect, ServerCallDisconnect);
        reconnectButton.onClick.AddListener(ReconnectWithTips);
        exitButton.onClick.AddListener(Application.Quit);
        exitButton.gameObject.SetActive(false);
        gameObject.SetActive(false);
        UiShow(States.None);
        SetException();
    }

    public void SetException(string exception = null)
    {
        exceptionMsg.gameObject.SetActive(!string.IsNullOrWhiteSpace(exception));
        exceptionMsg.text = exception;
    }

    private void UiShow(States state)
    {
        gameObject.SetActive(state != States.None);
        foreach (var ui in StateSet)
        {
            ui.Key.gameObject.SetActive(ui.Value == state);
        }
    }

    //当服务器强制离线
    private void ServerCallDisconnect(string message)
    {
        exitButton.gameObject.SetActive(true);
        SignalR.Disconnect(Disconnected);
        StopAllCoroutines();

        void Disconnected()
        {
            var state = message.IsNullArg() ? States.ServerMaintenance : States.Other;
            if (state == States.Other)
            {
                var obj = Json.Deserialize<ServerData>(message);
                Message.text = obj != null ? obj.Data.Msg : message;
            }
            UiShow(state);
        }
    }
    private class ServerData
    {
        public Message Data { get; set; }
        public class Message
        {
            public string Msg { get; set; }
        }
    }

    private async void ReconnectWithTips() => await SignalR.ReconnectServerWithTips();
}
