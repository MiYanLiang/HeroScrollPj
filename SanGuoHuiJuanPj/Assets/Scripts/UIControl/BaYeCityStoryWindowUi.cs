using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BaYeCityStoryWindowUi : MiniWindowUI
{
    [SerializeField] private StoryWindowUi StoryWindow;
    [SerializeField] private ResultWindowUi ResultWindow;
    [SerializeField] private BaYeCityOptionUi OptionPrefab;
    [SerializeField] private Transform OptionContent;
    private List<BaYeCityOptionUi> baYeCityOptions = new List<BaYeCityOptionUi>();

    public override void Init()
    {
        base.Init();
        var ops = OptionContent.GetComponentsInChildren<BaYeCityOptionUi>();
        foreach (var ui in ops) ui.gameObject.SetActive(false);
        CloseStory();
        CloseResult();
    }

    public void SetStory(BaYeManager.CityStory story,UnityAction<BaYeManager.CityStory.CityOption,bool> onCallbackOption)
    {
        CloseResult();
        gameObject.SetActive(true);
        StoryWindow.Set(story.Title,story.Intro);
        if (story.Type == BaYeManager.CityStory.Types.SelectionEvent)
        {
            SetOptions(story.Options, onCallbackOption);
            return;
        }

        throw new ArgumentOutOfRangeException(
            $"{nameof(BaYeCityStoryWindowUi)}.{nameof(SetStory)}:{story.Type} not allow to set!");
    }

    private void SetOptions(BaYeManager.CityStory.CityOption[] options,
        UnityAction<BaYeManager.CityStory.CityOption,bool> optionCallback)
    {
        ClearOptionUiElements();
        foreach (var o in options)
        {
            var ui = Instantiate(OptionPrefab, OptionContent);

            ui.Set(o, isConsumeAd => optionCallback.Invoke(o, isConsumeAd));
            baYeCityOptions.Add(ui);
        }
    }

    public void SetResult(BaYeManager.CityStory.CityResult result)
    {
        CloseStory();
        ResultWindow.Set(result.Brief);
        var rewardMap = new Dictionary<int, int>();
        if (result.Gold != 0) rewardMap.Add(0, result.Gold);
        if (result.BaYeExp != 0) rewardMap.Add(1, result.BaYeExp);
        if (result.CityProgress != 0) rewardMap.Add(2, result.CityProgress);
        
        Show(rewardMap, u =>
        {
            var ui = (CityStoryResultElementUI)u;
            ui.Init();
        });
        if (result.Lings.Any())
        {
            foreach (var ling in result.Lings)
            {
                InstanceElement(obj =>
                {
                    var ui = (CityStoryResultElementUI)obj;
                    ui.HideImage();
                    var forceId = ling.ForceId;
                    ui.text.text = ling.Amt.ToString();
                    ui.flag.Set(forceId);
                });
            }
        }
    }

    private void CloseStory()
    {
        StoryWindow.Close();
        ClearOptionUiElements();
    }

    private void ClearOptionUiElements()
    {
        if (baYeCityOptions.Any())
            foreach (var ui in baYeCityOptions)
                Destroy(ui.gameObject);
        baYeCityOptions.Clear();
    }
    private void CloseResult()
    {
        Close();
        ResultWindow.Close();
    }

    [Serializable]private class ResultWindowUi
    {
        public Image Window;
        public Text Brief;

        public void Set(string brief)
        {
            Window.gameObject.SetActive(true);
            Brief.text = brief;
        }

        public void Close() => Window.gameObject.SetActive(false);
    }
    [Serializable]private class StoryWindowUi
    {
        public Text TitleText;
        public Text IntroText;
        public Image Window;

        public void Set(string title,string intro)
        {
            IntroText.text = intro;
            TitleText.text = title;
            Window.gameObject.SetActive(true);
        }

        public void Close() => Window.gameObject.SetActive(false);
    }
}