using CorrelateLib;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class PlayerDataMock : PlayerDataForGame
{
#if UNITY_EDITOR
    [Header("从服务器获取存档")] [SerializeField] private bool downloadSavedData;
    [SerializeField] private string username;
    public static string Username { get; private set; }

    public override void Init(UnityAction onCompleteAction)
    {
        if (downloadSavedData)
        {
            Username = username;
            ApiPanel.instance.Call(bag => SetSavedFile(bag, () => base.Init(onCompleteAction)),
                _ => ShowStringTips("服务器连接失败！"),
                "Req_CallSaved", username);
            return;
        }

        base.Init(onCompleteAction);
        var isPlayerDataExist = File.Exists(AppDebugClass.plyDataString);
        var isCardDataExists = File.Exists(AppDebugClass.hstDataString);
        var isWarProgressDataExists = File.Exists(AppDebugClass.warUnlockDataString);
        var isRewardDataExists = File.Exists(AppDebugClass.gbocDataString);

        if (!isPlayerDataExist || !isCardDataExists || !isWarProgressDataExists || !isRewardDataExists)
            Debug.LogError($"找不到玩家存档，请下载玩家存档后再用重启游戏！");
        LoadSaveData.instance.LoadByJson();
    }

    private void SetSavedFile(DataBag bag, UnityAction onCompleteAction)
    {
        var playerData = bag.Get<PlayerDataDto>(0);
        var character = bag.Get<CharacterDto>(1);
        var warChestList = bag.Get<int[]>(2);
        var redeemedList = bag.Get<string[]>(3);
        var warCampaignList = bag.Get<WarCampaignDto[]>(4);
        var gameCardList = bag.Get<GameCardDto[]>(5);
        var troops = bag.Get<TroopDto[]>(6);
        SetDto(playerData, character, warChestList, redeemedList, warCampaignList, gameCardList, troops);
        onCompleteAction?.Invoke();
    }

#endif
}