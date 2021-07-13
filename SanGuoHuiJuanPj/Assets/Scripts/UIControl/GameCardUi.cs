﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class GameCardUi : GameCardUiBase
{
    public enum CardModes
    {
        Basic,
        Desk,
        War
    }
    public CardModes Mode { get; private set; }

    public GameCardCityUiOperation CityOperation;
    public GameCardWarUiOperation WarOperation;

    public bool IsSelected
    {
        get
        {
            switch (Mode)
            {
                case CardModes.Basic:
                    return false;
                case CardModes.Desk:
                    return CityOperation.IsSelected;
                case CardModes.War:
                    return WarOperation.State == GameCardWarUiOperation.States.Selected;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Obsolete("这个Set方法需要与Init一起用，基本上需要重构整个类")]
    public void Set(CardModes mode)
    {
        GrayScale.Init();
        Image.sprite = CardInfo.Type == GameCardType.Hero
            ? GameResources.Instance.HeroImg[Card.CardId]
            : GameResources.Instance.FuZhuImg[CardInfo.ImageId];
        Short.Set(CardInfo.Short, GameResources.Instance.ClassImg[CardInfo.Type == GameCardType.Hero ? 0 : 1]);
        SetMode(mode);
        gameObject.SetActive(true);
    }

    public void Off()
    {
        Selected(false);
        gameObject.SetActive(false);
    }

    public void SetMode(CardModes mode)
    {
        WarOperation.ResetUi();
        CityOperation.ResetUi();
        Mode = mode;
        if (mode == CardModes.Desk)
        {
            CityOperation.Show(this);
        }

        Level.gameObject.SetActive(mode != CardModes.Basic);

        if (mode == CardModes.War)
        {
            WarOperation.Show(this);
        }

        WarOperation.gameObject.SetActive(mode == CardModes.War);
        CityOperation.gameObject.SetActive(mode == CardModes.Desk);
    }

    public void Selected(bool isSelected)
    {
        switch (Mode)
        {
            case CardModes.Desk:
                CityOperation.SetSelected(isSelected);
                break;
            case CardModes.War:
                WarOperation.SetState(GameCardWarUiOperation.States.Selected);
                break;
            case CardModes.Basic:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Test([CallerMemberName] string methodName = null) => Debug.Log($"{methodName}:Invoke()");
}