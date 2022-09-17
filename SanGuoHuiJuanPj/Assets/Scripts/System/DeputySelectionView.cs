using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DeputySelectionView : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private GameCardUi _prefab;
    [SerializeField] private Button _submitButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _closeButton;
    private List<GameCardUi> Cards { get; set; }
    private GameCard SelectedCard { get; set; }

    public void Init()
    {
        Cards = new List<GameCardUi>();
        foreach (var ui in _scrollRect.content.GetComponentsInChildren<GameCardUi>()) 
            ui.gameObject.SetActive(false);
        _closeButton.onClick.AddListener(ResetWindow);
        ResetWindow();
    }

    private void ResetWindow()
    {
        SelectedCard = null;
        _cancelButton.onClick.RemoveAllListeners();
        _submitButton.onClick.RemoveAllListeners();
        _submitButton.interactable = false;
        ClearList();
        SetDisplay(false);
    }

    private void SetDisplay(bool display) => gameObject.SetActive(display);

    public void Set(IEnumerable<GameCard> deputyGameCards, 
        UnityAction<GameCard> onCardSubmitAction,
        UnityAction onCancelDeputyAction, 
        GameCard selectedCard = null)
    {
        SelectedCard = selectedCard;
        foreach (var card in deputyGameCards) Instance(card, onCardSubmitAction);
        _cancelButton.onClick.RemoveAllListeners();
        _cancelButton.onClick.AddListener(() =>
        {
            ResetWindow();
            onCancelDeputyAction();
        });
        SetDisplay(true);
    }

    private void Instance(GameCard card, UnityAction<GameCard> onCardSubmitAction)
    {
        var ui = Instantiate(_prefab, _scrollRect.content);
        Cards.Add(ui);
        ui.Init(card);
        ui.Set(GameCardUi.CardModes.Desk);
        ui.CityOperation.OffChipValue();
        var isDeputy = card == SelectedCard;
        if (isDeputy) return;
        ui.CityOperation.OnclickAction.AddListener(() =>
        {
            SelectedCard = card;
            _submitButton.onClick.RemoveAllListeners();
            _submitButton.interactable = true;
            _submitButton.onClick.AddListener(() =>
            {
                ResetWindow();
                onCardSubmitAction.Invoke(card);
            });
            RefreshSelected(ui);
        });
    }

    private void RefreshSelected(GameCardUi ui)
    {
        foreach (var cardUi in Cards) 
            cardUi.CityOperation.SetSelected(ui == cardUi);
    }

    private void ClearList()
    {
        foreach (var cardUi in Cards.ToArray()) Remove(cardUi);

        void Remove(GameCardUi ui)
        {
            Cards.Remove(ui);
            Destroy(ui.gameObject);
        }
    }
}