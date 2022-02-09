using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StartWarUi : MonoBehaviour
{
    const string ButtonTrigger = "isShow";
    [SerializeField] private Animator Animator;
    [SerializeField] private Button Button;
    [SerializeField] private GameObject CircleFlicker;
    private bool isBusy = false;

    public void Display(bool display)
    {
        if (!gameObject.activeInHierarchy)
        {
            Animator.SetBool(ButtonTrigger, display);
            Button.gameObject.SetActive(display);
            Button.interactable = display;
            CircleFlicker.gameObject.SetActive(display);
        }
        else
        {
            if (isBusy) return;
            StartCoroutine(OnAnimation(display));
        }
    }

    private IEnumerator OnAnimation(bool display)
    {
        isBusy = true;
        Animator.SetBool(ButtonTrigger, display);
        yield return new WaitForSeconds(0.7f);
        Button.gameObject.SetActive(display);
        Button.interactable = display;
        CircleFlicker.gameObject.SetActive(display);
        isBusy = false;
    }

    public void ResetAllClicks() => Button.onClick.RemoveAllListeners();
    public void SetClickEvent(UnityAction action) => Button.onClick.AddListener(action);

    public void ResetUi()
    {
        Display(false);
        ResetAllClicks();
    }
}