using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 势力旗帜UI
/// </summary>
public class ForceFlagUI : MonoBehaviour
{
    public Image forceFlag;
    public Image forceName;
    public Image selected;
    public Image panel;
    public Text lingText;
    public GameObject lingObj;
    public Text nameText;
    public Text panelText;
    public bool Selected => selected.gameObject.activeSelf;

    public void Set(int flagId, bool display = true, string nameInText = null)
    {
        var resources = GameResources.Instance;
        forceFlag.sprite = resources.ForceFlag[flagId];
        if (DataTable.Force.TryGetValue(flagId, out var force))
        {
            nameInText = force.Short;
        }
        //nameText.gameObject.SetActive(nameInText != null);
        //forceName.gameObject.SetActive(nameInText == null);
        //if (nameInText == null) forceName.sprite = resources.ForceName[flagId];
        //else nameText.text = nameInText;
        if (nameInText == null) nameInText = string.Empty;
        nameText.text = string.Join("\n", nameInText.ToCharArray().Where(c => c != default));
        nameText.gameObject.SetActive(true);
        forceName.gameObject.SetActive(false);
        gameObject.SetActive(display);
    }

    public void Hide() => gameObject.SetActive(false);

    public void Select(bool isSelected) => selected.gameObject.SetActive(isSelected);

    public void Interaction(bool enable, string text = null)
    {
        panel.gameObject.SetActive(!enable);
        panelText.text = text;
    }

    public void SetLing(int amount, bool display = true)
    {
        lingText.text = amount.ToString();
        lingObj.gameObject.SetActive(display);
    }
}