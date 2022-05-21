using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ZhanLingUi : MonoBehaviour
{
    public int MaxValue = 100;
    public Color HightColor = new Color(170, 0, 0);
    public Color NormalColor = Color.black;
    private int _value;
    public Text ValueText;
    public Text MaxText;
    public Text EffectText;
    public Text Countdown;

    public void UpdateUi(int value)
    {
        _value = value;
        InternalUpdateUi();
    }

    public void UpdateCountdown(string text)
    {
        Countdown.text = text;
        InternalUpdateUi();
    }

    private void InternalUpdateUi()
    {
        ValueText.fontStyle = FontStyle.Normal;
        MaxText.fontStyle = FontStyle.Normal;
        ValueText.text = _value.ToString();
        ValueText.color = NormalColor;
        MaxText.color = NormalColor;
        if (_value < MaxValue) return;
        Countdown.text = string.Empty;
        ValueText.fontStyle = FontStyle.Bold;
        ValueText.color = HightColor;
        MaxText.color = HightColor;
        MaxText.fontStyle = FontStyle.Bold;
    }

    public void ShowEffect(int value)
    {
        StopAllCoroutines();
        if (value == 0) return;
        var isIncrease = value > 0;
        EffectText.gameObject.SetActive(false);
        EffectText.text = (isIncrease ? "+" : string.Empty) + value;
        EffectText.color = isIncrease ? ColorDataStatic.deep_green : Color.red;
        if (isIncrease) PlayerDataForGame.instance.ShowStringTips(string.Format(DataTable.GetStringText(25), value));
        StartCoroutine(ShowStaminaDeduction());
    }

    IEnumerator ShowStaminaDeduction()
    {
        EffectText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        EffectText.gameObject.SetActive(false);
    }
}