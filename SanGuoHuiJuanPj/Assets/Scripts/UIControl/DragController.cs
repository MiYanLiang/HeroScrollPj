using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UI;

/// <summary>
/// 卡牌拖动控件
/// </summary>
public class DragController : DragObjectSender<FightCardData>,IPoolObject
{
    protected override FightCardData ThisObj => Card;
    private FightCardData Card;
    
    public void Init(FightCardData card)
    {
        Card = card;
        base.Init();
    }

    public void ObjReset() => Card = null;
}