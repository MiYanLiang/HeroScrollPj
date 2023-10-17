using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AdConsumeController : MonoBehaviour
{
    [SerializeField] private Button freeButton;
    [SerializeField] private Button ticketButton;
    [SerializeField] private Text ticketText;
    [SerializeField] private Color maxColor = Color.red;
    [SerializeField] private Color defaultTicketColor = Color.black;
    [SerializeField] private int maxTickets = 999;
    private UnityAction<bool> watchAdAction;
    private UnityAction<ViewBag> consumeAction;
    private IViewBag requestVb;
    private bool closeIfSuccess;
    private object[] requestObj;

    public void Init()
    {
        freeButton.onClick.RemoveAllListeners();
        freeButton.onClick.AddListener(OnFreeButtonInvoke);
        ticketButton.onClick.RemoveAllListeners();
        ticketButton.onClick.AddListener(OnTicketConsumeInvoke);
        defaultTicketColor = ticketText.color;
        UpdateTickets();
    }

    
    private void UpdateTickets()
    {
        var value = PlayerDataForGame.instance.pyData?.AdPass ?? 0;
        var isMax = value > maxTickets;
        ticketText.text =  isMax ? maxTickets.ToString() : value.ToString();
        ticketText.color = isMax ? maxColor : defaultTicketColor; 
        ticketButton.interactable = value > 0;
    }

    //public void SetCallBackAction(UnityAction<bool> watchAction, UnityAction<ViewBag> onSuccessConsume,
    //    IViewBag viewBag, bool closeOnSuccess)
    //{
    //    requestVb = viewBag;
    //    watchAdAction = watchAction;
    //    consumeAction = onSuccessConsume;
    //    closeIfSuccess = closeOnSuccess;
    //    UpdateTickets();
    //}
    public void SetCallBackAction(UnityAction<bool> watchAction, UnityAction<ViewBag> onSuccessConsume,
        bool closeOnSuccess, params object[] args)
    {
        requestObj = args;
        watchAdAction = watchAction;
        consumeAction = onSuccessConsume;
        closeIfSuccess = closeOnSuccess;
        UpdateTickets();
    }

    private void OnTicketConsumeInvoke()
    {
        //ApiPanel.instance.InvokeVb(vb =>
        //{
        //    var tickets = vb.GetInt(0);
        //    PlayerDataForGame.instance.UpdateFreeAdTicket(tickets);
        //    consumeAction.Invoke(vb);
        //    UpdateTickets();
        //    if (closeIfSuccess) Off();
        //}, PlayerDataForGame.instance.ShowStringTips, EventStrings.Req_ConsumeAdTicket, requestVb);
        ApiPanel.instance.CallVb(vb =>
            {
                var tickets = vb.GetInt(0);
                PlayerDataForGame.instance.UpdateFreeAdTicket(tickets);
                consumeAction.Invoke(vb);
                UpdateTickets();
                if (closeIfSuccess) Off();
            }, PlayerDataForGame.instance.ShowStringTips, EventStrings.Call_ConsumeAdTicket, requestObj);
    }

    public void ButtonsInteractive(bool isActive)
    {
        freeButton.interactable = isActive;
        ticketButton.interactable = isActive;
    }

    public void ShowWithUpdate()
    {
        UpdateTickets();
        gameObject.SetActive(true);
    }

    public void Off() => gameObject.SetActive(false);

    public void ResetUi()
    {
        ButtonsInteractive(true);
        watchAdAction = null;
        requestVb = null;
        consumeAction = null;
        closeIfSuccess = false;
    }

    private void OnFreeButtonInvoke() => AdAgentBase.instance.BusyRetry(
        () =>
        {
            watchAdAction.Invoke(true);
            if(closeIfSuccess)Off();
        }, () =>
        {
            watchAdAction.Invoke(false);
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(6));
        });
}