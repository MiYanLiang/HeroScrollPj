using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class YesButtonUi:MonoBehaviour
{
    [SerializeField] Button Button;
    [SerializeField] InputField Input;
    [SerializeField] private string YesText;

    public void Set(UnityAction confirmedAction)
    {
        ResetUi();
        Input.onValueChanged.AddListener(OnValueChange);
        Button.onClick.AddListener(() =>
        {
            Button.onClick.RemoveAllListeners();
            confirmedAction?.Invoke();
        });
        gameObject.SetActive(true);
    }

    private void OnValueChange(string text)
    {
        if (!YesText.Equals(text)) return;
        SetButtonInteraction(true);
    }

    private void SetButtonInteraction(bool enable)
    {
        Button.interactable = enable;
        Input.gameObject.SetActive(false);
    }

    private void ResetInput()
    {
        Input.text = string.Empty;
        Input.gameObject.SetActive(true);
        Input.onValueChanged.RemoveAllListeners();
    }

    public void ResetUi()
    {
        Button.onClick.RemoveAllListeners();
        Button.interactable = false;
        ResetInput();
    }
}