using System;
using UnityEngine;
using UnityEngine.UI;

public class VsWarUi : MonoBehaviour
{
    public int Id { get; set; }
    public WarInfoUi WarInfo;
    public ChallengeInfoUi ChallengeUi;
    public RankingUi Board;

    public void SetUi()
    {
        throw new NotImplementedException();
    }
    [Serializable]
    public class RankingUi
    {
        public GameObject Obj;
        public Text No;
        public Text HostName;
        public Text MPower;
        public Button ChallengeButton;
    }
    [Serializable]
    public class ChallengeInfoUi
    {
        public GameObject Obj;
        public Text Rank;
        public Image ChallengeImage;
        public Text TimerUi;
    }
    [Serializable]
    public class WarInfoUi
    {
        public GameObject Obj;
        public Image TextImage;
    }
}