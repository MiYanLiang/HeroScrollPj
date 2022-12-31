using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaYeForceSelectorUi : ForceSelectorUi
{
    public override void Init(PlayerDataForGame.WarTypes warType)
    {
        base.Init(warType);
        UpdateZhanLing();
    }

    public void UpdateZhanLing()
    {
        var bayeLing = BaYeManager.instance.BaYeLing;
        PlayerDataForGame.instance.WarForceMap[PlayerDataForGame.WarTypes.Baye] = bayeLing;
        OnSelected(bayeLing, true, true); //<----限制一个势力玩法
        //<----战令玩法
        //var baYe = PlayerDataForGame.instance.baYe;
        //foreach (var pair in Data)
        //{
        //    var forceId = pair.Key;
        //    var flagUi = pair.Value;
        //    baYe.zhanLingMap.TryGetValue(forceId, out var amount);
        //    flagUi.SetLing(amount);
        //}
    }
}

