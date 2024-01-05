using CorrelateLib;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class JinNangUI: MonoBehaviour
{
    public Button continueBtn;
    //public Button doubleAdBtn;
    public AdConsumeController AdConsume;
    public ScrollRect rewardsView;
    public JinNangRewardUI yuanBao;
    public JinNangRewardUI yvQue;
    public JinNangRewardUI exp;
    public JinNangRewardUI stamina;
    public Image window;
    public Text continueText;
    public Text jinNangContent;
    public Text characterName;
    public Text JinNangQuota;
    private PlayerDataDto playerDataDto;

    public void Init() => AdConsume.Init();

    public void OnInstanceReward(string content, Color contentColor, string character, int staminaValue, int yuanBaoValue,
        string token, PlayerDataDto playerData)
    {
        playerDataDto = playerData;
        AudioController0.instance.ChangeAudioClip(11);
        AudioController0.instance.PlayAudioSource(0);

        continueText.gameObject.SetActive(false);
        rewardsView.gameObject.SetActive(false);
        AdConsume.Off();
        //doubleAdBtn.gameObject.SetActive(false);
        jinNangContent.gameObject.SetActive(true);
        window.gameObject.SetActive(true);
        window.color = Opacity(window.color, 0);
        characterName.color = Opacity(characterName.color, 0);
        jinNangContent.text = content;
        jinNangContent.color = Opacity(contentColor, 0);
        JinNangQuota.text = $"今日次数：{playerDataDto.DailyJinNangRedemptionCount}/10";
        characterName.text = character;
        window.DOFade(1, 0.5f).OnComplete(() =>
        {
            characterName.DOFade(1, 1.5f);
            jinNangContent.DOFade(1, 1.5f).OnComplete(() =>
            {
                continueText.gameObject.SetActive(true);
                continueBtn.onClick.AddListener(() => OnRewardContinueAction(yuanBaoValue, staminaValue, token));
            });
        });
        gameObject.SetActive(true);
    }

    private void OnRewardContinueAction(int yuanBaoValue, int staminaValue, string token)
    {
        UIManager.instance.PlayOnClickMusic();
        characterName.DOFade(0, 1f);
        jinNangContent.DOFade(0, 1f).OnComplete(() =>
        {
            //展示奖励内容
            DisplayReward(yuanBaoValue, staminaValue);
            continueBtn.onClick.RemoveAllListeners();
            //点击背景领取并退出锦囊
            continueBtn.onClick.AddListener(() =>
            {
                UIManager.instance.PlayOnClickMusic();
                ConsumeManager.instance.SaveChangeUpdatePlayerData(playerDataDto);
                PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(43));
                continueBtn.onClick.RemoveAllListeners();
                gameObject.SetActive(false);
            });
            //点击广告双倍奖励

            AdConsume.SetCallBackAction(success =>
            {
                if (success) SuccessAdConsume(null);
                else PlayerDataForGame.instance.ShowStringTips("广告异常...");
            }, SuccessAdConsume, false, 1, token);
            AdConsume.ShowWithUpdate();
            void SuccessAdConsume(ViewBag _)
            {
                //ApiPanel.instance.InvokeVb(OnSuccessDoubleReward, PlayerDataForGame.instance.ShowStringTips,
                //    EventStrings.Req_TokenResources, ViewBag.Instance().SetValue(token));
                ApiPanel.instance.HttpCallVb(OnSuccessDoubleReward, PlayerDataForGame.instance.ShowStringTips,
                    EventStrings.Call_TokenResources, DataBag.SerializeBag(EventStrings.Call_TokenResources, token));
            }
        });
    }

    private void OnSuccessDoubleReward(ViewBag vb)
    {
        UIManager.instance.PlayOnClickMusic();
        //背景按钮无效
        var re = vb.GetResourceDto();
        playerDataDto = vb.GetPlayerDataDto();
        ConsumeManager.instance.SaveChangeUpdatePlayerData(playerDataDto);
        DisplayReward(re.YuanBao * 2, re.Stamina * 2);
        continueBtn.enabled = true;
        AdConsume.Off();
        PlayerDataForGame.instance.ShowStringTips("奖励翻倍!");
    }

    private void DisplayReward(int yuanBaoAmt, int staminaAmt)
    {
        rewardsView.gameObject.SetActive(true);
        yuanBao.gameObject.SetActive(yuanBaoAmt > 0);
        yuanBao.amount.text = "×" + yuanBaoAmt;
        stamina.gameObject.SetActive(staminaAmt > 0);
        stamina.amount.text = "×" + staminaAmt;
    }

    private Color Opacity(Color baseColor,float opacity) => new Color(baseColor.r, baseColor.g, baseColor.b, opacity);
}