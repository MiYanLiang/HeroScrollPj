using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class VsForceSelectorUi : ForceSelectorUi
{
    public int[] LimitedList { get; set; } = Array.Empty<int>();
    public void RegLimitedForce(IEnumerable<int> limitedForces)
    {
        LimitedList = limitedForces.ToArray();
        foreach (var f in Data)
        {
            var forceId = f.Key;
            var btn = BtnData[forceId];
            if (!LimitedList.Contains(forceId) || !btn.IsInteractable()) continue;
            var flag = f.Value;
            flag.Interaction(false, "出征");
            btn.interactable = false;
        }
    }

    public override void OnSelected(int forceId = -1, bool disableUi = false)
    {
        if (LimitedList.Contains(forceId)) return;
        base.OnSelected(forceId, disableUi);
    }
}