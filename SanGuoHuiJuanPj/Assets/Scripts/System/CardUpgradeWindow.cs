using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CardUpgradeWindow : MonoBehaviour
{
    [SerializeField] private Button _sellBtn;
    [SerializeField] private Button _closeBtn;
    [SerializeField] private Text _sellingPrice;
    [SerializeField] private CardUpgradeButton _mergeBtn;
    public enum Upgrades
    {
        Fragment,
        Leveling,
        MaxLevel
    }
    public void Init(UnityAction onSellAction, UnityAction onMergeAction)
    {
        _sellBtn.onClick.AddListener(() =>
        {
            Display(false);
            onSellAction();
        });
        _closeBtn.onClick.AddListener(() => Display(false));
        _mergeBtn.Init(() =>
        {
            Display(false);
            onMergeAction();
        });
        _mergeBtn.Set(0, true);
        Display(false);
    }

    public void Set(int sellingPrice, int upgradeCost, Upgrades upgrade)
    {
        _mergeBtn.SetInteractable(upgrade != Upgrades.MaxLevel);
        _mergeBtn.Set(upgradeCost, upgrade == Upgrades.Fragment);
        _sellingPrice.text = sellingPrice.ToString();
        Display(true);
    }

    void Display(bool display) => gameObject.SetActive(display);
}