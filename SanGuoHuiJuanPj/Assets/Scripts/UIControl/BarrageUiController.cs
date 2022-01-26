using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BarrageUiController : MonoBehaviour
{
    public BarrageUi Prefab;
    public Transform[] Rows;
    public List<BarrageUi> List { get; } = new List<BarrageUi>();
    private Dictionary<Transform, List<BarrageUi>> rowMap;

    public void Init()
    {
        rowMap = Rows.ToDictionary(r => r, _ => new List<BarrageUi>());
    }
    public void PlayBarrage(string message)
    {
        var ui = List.FirstOrDefault(b => !b.gameObject.activeSelf);
        if (ui == null)
        {
            ui = Instantiate(Prefab, transform);
            List.Add(ui);
        }
        ui.Message.text = message;
        StartCoroutine(PlayBarrage(ui));
    }

    public void StopAll()
    {
        StopAllCoroutines();
        List.ForEach(ResetUi);
        foreach (var list in rowMap.Values) list.Clear();
    }

    //回收弹幕框
    IEnumerator PlayBarrage(BarrageUi ui)
    {
        var min = rowMap.Min(m => m.Value.Count);
        var row = rowMap.Where(m => m.Value.Count == min).RandomPick();
        rowMap[row.Key].Add(ui);
        ui.transform.localPosition = new Vector2(ui.transform.localPosition.x, row.Key.localPosition.y);
        ui.gameObject.SetActive(true);
        yield return new WaitForSeconds(7.5f);
        rowMap[row.Key].Remove(ui);
        ResetUi(ui);
    }

    private void ResetUi(BarrageUi ui)
    {
        ui.transform.localPosition = new Vector2(ui.transform.localPosition.x, 0);
        ui.gameObject.SetActive(false);
    }
}
