using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Assets.Scripts.Utl;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StartSceneToServerCS : MonoBehaviour
{
#if UNITY_EDITOR
    public bool isSkipLogin;//是否跳过登录
    public bool isSkipInitBattle;//是否跳过初始战斗
#endif

    public static StartSceneToServerCS instance;

    public Button StartButton;

    //删除所有
    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        LoadSaveData.instance.DeleteAllSaveData();
    }

    /// <summary> 
    /// 清除帐户 
    /// </summary> 
    public void ClearAccountData()
    {
        PlayerDataForGame.instance.acData.Username = string.Empty;
        PlayerDataForGame.instance.acData.LastUpdate = default;
        PlayerDataForGame.instance.isNeedSaveData = true;
        LoadSaveData.instance.SaveGameData(1);
        PlayerPrefs.DeleteAll();
#if UNITY_EDITOR
        throw new Exception("清除账号完成,请重启游戏！");
#endif
        LoginGameInfoFun();
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public void Init()
    {
        //InfoButtonOnClickFun();//旧按键方法取消
        StartButton.gameObject.SetActive(false);
        PlayerDataForGame.instance.acData.Username = GamePref.Username;
        PlayerDataForGame.instance.acData.Password = GamePref.Password;
        PlayerDataForGame.instance.acData.Phone = PlayerPrefs.GetString(GamePref.PhoneNumber);
        LoginGameInfoFun();
    }

    /// <summary> 
    /// 游戏登陆方法初始化 
    /// </summary> 
    public void LoginGameInfoFun()
    {
        //如果有存档或初始剧情已播或是用户名已注册，不播剧情
//        if (!string.IsNullOrWhiteSpace(PlayerDataForGame.instance.acData.Username)
//#if UNITY_EDITOR
//            || isSkipInitBattle
//#endif
//            || StartSceneUIManager.instance.isPlayedStory)
//        {
            var login = GameSystem.LoginUi;
            login.OnAction(LoginUiController.ActionWindows.Login);
            login.OnLoggedInAction += OnLoggedIn;
            return;
        //}
        ////先播放剧情 
        //StartButton.gameObject.SetActive(true);
        //StartButton.onClick.RemoveAllListeners();
        //var startSceneUi = GetComponent<StartSceneUIManager>();
        //StartButton.onClick.AddListener(startSceneUi.DontHaveSaveDataPlayStory);
    }

    private void OnLoggedIn(string username, string password, int arrangement, int newReg)
    {
        if (newReg > 0)
        {
            GamePref.SetIsFirstPlay(true);
            PlayerDataForGame.instance.pyData = new PlayerData();
            PlayerDataForGame.instance.hstData = new HSTDataClass();
            PlayerDataForGame.instance.gbocData = new GetBoxOrCodeData();
            PlayerDataForGame.instance.warsData = new WarsDataClass();
        }

        var usr = username;
        GamePref.SetUsername(usr);
        var pwdEmpty = string.IsNullOrEmpty(password);
        if(!pwdEmpty)
        {
            GamePref.FlagDeviceReg(username);
            GamePref.SetPassword(password);
            PlayerDataForGame.instance.acData.Password = password;
        }
        PlayerDataForGame.instance.acData.Username = usr;
        PlayerDataForGame.instance.Arrangement = arrangement;

        GameSystem.InitGameDependencyComponents();
        GameSystem.LoginUi.Close();
        GameSystem.Instance.BeginAllOnlineServices();
        StartSceneUIManager.instance.LoadingScene(GameSystem.GameScene.MainScene, true);
    }
}