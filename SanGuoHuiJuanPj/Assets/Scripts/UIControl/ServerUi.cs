using System;
using UnityEngine;
using UnityEngine.UI;

public class ServerUi : MonoBehaviour
{
    public Button SelectButton;
    [SerializeField] private Text NameText;
    [SerializeField] private Text ZoneText;
    [SerializeField] private Image NewImage;
    [SerializeField] private Image SelectedImage;
    public bool IsActive { get; private set; }
    public int Zone { get; private set; }
    public string Title { get; private set; }
    public string ApiUrl { get; private set; }

    public void Init(int zone, string title, bool isNew, DateTime startDate, DateTime closeDate, string apiUrl)
    {
        ZoneText.text = $"{zone}";
        Zone = zone;
        Title = title;
        NameText.text = title;
        NewImage.gameObject.SetActive(isNew);
        var now = DateTime.Now;
        var isStartServing = startDate == default || startDate < now;
        var isCloseServing = closeDate != default && now > closeDate;
        if(!isStartServing)
        {
            SelectButton.onClick.RemoveAllListeners();
        }
        IsActive = isStartServing && !isCloseServing;
        SelectButton.interactable = IsActive;
        gameObject.SetActive(!isCloseServing);
        ApiUrl = apiUrl;
    }

    public void OnSelected(bool selected) => SelectedImage.gameObject.SetActive(selected);

}