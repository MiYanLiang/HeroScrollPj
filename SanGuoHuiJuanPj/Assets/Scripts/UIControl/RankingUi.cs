using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RankingUi : MonoBehaviour
{
    public Text No;
    public Text HostName;
    public Text MPower;
    public Button ChallengeButton;
    public Image FlagImage;
    public Text FlagText;

    public void Set(int rank, string charName, int milPower, Sprite flagImg, string flagText, bool isHost,
        UnityAction onclickAction)
    {
        gameObject.SetActive(true);
        No.text = rank.ToString();
        HostName.text = charName;
        if (!isHost) HostName.color = Color.white;
        MPower.text = milPower.ToString();
        ChallengeButton.onClick.RemoveAllListeners();
        if (onclickAction == default)
            ChallengeButton.interactable = false;
        else ChallengeButton.onClick.AddListener(onclickAction);
        FlagImage.sprite = flagImg;
        FlagText.text = flagText;
    }
}