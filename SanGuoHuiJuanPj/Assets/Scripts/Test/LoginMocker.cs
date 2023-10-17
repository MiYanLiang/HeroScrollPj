using System.Collections;
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

    public void ResetAccount() => Controller.OnAction(LoginUiController.ActionWindows.ResetAccount);
}
