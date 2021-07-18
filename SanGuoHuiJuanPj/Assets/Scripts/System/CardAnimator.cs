using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CardAnimator
{
    //攻击行动方式0-适用于-主动塔,远程兵
    public static IEnumerator RangePreAction(FightCardData card, float readyTime)
    {
        var obj = card.cardObj;
        yield return obj.transform.DOScale(new Vector3(1.15f, 1.15f, 1), readyTime).SetAutoKill(false).OnComplete(() => obj.transform.DOPlayBackwards()).WaitForCompletion();
    }

    public static float forward = 0.2f;   //去
    public static float stepBack = 0.3f;   //回
    public static float charge = 0.4f; //等
    public static float hit = 0.1f;  //去
    public static float posFloat;
    //近战移动方式
    public static IEnumerator MeleePreAction(FightCardData card, FightCardData target)
    {
        var ui = card.cardObj;
        var targetPos = target.cardObj.transform.position;
        var targetHalfPos = new Vector3(targetPos.x, targetPos.y - FightForManager.instance.posFloat, targetPos.z);
        var vec = new Vector3(
            targetHalfPos.x,
            targetHalfPos.y + (card.isPlayerCard ? (-1 * posFloat) : posFloat) * FightForManager.instance.oneDisY,
            targetHalfPos.z
        );
        yield return ui.transform.DOMove(targetHalfPos, FightController.instance.attackShakeTimeToGo * forward)
            .SetEase(Ease.Unset).WaitForCompletion();
        yield return ui.transform.DOMove(ui.transform.position, FightController.instance.attackShakeTimeToGo * stepBack)
            .SetEase(Ease.Unset).WaitForCompletion();
        yield return ui.transform.DOMove(vec, FightController.instance.attackShakeTimeToGo * charge).SetEase(Ease.Unset)
            .WaitForCompletion();
        yield return ui.transform.DOMove(targetHalfPos, FightController.instance.attackShakeTimeToGo * hit)
            .SetEase(Ease.Unset).WaitForCompletion();
    }

}