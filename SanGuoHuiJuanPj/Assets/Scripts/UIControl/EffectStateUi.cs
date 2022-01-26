using UnityEngine;
using UnityEngine.UI;

public class EffectStateUi : MonoBehaviour
{
    public Image Image;
    public Animator Animator;
    public Canvas Canvas;

    public void ImageFading(float rate) => Image.color = new Color(Image.color.r, Image.color.g, Image.color.b, rate);
}