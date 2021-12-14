using UnityEngine;
using UnityEngine.UI;

public class VsWarUi : MonoBehaviour
{
    public int Id;
    public Image Title;
    public Image InfoUi;
    public Image Flag;
    public Text TimeCount;
    public Text HostName;
    public Text MilitaryPower;
    public Button ClickButton;
    public GameObject HostDisplay;
    public GameObject LoseDisplay;
    public GameObject ChallengerDisplay;
    public long ExpiredTime { get; set; }
}