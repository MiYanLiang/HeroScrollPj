using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaYeCityStoryWindowUi : MiniWindowUI
{
    [SerializeField] private Text TitleText;
    [SerializeField] private Text IntroText;
    [SerializeField] private Image StoryParent;
    [SerializeField] private BaYeCityOptionUi OptionPrefab;
    [SerializeField] private Transform OptionContent;
    private List<BaYeCityOptionUi> baYeCityOptions;
    public void Set(BaYeManager.CityStory story,UnityAction<BaYeManager.CityStory.CityOption> onCallbackOption)
    {
        Close();
        StoryParent.gameObject.SetActive(true);
        TitleText.text = story.Title;
        IntroText.text = story.Intro;
        if (story.Type == BaYeManager.CityStory.Types.SelectionEvent)
        {
            SetOptions(story.Options, onCallbackOption);
            return;
        }

        throw new ArgumentOutOfRangeException(
            $"{nameof(BaYeCityStoryWindowUi)}.{nameof(Set)}:{story.Type} not allow to set!");
    }

    private void SetOptions(BaYeManager.CityStory.CityOption[] options,
        UnityAction<BaYeManager.CityStory.CityOption> optionCallback)
    {
        foreach (var o in options)
        {
            var ui = Instantiate(OptionPrefab, OptionContent);
            if (o.HasAdFree)
                ui.Set(o.Title, o.HasAdFree, () => optionCallback.Invoke(o));
            else ui.Set(o.Title, () => optionCallback.Invoke(o));
            baYeCityOptions.Add(ui);
        }
    }

    public void SetResult(BaYeManager.CityStory.CityResult result)
    {
        CloseStory();
        var rewardMap = new Dictionary<int, int>();
        if (result.Gold != 0) rewardMap.Add(0, result.Gold);
        if (result.BaYeExp != 0) rewardMap.Add(1, result.BaYeExp);
        if (result.CityProgress != 0) rewardMap.Add(2, result.CityProgress);
        Show(rewardMap);
        if (result.Lings.Any())
        {
            foreach (var ling in result.Lings)
            {
                InstanceElement(obj =>
                {
                    var ui = (CityStoryResultElementUI)obj;
                    ui.image.gameObject.SetActive(false);
                    ui.text.text = ling.Amt.ToString();
                    ui.flag.Set(ling.ForceId);
                });
            }
        }
    }

    private void CloseStory()
    {
        StoryParent.gameObject.SetActive(false);
        if (baYeCityOptions.Any())
            foreach (var ui in baYeCityOptions)
                Destroy(ui.gameObject);
        baYeCityOptions.Clear();
    }
}