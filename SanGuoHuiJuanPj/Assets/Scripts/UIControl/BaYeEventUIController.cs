using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beebyte.Obfuscator;
using UnityEngine;

public class BaYeEventUIController : MonoBehaviour
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
        foreach (var baYeCityStory in baYe.cityStories)
        {
            var ui = eventList[baYeCityStory.Key];
            var cityStory = mgr.InstanceCityStory(baYeCityStory.Key, baYeCityStory.Value);
            ui.SetStory(() => mgr.OnCityStoryClick(cityStory));
        }
    }
}
