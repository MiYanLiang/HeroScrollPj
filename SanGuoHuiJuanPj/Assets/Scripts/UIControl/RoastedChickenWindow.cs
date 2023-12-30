using System;
using System.Collections.Generic;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RoastedChickenWindow : MonoBehaviour
{
    public ChickenConsumeUi[] ChickenConsumeUis;
    public AdConsumeController AdConsumeUi;
    public Button CloseButton;
    public UnityEvent OnApiSuccess;

    public void Init()
    {
        for (var i = 0; i < ChickenConsumeUis.Length; i++)
        {
            var consumeId = i + 2;
            SetYuQueConsumeUi(consumeId, ChickenConsumeUis[i]);
        }
        AdConsumeUi.Init();
        AdConsumeUi.SetCallBackAction(success =>
        {
            if (success) RequestChickenApi(1);
        }, OnSuccessRequestChicken, false, 2);//ticketsAction = 2
        CloseButton.onClick.RemoveAllListeners();
        CloseButton.onClick.AddListener(Off);
        Off();
    }

    private void SetYuQueConsumeUi(int id, ChickenConsumeUi ui)
    {
        var ck = DataTable.Chicken[id];
        ui.Id = ck.Id;
        ui.YuQue.text = ck.YuQueCost.ToString();
        ui.Stamina.text = ck.Stamina.ToString();
        ui.Button.onClick.RemoveAllListeners();
        ui.Button.onClick.AddListener(() => OnConsumeYuQue(ui));
    }

    //商店购买体力 
    private void OnConsumeYuQue(ChickenConsumeUi ui)
    {
        var chickenId = ui.Id;
        AudioController0.instance.ChangeAudioClip(13);
        ButtonsInteractive(false);
        if (chickenId == 1) throw XDebug.Throw<RoastedChickenWindow>($"消费玉阙按键[{ui.Id}]异常,不允许请求免费战令！");
        RequestChickenApi(ui.Id);
    }

    private void RequestChickenApi(int chickenId)
    {
        //ApiPanel.instance.InvokeVb(OnSuccessRequestChicken, msg =>
        //    {
        //        PlayerDataForGame.instance.ShowStringTips(msg);
        //        ButtonsInteractive(true);
        //    }, EventStrings.Req_Chicken,
        //    ViewBag.Instance().SetValue(chickenId));
        ApiPanel.instance.HttpCallVb(OnSuccessRequestChicken, msg =>
            {
                PlayerDataForGame.instance.ShowStringTips(msg);
                ButtonsInteractive(true);
            }, EventStrings.Call_Chicken,
            DataBag.SerializeBag(EventStrings.Call_Chicken, chickenId));
    }

    private void OnSuccessRequestChicken(ViewBag bag)
    {
        var ck = bag.GetChicken();
        var player = bag.GetPlayerDataDto();
        ConsumeManager.instance.SaveChangeUpdatePlayerData(player);
        PlayerDataForGame.instance.ShowStringTips(string.Format(DataTable.GetStringText(51), ck.Stamina));
        //UIManager.instance.GetCkChangeTimeAndWindow();
        AudioController0.instance.ChangeAudioClip(25);
        AudioController0.instance.PlayAudioSource(0);
        OnApiSuccess?.Invoke();
    }

    private void ButtonsInteractive(bool activate)
    {
        foreach (var ui in ChickenConsumeUis) ui.Button.interactable = activate;
        AdConsumeUi.ButtonsInteractive(activate);
    }

    [Serializable] public class ChickenConsumeUi
    {
        public int Id;
        public Button Button;
        public Transform Parent;
        public Text YuQue;
        public Text Stamina;
    }

    public void Off() => gameObject.SetActive(false);
    public void Show()
    {
        AdConsumeUi.ShowWithUpdate();
        gameObject.SetActive(true);
    }
}