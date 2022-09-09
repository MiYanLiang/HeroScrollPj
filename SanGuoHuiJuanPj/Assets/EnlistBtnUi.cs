using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EnlistBtnUi : MonoBehaviour
{
    [SerializeField] private Button btn;
    [SerializeField] private Text text;
    public void Init(UnityAction onClickAction) => btn.onClick.AddListener(onClickAction);
    public void Set(bool enlist) => text.text = enlist ? "出战" : "回城";
}