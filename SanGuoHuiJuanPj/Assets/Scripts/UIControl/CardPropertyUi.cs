using UnityEngine;
using UnityEngine.UI;

public class CardPropertyUi : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _valueText;
    [SerializeField] private Text _addOnText;

    public void SetTitle(string title) => _titleText.text = title;
    public void Set(int value,int addOn = 0)
    {
        _valueText.text = value.ToString();
        var hasAddOn = addOn > 0;
        _addOnText.gameObject.SetActive(hasAddOn);
        _addOnText.text = $"+{addOn}";
        Display(true);
    }

    public void Display(bool display) => gameObject.SetActive(display);
}