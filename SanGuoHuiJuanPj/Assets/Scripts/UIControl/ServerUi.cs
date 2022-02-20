using UnityEngine;
using UnityEngine.UI;

public class ServerUi : MonoBehaviour
{
    public Text NameText;
    public Text ZoneText;
    public Button SelectButton;
    [SerializeField] private Image SelectedImage;
    private string Url;
    private int Zone;
    private string Name;

    public ServerUi(string url, int zone, string name)
    {
        Url = url;
        Zone = zone;
        Name = name;
        ZoneText.text = $"{zone}";
        if (string.IsNullOrWhiteSpace(name))
            NameText.text = name;
    }

    public void OnSelected(bool selected) => SelectedImage.gameObject.SetActive(selected);
}