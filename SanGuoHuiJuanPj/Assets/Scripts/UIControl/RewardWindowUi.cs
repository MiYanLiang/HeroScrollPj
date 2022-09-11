using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RewardWindowUi : MonoBehaviour
{
    [SerializeField] private Transform WindowTransform;
    [SerializeField] private Button OkButton;
    [SerializeField] private Transform Content;
    [SerializeField] private RewardObjectUi RewardPrefab;
    [SerializeField] private ScrollRect ScrollRect;
    private List<RewardObjectUi> pool = new List<RewardObjectUi>();

    public void ShowReward(DeskReward reward, float waitTime,UnityAction afterAnimAction)
    {
        StopAllCoroutines();
        pool.ForEach(o=>o.Off());
        GenerateUi(reward);
        gameObject.SetActive(true);
        StartCoroutine(OpenRewardsWindows(waitTime,afterAnimAction));
    }

    private void GenerateUi(DeskReward reward)
    {
        if(reward.YuanBao > 0) GetUiFromPool().SetYuanBao(reward.YuanBao);
        if(reward.YuQue > 0) GetUiFromPool().SetYuQue(reward.YuQue);
        if(reward.Exp > 0) GetUiFromPool().SetExp(reward.Exp);
        if(reward.Stamina > 0) GetUiFromPool().SetStamina(reward.Stamina);
        if(reward.AdPass > 0) GetUiFromPool().SetAdPass(reward.AdPass);
        foreach (var card in reward.Cards)
        {
            GetUiFromPool().SetCard(GameCard.Instance(
                cardId: card.cardId, 
                type: card.cardType, 
                level: 0, arouse: 0, 
                deputy1Id: -1, deputy1Level: -1, 
                deputy2Id: -1, deputy2Level: -1, 
                deputy3Id: -1, deputy3Level: -1, 
                deputy4Id: -1, deputy4Level: -1, 
                chips: card.cardChips));
        }
    }

    private RewardObjectUi GetUiFromPool()
    {
        var ui = pool.FirstOrDefault(o => !o.gameObject.activeSelf);
        if (!ui)
        {
            ui = Instantiate(RewardPrefab, Content);
            pool.Add(ui);
        }
        return ui;
    }

    //展示奖品 
    IEnumerator OpenRewardsWindows(float startTime, UnityAction afterAnimAction)
    {
        WindowTransform.gameObject.SetActive(false);
        OkButton.onClick.RemoveAllListeners();
        OkButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(startTime);
        WindowTransform.gameObject.SetActive(true);
        ScrollRect.horizontalNormalizedPosition = 0f;
        yield return new WaitForSeconds(1f);
        OkButton.gameObject.SetActive(true);
        ScrollRect.DOHorizontalNormalizedPos(1f, 1f);
        OkButton.onClick.AddListener(Off);
        yield return new WaitForSeconds(1f);
        afterAnimAction?.Invoke();
    }

    private void Off()
    {
        gameObject.SetActive(false);
        pool.ForEach(o=>o.Off());
    }
}