using System.Transactions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EnlistBtnUi : MonoBehaviour
{
    [SerializeField] private Button btn;
    [SerializeField] private Text text;
    public void SetOnClick(UnityAction onClickAction)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClickAction);
    }

    public void SetEnlist(GameCard card)
    {
        text.text = DataTable.GetStringText(card.IsFight > 0 ? 30 : 31);
        btn.interactable = true;
        btn.gameObject.SetActive(card.Level > 0);
    }

    public void SetRecall()
    {
        btn.interactable = true;
        text.text = "召回";
    }
    public void SetDisable()
    {
        btn.interactable = false;
        text.text = string.Empty;
    }
}