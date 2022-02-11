using System.Collections.Generic;
using UnityEngine.UI;

public class WarMiniWindowUI : MiniWindowUI
{
    public Text expeditionText;
    public ForceFlagUI flagPrefab;//战令id为负数

    /// <summary>
    /// 0= Gold,
    /// 1= Exp,
    /// 2= Chest,
    /// 3= YuanBao,
    /// 4= YvQue,
    /// </summary>
    /// <param name="rewardMap">0= Gold, 1= Exp, 2= Chest, 3= YuanBao, 4= YvQue,</param>
    public override void Show(Dictionary<int, int> rewardMap)
    {
        flagPrefab.gameObject.SetActive(false);
        base.Show(rewardMap);
        rewardMap.TryGetValue(2, out int chestAmt);//宝箱
        expeditionText.gameObject.SetActive(PlayerDataForGame.instance.WarType ==
            PlayerDataForGame.WarTypes.Expedition && chestAmt > 0);//宝箱大于0才会显示去桃园打开宝箱的提示。并且是战役才有
    }

    public void Show(WarReward reward, bool isBaYe)
    {
        base.Show(reward.ToRewardMap(isBaYe));
        foreach (var ling in reward.Ling)
        {
            var flag = Instantiate(flagPrefab, listView);
            flag.Set(ling.Key);
            flag.SetLing(ling.Value);
        }

        expeditionText.gameObject.SetActive(
            PlayerDataForGame.instance.WarType == PlayerDataForGame.WarTypes.Expedition &&
            reward.Chests.Count > 0); //宝箱大于0才会显示去桃园打开宝箱的提示。并且是战役才有
    }
}