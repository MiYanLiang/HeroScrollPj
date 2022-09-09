using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CardUpgradeButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Text _value;
    [SerializeField] private Image mergeImage;
    [SerializeField] private Image upgradeImage;

    public void Init(UnityAction upgradeAction)
    {
        _button.onClick.AddListener(upgradeAction);
    }

    public void Set(int value,bool isMerge)
    {
        _value.text = value.ToString();
        mergeImage.gameObject.SetActive(isMerge);
        upgradeImage.gameObject.SetActive(!isMerge);
    }

    public void SetInteractable(bool enable) => _button.interactable = enable;
}