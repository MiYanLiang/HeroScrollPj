﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaYeEventUI : MonoBehaviour
{
    public Image prefab;
    public Color defaultColor;
    public Color activeColor;
    public Text text;
    public RectTransform contentLayout;
    public int space = 13;
    public Button button;
    private List<Image> list;

    public void Init(int maxValue)
    {
        list = new List<Image>();
        for (int i = 0; i < maxValue; i++)
        {
            var box = Instantiate(prefab, contentLayout);
            box.color = defaultColor;
            box.gameObject.SetActive(true);
            list.Add(box);
        }
        contentLayout.gameObject.SetActive(true);
        contentLayout.sizeDelta = new Vector2(list.Count * space, contentLayout.sizeDelta.y);
    }

    public void SetValue(int value)
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i].color = i < value ? activeColor : defaultColor;
        }
    }
}
