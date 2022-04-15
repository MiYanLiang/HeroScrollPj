using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaYeTester : MonoBehaviour
{
    [SerializeField] private BaYeTradeLingWindowUi TradeWindow;

    private void Start() => TradeWindow.Init(2, (fId, price) => print($"测试战令[{fId}]+1, 价钱={price}"));
    public void OnGenerateStoryEvents() => BaYeManager.instance.GenerateBaYeStoryEvents();
    public void AddExp(int exp = 10) => BaYeManager.instance.AddExp(-10, exp);

    public void ShowTradeZhanling()
    {
        TradeWindow.SetTradeZhanLing(new[] { 1, 4 });
    }
}
