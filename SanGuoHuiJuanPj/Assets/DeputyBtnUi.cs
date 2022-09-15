﻿using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DeputyBtnUi : MonoBehaviour
{
    public enum Modes
    {
        Locked,
        Ready,
        Assigned
    }
    [SerializeField] private Button btn;
    [SerializeField] private Image lockImage;
    [SerializeField] private Image readyImage;
    [SerializeField] private GameCardUi gameCardUi;
    public GameCard DeputyCard { get; private set; }
    public void Init(UnityAction onclickAction) => btn.onClick.AddListener(onclickAction);

    public void SetMode(Modes mode,GameCard card = null)
    {
        ResetUi();
        DeputyCard = card;
        btn.enabled = mode != Modes.Locked;
        gameCardUi.CityOperation.gameObject.SetActive(false);
        gameCardUi.WarOperation.gameObject.SetActive(false);
        switch (mode)
        {
            case Modes.Locked:
                lockImage.gameObject.SetActive(true);
                break;
            case Modes.Ready:
                readyImage.gameObject.SetActive(true);
                break;
            case Modes.Assigned:
                gameCardUi.gameObject.SetActive(true);
                gameCardUi.Init(DeputyCard);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        Display(true);
    }

    private void ResetUi()
    {
        DeputyCard = null;
        readyImage.gameObject.SetActive(false);
        lockImage.gameObject.SetActive(false);
        gameCardUi.gameObject.SetActive(false);
        btn.enabled = false;
        Display(false);
    }
    public void Display(bool display) => gameObject.SetActive(display);
}