using System.Collections;
using System.Collections.Generic;
using BestHTTP.SignalRCore;
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
    private int count;
    public Button reconnectButton;
    public Button exitButton;
    public Text serverMaintenance;
    public Text requestTimeOut;
    public Text Message;
    public Text exceptionMsg;
    private SignalRClient SignalR;
    private bool isDisconnectRequested = false;

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
        SignalR.OnStatusChanged += OnStatusChanged;
        reconnectButton.onClick.AddListener(Reconnect);
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

    private void OnConnectAction(bool isConnected, int code, SignalRClient.SignalRConnectionInfo info)
    {
        gameObject.SetActive(!isConnected);
        SetException(code.ToString());
    }

    private void OnStatusChanged(ConnectionStates state)
    {
        StopAllCoroutines();
        gameObject.SetActive(state != ConnectionStates.Connected);
        UpdateReconnectBtn(state);
        switch (state)
        {
            case ConnectionStates.Negotiating:
            case ConnectionStates.Reconnecting:
                StartCoroutine(Counting(state));
                return;
            case ConnectionStates.Closed:
                Reconnect();
                break;
        }
    }

    //当服务器强制离线
    private void ServerCallDisconnect(string arg)
    {
        isDisconnectRequested = true;
        exitButton.gameObject.SetActive(true);
        SignalR.Disconnect();
        StopAllCoroutines();
        OnStatusChanged(ConnectionStates.Closed);
        var state = arg.IsNullArg() ? States.ServerMaintenance : States.Other;
        if (state == States.Other) Message.text = arg;
        UiShow(state);
    }

    private void UpdateReconnectBtn(ConnectionStates status)
    {
        reconnectButton.gameObject.SetActive(status != ConnectionStates.Connected);
        reconnectButton.interactable = status == ConnectionStates.Closed;
    }

    IEnumerator Counting(ConnectionStates state)
    {
        UpdateReconnectBtn(state);
        while (true)
        {
            Debug.Log($"等待......{count}");
            yield return new WaitForSeconds(1);
            count++;
        }
    }

    void OnApplicationFocus(bool isFocus)
    {
        if(!isFocus)return;
        if(SignalR.Status != ConnectionStates.Connected) Reconnect();
    }

    private void Reconnect()
    {
        if (!isDisconnectRequested) SignalR.ReconnectServer();
    }

}
