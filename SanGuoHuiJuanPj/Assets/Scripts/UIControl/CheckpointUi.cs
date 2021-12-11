using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CheckpointUi: MonoBehaviour,IPoolObject
{
    public Image CheckPointPanel;
    public Image PassImage;
    public Button SelectButton;
    public Button AttackButton;
    public Button ReportButton;
    public Image CityImage;
    public Text CityName;
    public Animator SelectedObj;
    public Image Dock;
    [SerializeField] private BattleInfo Info;
    [SerializeField] private Occupied Occupier;

    [SerializeField] private Sprite[] CitySprites;
    [SerializeField] private Sprite[] CharSprites;
    public int PointId { get; private set; } = -1;
    public int StageIndex { get; private set; } = -1;

    public void Set(VsWarStageController.SpCheckpoint cp, UnityAction selectedAction)
    {
        PointId = cp.PointId;
        StageIndex = cp.Index;
        SelectButton.onClick.RemoveAllListeners();
        SelectButton.onClick.AddListener(selectedAction);
        SetAttackButton(null);
        SetReportButton(null);
        var charSprite = CharSprites[0];
        CityName.text = cp.Title;
        SetCitySprite(cp.MaxCards);
        Info.Set(cp.MaxCards.ToString(), cp.MaxRounds.ToString(), string.Empty);
        Occupier.Set(charSprite);
        SetSelected(false);
        SetProgress(false);
        SetDock(false);
        gameObject.SetActive(true);
        Display(true);
    }

    private void SetCitySprite(int maxCards)
    {
        var citySprite = CitySprites[0];
        if (maxCards >= 12) citySprite = CitySprites[4];
        if (maxCards >= 9) citySprite = CitySprites[3];
        if (maxCards >= 6) citySprite = CitySprites[2];
        if (maxCards >= 3) citySprite = CitySprites[1];
        CityImage.sprite = citySprite;
    }
    public void SetReportButton(UnityAction reportAction) => SetButton(ReportButton, reportAction);
    public void SetAttackButton(UnityAction attAction) => SetButton(AttackButton,attAction);
    private static void SetButton(Button button, UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        button.gameObject.SetActive(action != null);
        button.onClick.AddListener(action);
    }

    public void ObjReset()
    {
        PointId = -1;
        StageIndex = -1;
        Display(false);
        SetDock(false);
        SetProgress(false); 
        gameObject.SetActive(false);
        CheckPointPanel.gameObject.SetActive(false);
        SetButton(SelectButton, null);
        SetSelected(false);
        SetReportButton(null);
        SetAttackButton(null);
    }
    
    public void SetDock(bool display) => Dock.gameObject.SetActive(display);
    public void SetProgress(bool isPass) => PassImage.gameObject.SetActive(isPass);

    public void SetSelected(bool isSelected) => SelectedObj.gameObject.SetActive(isSelected);

    public void Display(bool isShow) => CheckPointPanel.gameObject.SetActive(isShow);

    [Serializable]
    private class BattleInfo
    {
        public Text Commitment;
        public Text MaxCards;
        public Text MaxRounds;

        public void Set(string maxCards,string maxRounds,string commitment)
        {
            SetText(Commitment, commitment);
            SetText(MaxCards, maxCards);
            SetText(MaxRounds, maxRounds);
            void SetText(Text obj,string text)
            {
                if (string.IsNullOrEmpty(text))
                    obj.gameObject.SetActive(false);
                else
                {
                    obj.gameObject.SetActive(true);
                    obj.text = text;
                }
            }
        }
    }
    [Serializable]
    private class Occupied
    {
        public Image Dock;
        public Image Avatar;

        public void Set(Sprite avatar)
        {
            Dock.gameObject.SetActive(true);
            Avatar.gameObject.SetActive(true);
            Avatar.sprite = avatar;
        }

        public void Off()
        {
            Dock.gameObject.SetActive(false);
            Avatar.gameObject.SetActive(false);
        }
    }
}