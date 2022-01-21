using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CorrelateLib;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public class LoadSaveData : MonoBehaviour
{
    public static LoadSaveData instance;

    [HideInInspector]
    public int firstForceId;    //传递记录初始势力 
    [HideInInspector]
    public bool isLoadingSaveData;  //标记是否在加载存档 
    [HideInInspector]
    public bool isHadSaveData;  //是否有存档 

    int isEncrypted = 0;    //记录是否加密过 
    private static readonly string ISNEEDENCRYPT = "IsNeedEncrypt";

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
        XDebug.Init();
        isLoadingSaveData = true;
        isHadSaveData = IsSaveFilesExist();
        isEncrypted = float.Parse(Application.version) < 1.9f ? PlayerPrefs.GetInt(ISNEEDENCRYPT) : 1;
    }

    /// <summary> 
    /// 是否已有完整存档 
    /// </summary> 
    /// <returns></returns> 
    private bool IsSaveFilesExist()
    {
        string filePath = AppDebugClass.playerDataString;
        string filePath00 = AppDebugClass.pyDataString;

        string filePath0 = AppDebugClass.plyDataString;
        string filePath1 = AppDebugClass.hstDataString;
        string filePath2 = AppDebugClass.warUnlockDataString;
        return (File.Exists(filePath0) || File.Exists(filePath00) || File.Exists(filePath)) && File.Exists(filePath1) && File.Exists(filePath2);
    }

    /// <summary> 
    /// 删除所有存档 
    /// </summary> 
    public void DeleteAllSaveData()
    {
        Debug.Log("删除存档");

        string filePath = AppDebugClass.plyDataString;
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        filePath = AppDebugClass.hstDataString;
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        filePath = AppDebugClass.warUnlockDataString;
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        filePath = AppDebugClass.gbocDataString;
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
#if UNITY_EDITOR
        throw new Exception("删除完成，请重启游戏！");
#endif
        SceneManager.LoadScene(0);
        Destroy(DataTable.instance.gameObject);
        Destroy(PlayerDataForGame.instance.gameObject);
        Destroy(LoadSaveData.instance.gameObject);
    }

    /// <summary> 
    /// 存档json 
    /// </summary> 
    private void SaveByJson(PlayerData save)
    {
        if (save.Exp <= 0 &&
            save.ForceId <= 0 &&
            save.DailyJinNangRedemptionCount <= 0 &&
            save.DailyJiuTanRedemptionCount <= 0 &&
            save.LastJinNangRedeemTime <= 0 &&
            save.LastJiuTanRedeemTime <= 0 &&
            save.Level <= 1 &&
            save.YuanBao <= 0 &&
            save.YvQue <= 0)
        {
            XDebug.LogError<LoadSaveData>("存档数据异常");
        }

        isLoadingSaveData = true;
        try
        {
            File.WriteAllText(AppDebugClass.plyDataString, EncryptDecipherTool.DESEncrypt(JsonConvert.SerializeObject(save)));
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
        isLoadingSaveData = false;
    }
    private void SaveByJson(HSTDataClass save)
    {
        isLoadingSaveData = true;
        try
        {
            File.WriteAllText(AppDebugClass.hstDataString, EncryptDecipherTool.DESEncrypt(JsonConvert.SerializeObject(save)));
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
        isLoadingSaveData = false;
    }
    private void SaveByJson(WarsDataClass save)
    {
        isLoadingSaveData = true;
        try
        {
            File.WriteAllText(AppDebugClass.warUnlockDataString, EncryptDecipherTool.DESEncrypt(JsonConvert.SerializeObject(save)));
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
        isLoadingSaveData = false;
    }
    private void SaveByJson(GetBoxOrCodeData save)
    {
        isLoadingSaveData = true;
        try
        {
            File.WriteAllText(AppDebugClass.gbocDataString, EncryptDecipherTool.DESEncrypt(JsonConvert.SerializeObject(save)));
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
        isLoadingSaveData = false;
    }

    /// <summary> 
    /// 备份存档,传入json字符 
    /// </summary> 
    /// <param name="pyDataStr"></param> 
    /// <param name="hSTDataStr"></param> 
    /// <param name="warsDataStr"></param> 
    private void BackupArchiveForGame(string pyDataStr, string hSTDataStr, string warsDataStr, string gbocDataStr)
    {
        //string plyDataString1 = Application.dataPath + "/StreamingAssets/data.json"; 
        //string gbocDataString1 = Application.dataPath + "/StreamingAssets/data2.json"; 
        //string hstDataString1 = Application.dataPath + "/StreamingAssets/data3.json"; 
        //string warUnlockDataString1 = Application.dataPath + "/StreamingAssets/data4.json"; 
        //File.WriteAllText(plyDataString1, pyDataStr); 
        //File.WriteAllText(gbocDataString1, hSTDataStr); 
        //File.WriteAllText(hstDataString1, warsDataStr); 
        //File.WriteAllText(warUnlockDataString1, gbocDataStr); 

        string filePath0 = AppDebugClass.plyDataString1;
        string filePath1 = AppDebugClass.hstDataString1;
        string filePath2 = AppDebugClass.warUnlockDataString1;
        string filePath3 = AppDebugClass.gbocDataString1;

        try
        {
            File.WriteAllText(filePath0, EncryptDecipherTool.DESEncrypt(pyDataStr));
            File.WriteAllText(filePath1, EncryptDecipherTool.DESEncrypt(hSTDataStr));
            File.WriteAllText(filePath2, EncryptDecipherTool.DESEncrypt(warsDataStr));
            File.WriteAllText(filePath3, EncryptDecipherTool.DESEncrypt(gbocDataStr));

            Debug.Log("存档备份成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError("存档备份失败 " + e.ToString());
        }

        //if (PlayerDataForGame.instance.acData.Phone != "") 
        //{ 
        //    UploadArchiveToServer(pyDataStr, hSTDataStr, warsDataStr, gbocDataStr); 
        //} 
    }

    /// <summary> 
    /// 读档json 
    /// </summary> 
    public void LoadByJson()
    {
        isLoadingSaveData = true;

        if (isHadSaveData)
        {
            if (!PlayerDataForGame.instance.isHadNewSaveData)
            {
                string filePath = AppDebugClass.playerDataString;
                string filePath00 = AppDebugClass.pyDataString;
                ObsoletedPlayerData save = new ObsoletedPlayerData();
                ObsoletedPyData save00 = new ObsoletedPyData();
                string jsonStr = string.Empty;

                string filePath0 = AppDebugClass.plyDataString;
                string filePath1 = AppDebugClass.hstDataString;
                string filePath2 = AppDebugClass.warUnlockDataString;
                string filePath3 = AppDebugClass.gbocDataString;

                PlayerData save0 = new PlayerData();
                HSTDataClass save1 = new HSTDataClass();
                WarsDataClass save2 = new WarsDataClass();
                GetBoxOrCodeData save3 = new GetBoxOrCodeData();

                string jsonStr0 = string.Empty;
                string jsonStr1 = string.Empty;
                string jsonStr2 = string.Empty;
                string jsonStr3 = string.Empty;

                try
                {
                    //玩家数据读档 
                    if (File.Exists(filePath) || File.Exists(filePath00))  //存在老玩家数据存档的话 
                    {
                        if (File.Exists(filePath))
                        {
                            jsonStr = File.ReadAllText(filePath);
                            jsonStr = InspectionAndCorrectionString(jsonStr, new string[] { "true}]}", "false}]}" }, filePath);
                            save = JsonConvert.DeserializeObject<ObsoletedPlayerData>(jsonStr);

                            File.Delete(filePath);
                            if (File.Exists(AppDebugClass.playerDataString1))
                            {
                                File.Delete(AppDebugClass.playerDataString1);
                            }
                            {
                                save0.Level = save.level;
                                save0.Exp = save.exp;
                                save0.YuanBao = save.yuanbao;
                                save0.YvQue = save.yvque;
                                save0.ForceId = save.forceId;
                                SaveByJson(save0);
                            }
                            {
                                save3.fightBoxs = save.fightBoxs;
                                save3.redemptionCodeGotList = new List<string>();
                                SaveByJson(save3);
                            }
                        }
                        if (File.Exists(filePath00))
                        {
                            jsonStr = File.ReadAllText(filePath00);
                            jsonStr = InspectionAndCorrectionString(jsonStr, new string[] { "true}]}", "false}]}" }, filePath00);
                            save00 = JsonConvert.DeserializeObject<ObsoletedPyData>(jsonStr);

                            File.Delete(filePath00);
                            if (File.Exists(AppDebugClass.pyDataString1))
                            {
                                File.Delete(AppDebugClass.pyDataString1);
                            }
                            {
                                save0.Level = save00.level;
                                save0.Exp = save00.exp;
                                save0.YuanBao = save00.yuanbao;
                                save0.YvQue = save00.yvque;
                                save0.ForceId = save00.forceId;
                                SaveByJson(save0);
                            }
                            {
                                save3.fightBoxs = save00.fightBoxs;
                                save3.redemptionCodeGotList = new List<string>();
                                SaveByJson(save3);
                            }
                        }

                        Debug.Log("读老版玩家档成功，删除并建立新档成功");

                        jsonStr0 = File.ReadAllText(filePath0);
                        jsonStr3 = File.ReadAllText(filePath3);
                    }
                    else
                    {
                        jsonStr0 = File.ReadAllText(filePath0);                 //读取文件 
                        if (isEncrypted != 0)
                            jsonStr0 = EncryptDecipherTool.DESDecrypt(jsonStr0);    //解密json 
                        jsonStr0 = InspectionAndCorrectionString(jsonStr0, new string[] { "}" }, filePath0);    //修正json 
                        save0 = JsonConvert.DeserializeObject<PlayerData>(jsonStr0);                          //解析json 

                        jsonStr3 = File.ReadAllText(filePath3);
                        if (isEncrypted != 0)
                            jsonStr3 = EncryptDecipherTool.DESDecrypt(jsonStr3);
                        jsonStr3 = InspectionAndCorrectionString(jsonStr3, new string[] { "}]}" }, filePath3);
                        save3 = Json.Deserialize<PrivateCodeData>(jsonStr3)?.ToNewSave();
                    }

                    //武将士兵塔数据读档 
                    jsonStr1 = File.ReadAllText(filePath1);
                    if (isEncrypted != 0)
                        jsonStr1 = EncryptDecipherTool.DESDecrypt(jsonStr1);
                    jsonStr1 = InspectionAndCorrectionString(jsonStr1, new string[] { ":0}]}" }, filePath1);
                    save1 = ArchiveCorrection(JsonConvert.DeserializeObject<HSTDataClass>(jsonStr1));

                    //关卡解锁进度数据读档 
                    jsonStr2 = File.ReadAllText(filePath2);
                    if (isEncrypted != 0)
                        jsonStr2 = EncryptDecipherTool.DESDecrypt(jsonStr2);
                    jsonStr2 = InspectionAndCorrectionString(jsonStr2, new string[] { "true}]}", "false}]}" }, filePath2);
                    save2 = ArchiveCorrection(JsonConvert.DeserializeObject<WarsDataClass>(jsonStr2));
                    Debug.Log("读档成功");

                    //备份存档 
                    BackupArchiveForGame(jsonStr0, jsonStr1, jsonStr2, jsonStr3);

                    if (isEncrypted == 0)
                    {
                        File.WriteAllText(filePath0, EncryptDecipherTool.DESEncrypt(jsonStr0));
                        File.WriteAllText(filePath1, EncryptDecipherTool.DESEncrypt(jsonStr1));
                        File.WriteAllText(filePath2, EncryptDecipherTool.DESEncrypt(jsonStr2));
                        File.WriteAllText(filePath3, EncryptDecipherTool.DESEncrypt(jsonStr3));
                        isEncrypted = 1;
                        PlayerPrefs.SetInt(ISNEEDENCRYPT, isEncrypted);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("读档失败 " + e.ToString());

                    filePath0 = AppDebugClass.plyDataString1;
                    filePath1 = AppDebugClass.hstDataString1;
                    filePath2 = AppDebugClass.warUnlockDataString1;
                    filePath3 = AppDebugClass.gbocDataString1;

                    if (File.Exists(AppDebugClass.playerDataString1))
                    {
                        jsonStr = File.ReadAllText(AppDebugClass.playerDataString1);
                        jsonStr = InspectionAndCorrectionString(jsonStr, new string[] {"true}]}", "false}]}"},
                            AppDebugClass.playerDataString1);
                        save = JsonConvert.DeserializeObject<ObsoletedPlayerData>(jsonStr);
                        {
                            save0.Level = save.level;
                            save0.Exp = save.exp;
                            save0.YuanBao = save.yuanbao;
                            save0.YvQue = save.yvque;
                            save0.ForceId = save.forceId;
                            SaveByJson(save0);
                        }
                        if (save3 != null)
                        {
                            save3.fightBoxs = save.fightBoxs;
                            save3.redemptionCodeGotList = new List<string>();
                            SaveByJson(save3);
                        }

                        File.Delete(AppDebugClass.playerDataString1);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    else
                    {
                        if (File.Exists(AppDebugClass.pyDataString1))
                        {
                            jsonStr = File.ReadAllText(AppDebugClass.pyDataString1);
                            jsonStr = InspectionAndCorrectionString(jsonStr, new string[] {"true}]}", "false}]}"},
                                AppDebugClass.pyDataString1);
                            save00 = JsonConvert.DeserializeObject<ObsoletedPyData>(jsonStr);
                            {
                                save0.Level = save00.level;
                                save0.Exp = save00.exp;
                                save0.YuanBao = save00.yuanbao;
                                save0.YvQue = save00.yvque;
                                save0.ForceId = save00.forceId;
                                SaveByJson(save0);
                            }
                            {
                                if(save3!=null)
                                {
                                    save3.fightBoxs = save00.fightBoxs;
                                    save3.redemptionCodeGotList = new List<string>();
                                    SaveByJson(save3);
                                }
                            }
                            File.Delete(AppDebugClass.pyDataString1);
                            if (File.Exists(filePath00))
                            {
                                File.Delete(filePath00);
                            }
                        }
                        else
                        {
                            jsonStr0 = File.ReadAllText(filePath0);
                            if (isEncrypted != 0)
                                jsonStr0 = EncryptDecipherTool.DESDecrypt(jsonStr0);
                            save0 = JsonConvert.DeserializeObject<PlayerData>(jsonStr0);

                            jsonStr3 = File.ReadAllText(filePath3);
                            if (isEncrypted != 0)
                                jsonStr3 = EncryptDecipherTool.DESDecrypt(jsonStr3);
                            save3 = Json.Deserialize<PrivateCodeData>(jsonStr3).ToNewSave();
                        }
                    }

                    jsonStr1 = File.ReadAllText(filePath1);
                    if (isEncrypted != 0)
                        jsonStr1 = EncryptDecipherTool.DESDecrypt(jsonStr1);
                    jsonStr1 = InspectionAndCorrectionString(jsonStr1, new string[] {":0}]}"}, filePath1);
                    save1 = ArchiveCorrection(JsonConvert.DeserializeObject<HSTDataClass>(jsonStr1));

                    jsonStr2 = File.ReadAllText(filePath2);
                    if (isEncrypted != 0)
                        jsonStr2 = EncryptDecipherTool.DESDecrypt(jsonStr2);
                    jsonStr2 = InspectionAndCorrectionString(jsonStr2, new string[] {"true}]}", "false}]}"}, filePath2);
                    save2 = ArchiveCorrection(JsonConvert.DeserializeObject<WarsDataClass>(jsonStr2));

                    Debug.Log("读取备份存档成功");
                }

                //存档数据提取到游戏中 
                SetGamePlayerBasicData(save0, save1, save2, save3);
            }
            //初始化新手指引 
            GamePref.SetIsFirstPlay(false);
        }
        else
        {
            CreatePlayerDataSave();

            LoadByJson();
        }
        //设置游戏音乐开关标签 
        if(AudioController0.instance)
        {
            AudioController0.instance.MusicSwitch(GamePref.PrefMusicPlay);
        }

        isLoadingSaveData = false;
    }

    //武将塔等数据存档修正 
    private HSTDataClass ArchiveCorrection(HSTDataClass save)
    {
        var isNeedSaveDate = true;
        save.heroSaveData = ResolveData(save.heroSaveData, GameCardType.Hero);
        save.towerSaveData = ResolveData(save.towerSaveData, GameCardType.Tower);
        save.trapSaveData = ResolveData(save.trapSaveData, GameCardType.Trap);
        if (isNeedSaveDate)
        {
            SaveByJson(save);
        }
        return save;

        List<GameCard> ResolveData(List<GameCard> data,GameCardType cardType)
        {
            return data.Where(card =>
            {
                switch (cardType)
                {
                    case GameCardType.Hero:
                        return DataTable.Hero[card.id].IsProduce > 0;
                    case GameCardType.Tower:
                        return DataTable.Tower[card.id].IsProduce > 0;
                    case GameCardType.Trap:
                        return DataTable.Trap[card.id].IsProduce > 0;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cardType), cardType, null);
                }
            }).ToList();
        }
    }
    //战役数据存档修正 
    private WarsDataClass ArchiveCorrection(WarsDataClass save)
    {
        bool isNeedSaveDate = false;
        //战役存档修正 
        int nowDataCount = save.warUnlockSaveData.Count;
        int jsonDataCount = DataTable.War.Count;
        if (nowDataCount < jsonDataCount)
        {
            isNeedSaveDate = true;
            for (int i = nowDataCount; i < jsonDataCount; i++)
            {
                UnlockWarCount unlock = new UnlockWarCount();
                unlock.warId = i;
                unlock.unLockCount = 0;
                unlock.isTakeReward = false;
                save.warUnlockSaveData.Add(unlock);
            }
        }

        var duplicated = save.warUnlockSaveData.GroupBy(w => w.warId, w => w).Where(w => w.Count() > 1).ToList();
        for (int i = 0; i < duplicated.Count; i++)
        {
            var w = duplicated[i];
            var elements = w.ToList();
            for (int j = 1; j < elements.Count; j++)
            {
                var e = elements[j];
                save.warUnlockSaveData.Remove(e);
            }
        }
        if (isNeedSaveDate)
        {
            SaveByJson(save);
            //Debug.Log("战役数据存档修正"); 
        }
        return save;
    }
    //玩家宝箱与兑换码存档修正 

    /// <summary> 
    /// 游戏数据初次存档 
    /// </summary> 
    /// <returns></returns> 
    public void CreatePlayerDataSave()
    {
        //////////////////////////////////////////////////////////////////////////////////////// 
        PlayerData pySaveData = new PlayerData();
        pySaveData.Level = 1;
        pySaveData.Exp = 0;
        pySaveData.YvQue = DataTable.ResourceConfig[0].NewPlayerValue;
        pySaveData.YuanBao = DataTable.ResourceConfig[1].NewPlayerValue;
        pySaveData.Stamina = DataTable.ResourceConfig[2].NewPlayerValue;
        pySaveData.ForceId = firstForceId;   //暂给初始势力
        pySaveData.LastGameVersion = float.Parse(Application.version);
        SaveByJson(pySaveData);
        //////////////////////////////////////////////////////////////////////////////////////// 
        GetBoxOrCodeData gbocSaveData = new GetBoxOrCodeData();
        gbocSaveData.fightBoxs = new List<int>();
        gbocSaveData.redemptionCodeGotList = new List<string>();
        SaveByJson(gbocSaveData);
        //////////////////////////////////////////////////////////////////////////////////////// 
        HSTDataClass hstSaveData = new HSTDataClass
        {
            heroSaveData = new List<GameCard>(),
            towerSaveData = new List<GameCard>(),
            trapSaveData = new List<GameCard>()
        };
        DataTable.PlayerInitialConfig[pySaveData.ForceId].InitialHero.ToList().ForEach(id =>
        {
            var card = hstSaveData.heroSaveData.GetOrInstance(id, GameCardType.Hero, 1);
            card.isFight = id == 0 || id == 1 ? 1 : 0;
        });
        DataTable.PlayerInitialConfig[pySaveData.ForceId].InitialTower.ToList().ForEach(id =>
        {
            var card = hstSaveData.towerSaveData.GetOrInstance(id, GameCardType.Tower, 1);
            card.isFight = id == 0 ? 1 : 0;
        });
        SaveByJson(hstSaveData);
        /////////////////////////////////////////////////////////////////////////////////////////// 
        WarsDataClass warsSaveData = new WarsDataClass();
        warsSaveData.warUnlockSaveData = new List<UnlockWarCount>();
        for (int i = 0; i < DataTable.War.Count; i++)
        {
            UnlockWarCount unlock = new UnlockWarCount();
            unlock.warId = i;
            unlock.unLockCount = 0;
            unlock.isTakeReward = false;
            warsSaveData.warUnlockSaveData.Add(unlock);
        }
        SaveByJson(warsSaveData);
        //////////////////////////////////////////////////////////////////////////////////////////// 
        isEncrypted = 1;
        PlayerPrefs.SetInt(ISNEEDENCRYPT, isEncrypted);
        GamePref.SetIsFirstPlay(true);

        isHadSaveData = true;
        Debug.Log("初创存档成功");
    }

    /// <summary> 
    /// 提取存档数据到游戏中 
    /// </summary> 
    /// <param name="save">玩家数据存档</param> 
    /// <param name="save1">卡牌数据存档</param> 
    /// <param name="save2">战役数据存档</param> 
    public void SetGamePlayerBasicData(PlayerData save, HSTDataClass save1, WarsDataClass save2, GetBoxOrCodeData save3)
    {
        PlayerDataForGame.instance.pyData = save;
        PlayerDataForGame.instance.hstData = save1;
        PlayerDataForGame.instance.warsData = save2;
        if(save3!=null) PlayerDataForGame.instance.gbocData = save3;
        PlayerDataForGame.instance.isHadNewSaveData = true;
    }

    /// <summary> 
    /// 存储游戏 
    /// </summary> 
    /// <param name="indexFun">默认0：全部存储，1：存储pyData，2：存储hstData，3：存储warsData，4：存储gbocData，5:py + war(霸业)，6:py + gboc，7:py + hst，8:BaYe</param> 
    public void SaveGameData(int indexFun = 0)
    {
        if (PlayerDataForGame.instance.isNeedSaveData)
        {
            switch (indexFun)
            {
                case 0:
                    SaveByJson(PlayerDataForGame.instance.pyData);
                    SaveByJson(PlayerDataForGame.instance.hstData);
                    SaveByJson(PlayerDataForGame.instance.warsData);
                    SaveByJson(PlayerDataForGame.instance.gbocData);
                    break;
                case 1:
                    SaveByJson(PlayerDataForGame.instance.pyData);
                    break;
                case 2:
                    SaveByJson(PlayerDataForGame.instance.hstData);
                    break;
                case 3:
                    SaveByJson(PlayerDataForGame.instance.warsData);
                    break;
                case 4:
                    SaveByJson(PlayerDataForGame.instance.gbocData);
                    break;
                case 5:
                    SaveByJson(PlayerDataForGame.instance.pyData);
                    SaveByJson(PlayerDataForGame.instance.warsData);
                    break;
                case 6:
                    SaveByJson(PlayerDataForGame.instance.pyData);
                    SaveByJson(PlayerDataForGame.instance.gbocData);
                    break;
                case 7:
                    SaveByJson(PlayerDataForGame.instance.pyData);
                    SaveByJson(PlayerDataForGame.instance.hstData);
                    break;
                default:
                    break;
            }
            PlayerDataForGame.instance.isNeedSaveData = false;
        }
#if UNITY_EDITOR
        XDebug.Log<LoadSaveData>($"存档[{indexFun}]完毕！");
#endif
    }

    /// <summary> 
    /// 检查与矫正字符串(修复末尾) 
    /// </summary> 
    /// <param name="jsonStr"></param> 
    /// <param name="endStr"></param> 
    /// <param name="fileName"></param> 
    /// <returns></returns> 
    private string InspectionAndCorrectionString(string jsonStr, string[] endStr, string fileName)
    {
        //Debug.Log(jsonStr); 
        try
        {
            for (int i = 0; i < endStr.Length; i++)
            {
                int lastIndex = jsonStr.IndexOf(endStr[i]);
                if (lastIndex != -1)
                {
                    lastIndex = lastIndex + endStr[i].Length;
                    if (lastIndex < jsonStr.Length)
                    {
                        jsonStr = jsonStr.Remove(lastIndex);
                        //Debug.Log(fileName + "：存档格式损坏修复"); 
                    }
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("fileName: " + e.ToString());
        }
        //Debug.Log(jsonStr); 
        return jsonStr;
    }

    private class PrivateCodeData
    {
        //战役宝箱
        public List<int> fightBoxs;
        //兑换码
        public List<RCode> redemptionCodeGotList;

        public GetBoxOrCodeData ToNewSave()
        {
            var list = DataTable.RCode.Join(redemptionCodeGotList.Where(r=>r.isGot), r => r.Key, c => c.id, (r, _) => r.Value.Code).ToList();
            return new GetBoxOrCodeData
                {fightBoxs = fightBoxs, redemptionCodeGotList = list};
        }
    }
}