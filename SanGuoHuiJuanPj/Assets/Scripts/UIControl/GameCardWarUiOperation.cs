using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameCardWarUiOperation : MonoBehaviour
{
    public Image Hp;
    public Image Lose;
    public Image Highlight;
    public Image Selected;
    public Transform StateContent;
    public GameCardUiBase baseUi { get; private set; }
    public DragController DragController { get; private set; }
    private Dictionary<States,GameObject> StateObjs
    {
        get
        {
            if (_stateObjs == null)
            {
                _stateObjs = new Dictionary<States, GameObject>
                {
                    {States.Lose,Lose.gameObject},
                    {States.Selected,Selected.gameObject}
                };
            }
            return _stateObjs;
        }
    }
    private Dictionary<States,GameObject> _stateObjs;
    private List<Image> ConditionList = new List<Image>();

    public enum States
    {
        Normal,
        Selected,
        Lose
    }

    public States State { get; private set; }

    public void Init(DragController drag)
    {
        DragController = drag;
        Selected.gameObject.SetActive(false);
        Highlight.gameObject.SetActive(false);
        Lose.gameObject.SetActive(false);
    }

    public void Show(GameCardUiBase ui)
    {
        baseUi = ui;
        Hp.fillAmount = 0;
        gameObject.SetActive(true);
    }

    public void SetHp(float hp) => Hp.fillAmount = 1f - hp;

    public void SetState(States state)
    {
        foreach (var obj in StateObjs) obj.Value.SetActive(obj.Key == state);
    }

    public void AddCondition(Image prefab)
    {
        ConditionList.Add(Instantiate(prefab, StateContent));
    }

    public void ClearCondition()
    {
        foreach (var image in ConditionList) Destroy(image.gameObject);
        ConditionList.Clear();
    }

    public void ResetUi()
    {
        Hp.fillAmount = 0;
        ClearCondition();
        foreach (var obj in StateObjs)
        {
            obj.Value.SetActive(false);
        }
    }
}