using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TextImageUi :MonoBehaviour
{
    public Text Txt;
    public Image Img;

    public void Set(int value, Sprite image) => Set(value.ToString(), image);
    public void Set(float value, Sprite image) => Set(value.ToString("F"), image);
    public void Set(string text, Sprite image)
    {
        Txt.text = text;
        if(Img) Img.sprite = image;
        Show();
    }

    public void Show()=>gameObject.SetActive(true);

    public void Off() => gameObject.SetActive(false);
}