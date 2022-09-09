using UnityEngine;
using UnityEngine.UI;

public class TextUi : MonoBehaviour
{
    [SerializeField] private Text _title;
    [SerializeField] private Text _value;

    public void Set(string title,int value)
    {
        _title.text = title;
        _value.text = value.ToString();
    }

    public void Display(bool display) => gameObject.SetActive(display);
}