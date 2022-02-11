using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaYeLingSelectBtn : MonoBehaviour
{
    public ForceFlagUI forceFlagUI;
    public Text text;
    public Button btn;

    public void Set(int flagId, int value)
    {
        forceFlagUI.Set(flagId);
        text.text = $"+{value}";
    }
}
