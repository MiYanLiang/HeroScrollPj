using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameCardCityUiOperation : MonoBehaviour
{
    [SerializeField] Button Button;
    public Text Chips;
    public Color HighlightColor = ColorDataStatic.deep_green;
    public Color NormalColor = Color.white;
    public Image Enlisted;
    public Image Disabled;
    public Image Selected;
    public Image Deputy;
    public UnityEvent OnclickAction;
    public bool IsSelected { get; private set; }
    private Dictionary<States,GameObject> StateObjs
    {
        get
        {
            if (_stateObjs == null)
            {
                _stateObjs = new Dictionary<States, GameObject>
                {
                    { States.Enlisted, Enlisted.gameObject },
                    { States.Disable, Disabled.gameObject },
                    { States.Deputy, Deputy.gameObject }
                };
            }
            return _stateObjs;
        }
    }
    private Dictionary<States,GameObject> _stateObjs;

    void Awake() => Button.onClick.AddListener(OnclickAction.Invoke);

    public States State { get; private set; }
    public enum States
    {
        None,
        Enlisted,
        Deputy,
        Disable
    }

    public void Show(GameCardUi ui)
    {
        ResetUi();
        var nextLevel = ui.Card.Level + 1;
        var max = DataTable.CardLevel.ContainsKey(nextLevel) ? DataTable.CardLevel[nextLevel].ChipsConsume : 0;
        Chips.text = $"{ui.Card.Chips}/{max}";
        Chips.color = ui.Card.Chips >= max ? HighlightColor : NormalColor;
        Button.image.sprite = ui.Image.sprite;
        Enlisted.gameObject.SetActive(ui.Card.IsFight > 0);
    }

    public void OffEnlisted() => Enlisted.gameObject.SetActive(false);
    public void OffChipValue() => Chips.gameObject.SetActive(false);

    public void SetDisable(bool disable) => Disabled.gameObject.SetActive(disable);

    public void ResetUi()
    {
        State = States.None;
        Chips.gameObject.SetActive(true);
        foreach (var obj in StateObjs) obj.Value.SetActive(false);
    }

    public void SetSelected(bool isSelected)
    {
        IsSelected = Selected;
        Selected.gameObject.SetActive(isSelected);
    }

    public void SetState(States state)
    {
        if(state == States.None)
        {
            ResetUi();
            return;
        }
        foreach (var obj in StateObjs) obj.Value.gameObject.SetActive(obj.Key == state);
    }

    public void Off()
    {
        ResetUi();
        gameObject.SetActive(false);
    }
}