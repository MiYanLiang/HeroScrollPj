using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

public class AdTestUi : MonoBehaviour
{
    public AdManager manager;
    public Text RatioText;
    public Text CurrentText;
    public ScrollRect ScrollRect;
    public Text AdPrefab;
    private List<Text> List = new List<Text>();
    private float deltaTime = 0;
    [SerializeField] private float UpdateSec = 2f;

    void Update()
    {
        deltaTime += Time.deltaTime;
        if (deltaTime < UpdateSec) return;
        deltaTime = 0;
        Refresh();
    }
    
    private void Refresh()
    {
        if (manager == null || !manager.IsInit) return;
        CurrentText.text = string.Empty;
        RatioText.text = $"{AdManager.Ads.Pangle}:{AdManager.Ads.Unity}";
        CurrentText.text = manager.Current.ToString();
        RefreshList();
    }

    AdControllerBase[] AdControllers;
    private void RefreshList()
    {
        if (AdControllers != null)
        {
            for (var i = 0; i < AdControllers.Length; i++)
            {
                var controller = AdControllers[i];
                SetAdText(List[i], controller.Name, controller.Status.ToString());
            }
            return;
        }

        AdControllers = new AdControllerBase[]
        {
            manager.PangleController, 
            //manager.UnityAdController
        };
        foreach (var ad in AdControllers)
        {
            var ui = Instantiate(AdPrefab, ScrollRect.content.transform);
            ui.gameObject.SetActive(true);
            SetAdText(ui, ad.Name, ad.StatusDetail);
            List.Add(ui);
        }
    }

    private void SetAdText(Text ui, string adName, string status) => ui.text = $"【{adName}】:{status}";
}
