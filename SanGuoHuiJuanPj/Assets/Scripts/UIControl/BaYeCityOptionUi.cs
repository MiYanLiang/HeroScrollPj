using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaYeCityOptionUi: MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private Button SelectButton;
    [SerializeField] private AdConsumeController Ad;

    public void Set(string title, UnityAction onAction)
    {
        Ad.gameObject.SetActive(false);
        Set(title, false, onAction);
    }

    public void Set(string title, bool hasAd, UnityAction onSelectAction)
    {
        Ad.gameObject.SetActive(hasAd);
        text.text = title;
        SelectButton.onClick.RemoveAllListeners();
        SelectButton.onClick.AddListener(onSelectAction);
        if (!hasAd) return;
        Ad.Init();
        Ad.SetCallBackAction(success =>
        {
            if (success)
                onSelectAction.Invoke();
        }, _ => onSelectAction.Invoke(), ViewBag.Instance().SetValue(0), true);
    }
}