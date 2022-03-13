
using System;
using System.Diagnostics.Contracts;
using UnityEngine;
using UnityEngine.UI;

public class QqCaller : MonoBehaviour
{
    [SerializeField] private Text Message;
    /****************
    *
    * 发起添加群流程。群号：【英雄绘卷】内测群(216385360) 的 key 为： EWQip3HzKNPESwQ_0P7JvU_yGkTe0SlU
    * 调用 joinQQGroup(EWQip3HzKNPESwQ_0P7JvU_yGkTe0SlU) 即可发起手Q客户端申请加群 【英雄绘卷】内测群(216385360)
    *
    * @param key 由官网生成的key
    * @return 返回true表示呼起手Q成功，返回false表示呼起失败
    ******************/
    //public boolean joinQQGroup(String key)
    //{
    //    Intent intent = new Intent();
    //    intent.setData(Uri.parse("mqqopensdkapi://bizAgent/qm/qr?url=http%3A%2F%2Fqm.qq.com%2Fcgi-bin%2Fqm%2Fqr%3Ffrom%3Dapp%26p%3Dandroid%26jump_from%3Dwebapi%26k%3D" + key));
    //    // 此Flag可根据具体产品需要自定义，如设置，则在加群界面按返回，返回手Q主界面，不设置，按返回会返回到呼起产品界面    //intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
    //    try
    //    {
    //        startActivity(intent);
    //        return true;
    //    }
    //    catch (Exception e)
    //    {
    //        // 未安装手Q或安装的版本不支持
    //        return false;
    //    }
    //}
    [SerializeField] private string QQKey = "Rb-uAH1GXBy2TgmrsHxIGGZlREBQRUXX";
    private AndroidJavaClass unityPlayerJavaClass;
    private AndroidJavaObject qqJo;

    private void Start()
    {
#if !UNITY_EDITOR
        unityPlayerJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");//UnityPlayer class
        var currentAvtivity = unityPlayerJavaClass.GetStatic<AndroidJavaObject>("currentActivity");//get activity
        var qqJavaClass = new AndroidJavaClass("com.icefoxz.qqcaller.QQGroup");//qqGroup class
        qqJo = qqJavaClass.CallStatic<AndroidJavaObject>("instance", currentAvtivity);//call instance
#endif
    }

    public void TestJoinQQ()
    {
        var msg = string.Empty;
        SetMessage(msg);
        try
        {
            var success = JoinQQGroup();
            msg = $"尝试调用QQ群：{success}";
            SetMessage(msg);
        }
        catch (Exception e)
        {
            msg = $"尝试调用QQ群：{e}";
            SetMessage(msg);
            throw;
        }
    }

    public bool JoinQQGroup()
    {
        return qqJo.Call<bool>("joinQQGroup", QQKey);
    }

    public void OnClickJoinQQGroup()
    {
#if !UNITY_EDITOR
        var success = JoinQQGroup();
        if (!success)
            PlayerDataForGame.instance.ShowStringTips("尝试打开QQ失败，请确保已安装QQ客户端。");
#endif
        PlayerDataForGame.instance.ShowStringTips("安卓手机才能打开QQ群。");
    }
    private void SetMessage(string msg)
    {
        if (Message)
            Message.text = msg;
        Debug.Log(msg);
    }
}