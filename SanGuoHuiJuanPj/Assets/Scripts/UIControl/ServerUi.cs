using UnityEngine;
using UnityEngine.UI;

public class ServerUi : MonoBehaviour
{
    public Button SelectButton;
    [SerializeField] private Text NameText;
    [SerializeField] private Text ZoneText;
    [SerializeField] private Image NewImage;
    [SerializeField] private Image SelectedImage;

    public void Init(int zone, string title, bool isNew)
    {
        ZoneText.text = $"{zone}";
        NameText.text = title;
        NewImage.gameObject.SetActive(isNew);
        gameObject.SetActive(true);
    }

    public void OnSelected(bool selected) => SelectedImage.gameObject.SetActive(selected);
}