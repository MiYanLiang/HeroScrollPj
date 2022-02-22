﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ConfirmWindowUi : MonoBehaviour
{
    [SerializeField] private Text Message;
    [SerializeField] private Button YesButton;
    [SerializeField] private Button CancelButton;
    private UnityAction YesAction;
    private UnityAction NoAction;
    public void Open(string message,UnityAction yesAction, UnityAction noAction = null)
    {
        Message.text = message;
        YesAction = yesAction;
        NoAction = noAction;
        YesButton.onClick.AddListener(() =>
        {
            YesButton.onClick.RemoveAllListeners();
            CancelButton.onClick.RemoveAllListeners();
            OnAction(true);
        });
        CancelButton.onClick.AddListener(() =>
        {
            YesButton.onClick.RemoveAllListeners();
            CancelButton.onClick.RemoveAllListeners();
            OnAction(false);
        });
        gameObject.SetActive(true);
    }

    private void OnAction(bool ok)
    {
        if (ok) YesAction?.Invoke();
        else NoAction?.Invoke();
        gameObject.SetActive(false);
    }
}