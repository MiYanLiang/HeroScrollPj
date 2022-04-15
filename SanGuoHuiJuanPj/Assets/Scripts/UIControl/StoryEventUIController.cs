using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beebyte.Obfuscator;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryEventUIController : MonoBehaviour
{
    public List<StoryEventPoint> storyEventPoints;
    public List<BaYeStoryUi> eventTypePrefabs;
    [Header("对应上面事件的音效，-1为为随机古筝，其余负数为无音效。")]
    public List<int> eventTypeAudioIds;
    private Dictionary<int, StoryEventPoint> points;

    public void ResetUi()
    {
        if(GameSystem.CurrentScene != GameSystem.GameScene.MainScene)return;//如果不是主场景不更新。
        storyEventPoints.ForEach(b =>
        {
            if (!b) return;
            b.gameObject.SetActive(false);
            if (b.content)
                Destroy(b.content.gameObject);
        });
        var storyMap = PlayerDataForGame.instance.baYe.storyMap.Where(kv => kv.Value.Type != 0).ToDictionary(s=>s.Key,s=>s.Value);//类型0：无事件
        points = new Dictionary<int, StoryEventPoint>();
        for (int i = 0; i < storyEventPoints.Count; i++)
        {
            var point =  storyEventPoints[i];
            var isContainEvent = storyMap.ContainsKey(i);
            point.gameObject.SetActive(isContainEvent);
            if (!isContainEvent) continue;
            var sEvent = storyMap[i];
            var eventIndex = sEvent.Type - 1;//由于0为无事件，所以第一个是事件1的图标
            if (sEvent.Type == 9)
            {
                point.content = Instantiate(eventTypePrefabs[4], point.transform);
            }
            else point.content = Instantiate(eventTypePrefabs[eventIndex],point.transform);
            if (sEvent.Type == 3) //如果是战斗事件
            {
                InitWarEventUi(point.content,sEvent.WarId);
            }
            points.Add(i, point);
        }
    }

    private void InitWarEventUi(BaYeStoryUi baYeStoryUi, int warId)
    {
        var ui = (BaYeWarStoryUi)baYeStoryUi;
        var (level, color) = BaYeManager.instance.StoryWarEventMap[warId];
        ui.Set(level, color);
    }

    [SkipRename]public void OnStoryEventClick(int eventPoint)
    {
        var sEvent = PlayerDataForGame.instance.baYe.storyMap[eventPoint];
        OnClickAudioPlay(sEvent.Type);
        BaYeManager.instance.OnBaYeWarEventPointSelected(BaYeManager.EventTypes.Story, eventPoint);
        if ((BaYeManager.StoryEventTypes) sEvent.Type == BaYeManager.StoryEventTypes.讨伐)
            return; //讨伐事件ui在第一次点击是不会销毁的。
        var point = points[eventPoint];
        Destroy(point.content.gameObject);
        point.gameObject.SetActive(false);
    }

    private void OnClickAudioPlay(int type)
    {
        if (type > eventTypeAudioIds.Count) return;
        var audioId = eventTypeAudioIds[type - 1];
        if (audioId < -1) return;
        if (audioId == -1)
        {
            AudioController0.instance.RandomPlayGuZhengAudio();
            return;
        }
        AudioController0.instance.ForcePlayAudio(audioId);
    }
}