using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaYeCityEventUI : MonoBehaviour
{
    //进度条
    public Image prefab;
    public Color defaultColor;
    public Color activeColor;
    
    //等级限制文字
    public Text text;
    public RectTransform contentLayout;
    public int space = 13;
    public Button button;
    private List<Image> list;
    public Color cityNameColor;
    public Color defaultCityColor;
    public ForceFlagUI forceFlag;
    public Button storyButton;
    public Text storyTitle;
    public void Init(int maxValue)
    {
        if (list != null && list.Count > 0) list.ForEach(f => Destroy(f.gameObject));
        list = new List<Image>();
        for (int i = 0; i < maxValue; i++)
        {
            var box = Instantiate(prefab, contentLayout);
            box.color = defaultColor;
            box.gameObject.SetActive(true);
            list.Add(box);
        }
        text.color = cityNameColor;
        contentLayout.gameObject.SetActive(true);
        contentLayout.sizeDelta = new Vector2(list.Count * space, contentLayout.sizeDelta.y);
    }
    public void InactiveCityColor()
    {
        text.color = defaultCityColor;
    }

    public void SetValue(int value)
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i].color = i < value ? activeColor : defaultColor;
        }
    }

    public void SetStory(string title,UnityAction action)
    {
        storyTitle.text = title;
        storyButton.gameObject.SetActive(true);
        storyButton.onClick.RemoveAllListeners();
        storyButton.onClick.AddListener(ClickAction);

        void ClickAction()
        {
            storyButton.onClick.RemoveAllListeners();
            action?.Invoke();
        }
    }

    public void CloseStory() => storyButton.gameObject.SetActive(false);
}
