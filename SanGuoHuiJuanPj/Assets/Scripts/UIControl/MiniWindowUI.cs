using System;
using System.Collections.Generic;
using UnityEngine;

public class MiniWindowUI : MonoBehaviour
{
    public MiniWindowElementUI prefab;
    public Sprite[] rewardImages;
    public Transform listView;
    private List<MiniWindowElementUI> items = new List<MiniWindowElementUI>();
    public virtual void Init()
    {
        foreach (var ui in listView.GetComponentsInChildren<MiniWindowElementUI>(true))
            ui.gameObject.SetActive(false);
        listView.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public virtual void Show(Dictionary<int, int> rewardMap,Action<MiniWindowElementUI> extraSetAction = null)
    {
        if (items.Count > 0) items.ForEach(element =>
        {
            if (element != null && element.gameObject != null)
                Destroy(element.gameObject);
        });
        items.Clear();
        foreach (var set in rewardMap)
        {
            if (set.Value == 0) continue;
            InstanceElement(ui =>
            {
                var prefix = set.Value > 0 ? "+" : string.Empty;
                ui.image.sprite = rewardImages[set.Key];
                ui.text.text = $"{prefix}{set.Value}";
                extraSetAction?.Invoke(ui);
            });
        }
        listView.gameObject.SetActive(true);
        gameObject.SetActive(true);
    }

    public void InstanceElement(Action<MiniWindowElementUI> setUiAction)
    {
        var item = Instantiate(prefab, listView);
        items.Add(item);
        item.gameObject.SetActive(true);
        setUiAction.Invoke(item);
    }

    public virtual void Close()
    {
        listView.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}