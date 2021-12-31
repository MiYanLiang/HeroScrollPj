using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RankingUi : MonoBehaviour
{
    public Text No;
    public Text HostName;
    public Text MPower;
    public Button ChallengeButton;

    public void Set(int rank, string charName,int milPower,UnityAction onclickAction)
    {
        gameObject.SetActive(true);
        No.text = rank.ToString();
        HostName.text = charName;
        MPower.text = milPower.ToString();
        ChallengeButton.onClick.RemoveAllListeners();
        if (onclickAction == default)
            ChallengeButton.interactable = false;
        else ChallengeButton.onClick.AddListener(onclickAction);
    }
}