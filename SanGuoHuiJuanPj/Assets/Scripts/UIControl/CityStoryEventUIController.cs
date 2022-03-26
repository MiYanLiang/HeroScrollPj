using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beebyte.Obfuscator;
using UnityEngine;

public class CityStoryEventUIController : MonoBehaviour
{
    public BaYeCityEventUI[] eventList;

    [SkipRename]public void OnClickAudioPaly() 
    {
        AudioController0.instance.RandomPlayGuZhengAudio();//播放随机音效
    }

    public void ResetUi()
    {
        if (GameSystem.CurrentScene != GameSystem.GameScene.MainScene) return;//如果不是主场景不更新。
        var baYe = PlayerDataForGame.instance.baYe;
        var mgr = PlayerDataForGame.instance.BaYeManager;
        for (var i = 0; i < eventList.Length; i++)
        {
            var ui = eventList[i];
            var cityId = i + 1;
            if (!baYe.cityStories.TryGetValue(cityId, out var storyId))
            {
                ui.CloseStory();
                continue;
            }

            var cityStory = mgr.InstanceCityStory(cityId, storyId);
            ui.SetStory(cityStory.Title, () =>
            {
                ui.CloseStory();
                mgr.OnCityStoryClick(cityStory);
            });
        }
    }
}
