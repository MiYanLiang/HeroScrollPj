﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SanGuoTvUi : MonoBehaviour
{
    public Text[] contents;
    public Text[] clocks;
    private float lastTime;
    public int Secs = 60;
    
    private void Start()
    {
        GenerateReport();
    }

    void Update()
    {
        lastTime += Time.deltaTime;
        if (lastTime <= Secs)return;
        lastTime = 0;
        GenerateReport();
    }

    //每个半个小时更新报道一次
    public void GenerateReport()
    {
        var now = DateTime.Now;
        var result = DataTable.BaYeTv
            .Select(map =>
            {
                int.TryParse(map.Value[4], out var format);
                return new Report
                {
                    Id = map.Key,
                    Weight = int.Parse(map.Value[1]), //权重
                    Text = map.Value[2], //文本
                    Time = map.Value[3], //时间
                    Format = format //格式

                };
            }).Where(r => r.Time.IsTableTimeInRange(now))
            .Pick(contents.Length).ToList();
        for (int i = 0; i < result.Count; i++)
        {
            clocks[i].text = SystemTimer.instance.CurrentClock;
            contents[i].text = GetFormattedText(result[i]);
        }
    }

    private string GetFormattedText(Report report)
    {
        object[] names = DataTable.BaYeName.Select(map => new Character
        {
            Name = map.Value[2],
            Weight = int.Parse(map.Value[1])
        }).Pick(report.Format).Select(r => r.Name).ToArray();
        return string.Format(report.Text, names);
    }

    private class Report : IWeightElement
    {
        public int Id{ get; set; }
        public int Weight { get; set; }
        public string Text { get; set; }
        public string Time { get; set; }
        public int Format { get; set; }
    }

    private class Character : IWeightElement
    {
        public string Name { get; set; }
        public int Weight { get; set; }
    }
}
