using System;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaYeCityOptionUi: MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private Button SelectButton;
    [SerializeField] private AdConsumeController Ad;
    [SerializeField] private CostUi CostWindow;

    public void Set(BaYeManager.CityStory.CityOption op, UnityAction<bool> onAction)
    {
        gameObject.SetActive(true);
        Ad.gameObject.SetActive(op.HasAdFree);
        text.text = op.Title;
        SelectButton.onClick.RemoveAllListeners();
        SelectButton.onClick.AddListener(() => onAction.Invoke(false));
        CostWindow.Set(op);
        if (!op.HasAdFree) return;
        Ad.Init();
        Ad.SetCallBackAction(success =>
        {
            if (success)
                onAction.Invoke(true);
        }, _ => onAction.Invoke(true), ViewBag.Instance().SetValue(0), true);
    }

    [Serializable]
    private class CostUi
    {
        [SerializeField] private FlagUi flagUi;
        [SerializeField] private GoldUi goldUi;
        [SerializeField] private Image Window;

        public void Off()=> Window.gameObject.SetActive(false);
        public void Set(BaYeManager.CityStory.CityOption op)
        {
            Window.gameObject.SetActive(true);
            goldUi.Off();
            flagUi.Off();
            if(op.Gold!=0)
            {
                var prefix = op.Gold > 0 ? "-" : "+";
                goldUi.Set($"{prefix}{op.Gold}");
            }

            var ling = op.Lings.FirstOrDefault();
            if (ling!=null)
            {
                var prefix = ling.Amt > 0 ? "-" : "+";
                flagUi.Set(ling.ForceId, $"{prefix}{ling.Amt}");
            }
        }

        [Serializable]private class FlagUi
        {
            public ForceFlagUI Flag;
            public Text Text;
            public Image Window;

            public void Set(int forceId,string text)
            {
                Flag.Set(forceId);
                Text.text = text;
                Text.gameObject.SetActive(true);
                Window.gameObject.SetActive(true);
            }

            public void Off()
            {
                Flag.Hide();
                Text.gameObject.SetActive(false);
                Window.gameObject.SetActive(false);
            }

        }
        [Serializable]private class GoldUi
        {
            public Image Image;
            public Text Text;

            public void Set(string text)
            {
                Image.gameObject.SetActive(true);
                Text.text = text;
                Text.gameObject.SetActive(true);
            }

            public void Off()
            {
                Image.gameObject.SetActive(false);
                Text.gameObject.SetActive(false);
            }
        }
    }
}