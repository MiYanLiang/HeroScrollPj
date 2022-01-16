using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;

public class LoginMocker : MonoBehaviour
{
    public LoginUiController Controller;

    public void OnLoginWindowOpen()
    {
        Controller.OnLoggedInAction += (a,arg0, i, i1) =>  Controller.Close();
        Controller.OnAction(LoginUiController.ActionWindows.Login);
    }

    public void OnRequestDownload()
    {
        Debug.Log("请注意，此操作一旦登录将下载玩家数据，并且覆盖本地数据。");
        Controller.OnLoggedInAction += RequestSaved;
        Controller.OnAction(LoginUiController.ActionWindows.Login);
    }

    private void RequestSaved(string username,string password ,int arrangement, int newReg) => ApiPanel.instance.SyncSaved(() => Debug.LogError("存单已下载完毕！请重启游戏。注意！请勿在正式服使用，此下载只是基本存档。玩家的通关记录和首通宝箱类的存档会有疏漏。这将导致玩家无法在正式服上传自己的最新存档而导致数据异常！"));

    public void OnRequestWarReward()
    {
        var viewBag = ViewBag.Instance()
            .ResourceDto(new ResourceDto(0))
            .WarCampaignDto(new WarCampaignDto{IsFirstRewardTaken = true,UnlockProgress = 31,WarId = 122})
            .SetValues("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1SWQiOiIxNzQ5IiwicElkIjoiMTQ2MCIsImNwIjoiMTIyIiwidHAiOiIxMDUiLCJnYyI6IjYiLCJuYmYiOjE2MTk2NzQwNTYsImV4cCI6MTYxOTY3NTg1NiwiaWF0IjoxNjE5Njc0MDU2LCJpc3MiOiJIZXJvU2Nyb2xsUGoiLCJhdWQiOiJIZXJvU2Nyb2xsQ2xpZW50In0.OrQVE7U__POfMEse0oAEg7sRY-QUAdv8u43s3qcrPco", new List<int>{112,112,112,112,132,132,112,112,112});

        ApiPanel.instance.InvokeVb(vb =>
            {
                var player = vb.GetPlayerDataDto();
                var campaign = vb.GetWarCampaignDto();
                var chests = vb.GetPlayerWarChests();
                PlayerDataForGame.instance.gbocData.fightBoxs.AddRange(chests);
                var war = PlayerDataForGame.instance.warsData.warUnlockSaveData.First(
                    c => c.warId == campaign.WarId);
                war.unLockCount = campaign.UnlockProgress;
                ConsumeManager.instance.SaveChangeUpdatePlayerData(player, 0);
            }, PlayerDataForGame.instance.ShowStringTips,
            EventStrings.Req_WarReward, viewBag);

    }

    public void ResetAccount() => Controller.OnAction(LoginUiController.ActionWindows.ResetAccount);
}
