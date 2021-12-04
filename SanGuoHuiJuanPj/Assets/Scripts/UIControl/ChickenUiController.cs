using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChickenUiController : MonoBehaviour
{
    [SerializeField] private Text chickenCountdownText;
    [SerializeField] private GameObject chickenUiParent;
    public Button ChickenButton;
    public UnityEvent OnUiClose;
    private DateTime targetTime;
    private RoasterChickenTrigger trigger;
    private void StartChickenTimeCountdown(int[] timeRange)
    {
        StopAllCoroutines();
        targetTime = DateTime.Today.AddHours(timeRange[1]);
        StartCoroutine(UpdateChickenCountdownText());
    }

    private void StartUi(int[] timeRange)
    {
        chickenUiParent?.SetActive(true);
        StartChickenTimeCountdown(timeRange);
    }

    public void Off()
    {
        StopAllCoroutines();
        chickenUiParent.SetActive(false);
    }

    private IEnumerator UpdateChickenCountdownText()
    {
        var timeInterval = GetInterval();
        chickenCountdownText.text =
            TimeSystemControl.instance.TimeDisplayInChineseText(timeInterval);

        while (timeInterval.TotalSeconds >= 1)
        {
            yield return new WaitForSeconds(1);
            timeInterval = GetInterval();
            chickenCountdownText.text =
                TimeSystemControl.instance.TimeDisplayInChineseText(timeInterval);
        }

        OnUiClose?.Invoke();
        chickenUiParent.SetActive(false);

        TimeSpan GetInterval()
        {
            var t = targetTime - DateTime.Now;
            if (t < default(TimeSpan))
                t = default;
            return t;
        }
    }

    public void Init(RoasterChickenTrigger chickenTrigger)
    {
        trigger = chickenTrigger;
        trigger.OnRoasterChickenTrigger += StartUi;
    }

    private void OnDestroy()
    {
        if (trigger != null)
            trigger.OnRoasterChickenTrigger -= StartUi;
    }
}