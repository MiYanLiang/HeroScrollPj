using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Utl;
using Beebyte.Obfuscator;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

[Skip]public class TaoYuan : MonoBehaviour
{
    public int openJiuTanYBNums;   //开酒坛所需元宝
    public int maxZhanYiChests = 100;//战役宝箱上限
    public Button freeJiuTanAdButton;//开酒坛的免费按键
    private bool isConsumeAd;//是否消费了广告
    public JiuTanUI jiuTan;//酒坛
    public Button jinNangBtn;//锦囊
    public Button jiBanBtn;//羁绊
    public TaoYuanChestUI zhanYiChest;//战役宝箱
    public YvQueChestUI copperChest;//铜宝箱
    public YvQueChestUI goldChest;//金宝箱
    public JinNangUI jinNangUi;//锦囊
    public JiBanWindowController jiBanController;//羁绊
    [SerializeField] private RoastedChickenWindow chickenWindow;
    [SerializeField] private ChickenUiController chickenUiController;

    private RoasterChickenTrigger chickenTrigger;

    private Dictionary<TaoYuanChestUI, int> chestCostMap;//宝箱和价钱的映射表

    public void Init()
    {
        jinNangUi.Init();
        InitChests();
        InitJiBan();
        InitChicken();
    }

    private void InitJiBan()
    {
        jiBanController.Init();
        jiBanBtn.onClick.RemoveAllListeners();
        jiBanBtn.onClick.AddListener(OnShowJiBanWindow);
    }

    private void InitChicken()
    {
        chickenTrigger = new RoasterChickenTrigger();
        chickenUiController.Init(chickenTrigger);
        TimeSystemControl.instance.OnHourly += chickenTrigger.UpdateTimeNow;
        var ui = chickenUiController;
        ui.ChickenButton.onClick.RemoveAllListeners();
        ui.ChickenButton.onClick.AddListener(chickenWindow.Show);
        ui.OnUiClose.AddListener(chickenWindow.Off);
        var window = chickenWindow;
        window.OnApiSuccess.AddListener(() =>
        {
            chickenUiController.Off();
            chickenTrigger.FlagNow();
            window.Off();
        });
        window.Init();
        ui.Off();
        chickenTrigger.UpdateTimeNow();
    }

    

    //初始化宝箱的展示
    private void InitChests()
    {
        chestCostMap = new Dictionary<TaoYuanChestUI, int>
        {
            {jiuTan, DataTable.GetGameValue(3)},
            {zhanYiChest, 0},//战役宝箱不花费
            {copperChest, DataTable.GetGameValue(4)},
            {goldChest, DataTable.GetGameValue(5)}
        };
        freeJiuTanAdButton.onClick.AddListener(OnWatchAdGetJiuTan);
        foreach (var map in chestCostMap)
        {
            var chest = map.Key;
            var cost = map.Value;
            chest.chestButton.onClick.AddListener(() => OpenChest(chest));
            if (chest != zhanYiChest)
            {
                chest.value.text = cost.ToString();
                continue;
            }
            //如果是战役宝箱
            UpdateZhanYiChest(PlayerDataForGame.instance.gbocData.fightBoxs.Count); //战役宝箱数量
            continue;
        }

        jinNangBtn.onClick.AddListener(RequestJinNang);
    }

    private void UpdateZhanYiChest(int chestCount)
    {
        zhanYiChest.UpdateUi(chestCount.ToString(), maxZhanYiChests.ToString());
        zhanYiChest.chestButton.enabled = chestCount > 0;
    }

    private void OnJinNangFailed(string arg)
    {
        UIManager.instance.PlayOnClickMusic();
        PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(44));
    }

    private void RequestJinNang()
    {
        if (TimeSystemControl.instance.IsJinNangAvailable())
        {
            //ApiPanel.instance.InvokeVb(OnOpenJinNang, OnJinNangFailed, EventStrings.Req_JinNang);
            ApiPanel.instance.CallVb(OnOpenJinNang, OnJinNangFailed, EventStrings.Call_JinNang);
            return;
        }
        OnJinNangFailed(string.Empty);
    }

    public void OnShowJiBanWindow() => jiBanController.Show();

    public void OnOpenJinNang(ViewBag viewBag)
    {
        var jinNang = viewBag.GetJinNang();
        var doubleToken = viewBag.Values[0].ToString();
        var playerDto = viewBag.GetPlayerDataDto();
        ConsumeManager.instance.SaveChangeUpdatePlayerData(playerDto);
        var textColor = jinNang.Color == 1 ? ColorDataStatic.name_deepRed : ColorDataStatic.name_brown;
        jinNangUi.OnInstanceReward(jinNang.Text, textColor, jinNang.Sign, jinNang.Stamina, jinNang.YuanBao, doubleToken,
            playerDto);
    }

    public void UpdateJiuTan(bool isReady,int jiuTanCount, string countDown)
    {
        jiuTan.chestButton.gameObject.SetActive(isReady);
        jiuTan.chestButton.enabled = isReady;
        freeJiuTanAdButton.gameObject.SetActive(isReady);
        freeJiuTanAdButton.enabled = isReady;
        jiuTan.displayText.gameObject.SetActive(!isReady);//倒数文本
        jiuTan.lasting.gameObject.SetActive(!isReady);//倒数文本
        jiuTan.UpdateUi(jiuTanCount.ToString(), countDown);
    }

    private bool isWatchingJiuTanAd;

    /// <summary>
    /// 观看视频免费开启酒坛
    /// </summary>
    public void OnWatchAdGetJiuTan()
    {
        if (isWatchingJiuTanAd) return;
        isWatchingJiuTanAd = true;
        //获得酒坛消耗元宝按钮
        freeJiuTanAdButton.enabled = false;
        jiuTan.chestButton.enabled = false;

        AdAgentBase.instance.BusyRetry(() =>
        {
            int index = openJiuTanYBNums;
            openJiuTanYBNums = 0;
            isConsumeAd = true; //已成功消费广告
            OpenJiuTan();
            openJiuTanYBNums = index;
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(5));
            freeJiuTanAdButton.enabled = true;
            jiuTan.chestButton.enabled = true;
            isWatchingJiuTanAd = false;
        }, () =>
        {
            PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(6));
            freeJiuTanAdButton.enabled = true;
            jiuTan.chestButton.enabled = true;
            isConsumeAd = false;
            isWatchingJiuTanAd = false;
        });
    }

    /// <summary>
    /// 打开桃园宝箱
    /// </summary>
    /// <param name="chestUi"></param>
    public void OpenChest(TaoYuanChestUI chestUi)
    {
        AudioController0.instance.ChangeAudioClip(13);

        var chestId = -1; //宝箱在表里的Id
        var consume = 0;//消费类型,0不消费,1消费,2打开宝箱
        if (chestUi == jiuTan) //酒坛
        {
            chestId = 0;
            //如果玩家元宝小于预设的酒坛花费或开酒坛失败(时间还未到)
            if (PlayerDataForGame.instance.pyData.YuanBao < openJiuTanYBNums ||
                !TimeSystemControl.instance.IsJiuTanAvailable(SysTime.IsToday(PlayerDataForGame.instance.pyData.LastJiuTanRedeemTime)))
            {
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(0));
                AudioController0.instance.PlayAudioSource(0);
                return;
            }

            if (!isConsumeAd)
            {
                ConsumeManager.instance.DeductYuanBao(openJiuTanYBNums); //扣除酒坛元宝
                consume = 1;//消费元宝
            }
            else
            {
                //如果已消费广告就不扣除元宝
                isConsumeAd = false;
            }
        }

        if (chestUi == copperChest)
        {
            chestId = 1;
            if (!TimeSystemControl.instance.IsFreeFourDaysChestAvailable())
            {
                if (!ConsumeManager.instance.DeductYuQue(chestCostMap[chestUi]))
                {
                    PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(2));
                    return;
                }
                consume = 1; //消费玉阙
            }
        }

        if (chestUi == goldChest)
        {
            chestId = 2;
            if (!TimeSystemControl.instance.IsFreeWeeklyChestAvailable())
            {
                if (!ConsumeManager.instance.DeductYuQue(chestCostMap[chestUi]))//消费玉阙
                {
                    PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(2));
                    return;
                }
                consume = 1;
            }
            UIManager.instance.ShowOrHideGuideObj(0, false);
        }

        if (chestUi == zhanYiChest) //战役宝箱
        {
            if (PlayerDataForGame.instance.gbocData.fightBoxs.Count <= 0) //如果没有宝箱记录
            {
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(7));
                AudioController0.instance.PlayAudioSource(0);
                chestUi.chestButton.enabled = false;
                return;
            }

            consume = 2;
            chestId = PlayerDataForGame.instance.gbocData.fightBoxs[0]; //获取叠在最上面的奖励id
            PlayerDataForGame.instance.gbocData.fightBoxs.Remove(chestId); //存档移除奖励
            var chestCount = PlayerDataForGame.instance.gbocData.fightBoxs.Count; //剩余的宝箱数量
            UpdateZhanYiChest(chestCount);//改变宝箱数量
            UIManager.instance.ShowOrHideGuideObj(1, false);
        }

        //ApiPanel.instance.InvokeVb(viewBag =>
        //        OnChestRecallAction(UIManager.instance.WarChestRecallAction(viewBag), chestUi),
        //    PlayerDataForGame.instance.ShowStringTips, EventStrings.Req_WarChest,
        //    ViewBag.Instance().SetValues(chestId, consume));
        ApiPanel.instance.CallVb(viewBag =>
                OnChestRecallAction(UIManager.instance.WarChestRecallAction(viewBag), chestUi),
            PlayerDataForGame.instance.ShowStringTips, EventStrings.Call_WarChest,
            DataBag.SerializeBag(EventStrings.Call_WarChest, chestId, consume));
    }

    public void OnChestRecallAction(PlayerDataDto player, TaoYuanChestUI chestUi)
    {
        chestUi.SetChest(true); //UI，打开箱子
        AudioController0.instance.ChangeAudioClip(0);
        AudioController0.instance.PlayAudioSource(0);
    }

    public void OpenJiuTan() => OpenChest(jiuTan);

    public void CloseAllChests()
    {
        if (chestCostMap == null) return;
        foreach (var map in chestCostMap)
        {
            var chest = map.Key;
            chest.SetChest(false);
        }
    }

    private void OnDestroy()
    {
        TimeSystemControl.instance.OnHourly -= chickenTrigger.UpdateTimeNow;
    }
}
