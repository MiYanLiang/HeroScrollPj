using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public abstract class DragController : MonoBehaviour
{
    protected const string Mouse_X = "Mouse X";
    protected const string Mouse_Y = "Mouse Y";
    public int PosIndex => FightCard.posIndex;  //上场位置记录
    public bool IsLocked { get;  set; } = false; //记录是否是上阵卡牌
    
    protected Transform CardListTransform { get; private set; }
    /// <summary>
    /// Scroll View上的Scroll Rect组件
    /// </summary>
    protected ScrollRect ScrollRect { get; private set; }

    protected FightCardData FightCard { get; private set; }

    public virtual void Init(FightCardData fightCard, Transform cardListTrans, ScrollRect scrollRect)
    {
        CardListTransform = cardListTrans;
        ScrollRect = scrollRect;
        FightCard = fightCard;
    }

    public abstract void BeginDrag(BaseEventData data);
    public abstract void OnDrag(BaseEventData data);
    public abstract void EndDrag(BaseEventData data);

    public abstract void ResetPos(int pos);

    public abstract void Disable();
}

public class CardForDrag : DragController
{
    public override void BeginDrag(BaseEventData data)
    {
        if (IsLocked)
        {
            return;
        }

        Vector2 touchDeltaPosition = Vector2.zero;
#if UNITY_EDITOR
        float delta_x = Input.GetAxis(Mouse_X);
        float delta_y = Input.GetAxis(Mouse_Y);
        touchDeltaPosition = new Vector2(delta_x, delta_y);

#elif UNITY_ANDROID || UNITY_IPHONE
        touchDeltaPosition = Input.GetTouch(0).deltaPosition;  
#endif
        if (transform.parent != WarsUIManager.instance.PlayerCardsRack.transform || Mathf.Abs(touchDeltaPosition.x) / 2 < Mathf.Abs(touchDeltaPosition.y))
        {
            WarsUIManager.instance.isDragDelegated = true;
        }
        else
        {
            WarsUIManager.instance.isDragDelegated = false;
            PointerEventData _eventData = data as PointerEventData;
            //调用Scroll的OnBeginDrag方法，有了区分，就不会被item的拖拽事件屏蔽
            ScrollRect.OnBeginDrag(_eventData);
        }

        if (!WarsUIManager.instance.isDragDelegated)
        {
            return;
        }
        transform.SetParent(CardListTransform);
        transform.SetAsLastSibling();   //设置为同父物体的最从底层，也就是不会被其同级遮挡。
        if (transform.GetComponent<Image>().raycastTarget)
            transform.GetComponent<Image>().raycastTarget = false;
    }

    //拖动中
    public override void OnDrag(BaseEventData data)
    {
        if (IsLocked)
        {
            return;
        }

        if (!WarsUIManager.instance.isDragDelegated)
        {
            PointerEventData _eventData = data as PointerEventData;
            ScrollRect.OnDrag(_eventData);
            return;
        }
        if (FightController.instance.recordWinner != 0)
        {
            EndDrag(data);
            return;
        }
        transform.position = Input.mousePosition;
    }

    //结束时
    public override void EndDrag(BaseEventData data)
    {
        if (IsLocked)
        {
            return;
        }

        if (!WarsUIManager.instance.isDragDelegated)    //判断是否拖动的是滑动列表
        {
            PointerEventData eventData = data as PointerEventData;
            ScrollRect.OnEndDrag(eventData);
            return;
        }

        if (FightController.instance.isRoundBegin)  //战斗回合突然开始
        {
            transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
            transform.GetChild(8).gameObject.SetActive(false);
            transform.GetComponent<Image>().raycastTarget = true;
            FightCard.posIndex = -1;
            return;
        }

        PointerEventData _eventData = data as PointerEventData; //获取拖拽释放事件
        if (_eventData == null)
            return;
        var mgr = FightForManager.instance;
        GameObject go = _eventData.pointerCurrentRaycast.gameObject;    //释放时鼠标透过拖动的Image后的物体
        //是否拖放在 战斗格子 或 卡牌上
        if (go != null && (go.CompareTag("CardPos") || go.CompareTag("PyCard")))
        {
            //放在 战斗格子上
            if (go.CompareTag("CardPos"))
            {
                //拖动牌原位置 在上阵位
                if (FightCard.posIndex != -1)
                {
                    int targetPos = int.Parse(go.GetComponentInChildren<Text>().text);    //目标位置编号
                    if (FightCard.posIndex != targetPos)    //上阵位置改变
                    {
                        var playerCards = FightForManager.instance.GetCardList(true);
                        var card = playerCards[FightCard.posIndex];
                        FightForManager.instance.PlaceCardOnBoard(playerCards[FightCard.posIndex], FightCard.posIndex,
                            false);
                        FightCard.posIndex = targetPos;

                        FightForManager.instance.PlaceCardOnBoard(card, FightCard.posIndex, true);
                    }
                    transform.position = go.transform.position;
                    FightController.instance.PlayAudioForSecondClip(85, 0);

                    EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                    //GameObject eftObj = EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                    //eftObj.transform.SetParent(FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                }
                //拖动牌原位置 在备战位
                else
                {
                    int num = int.Parse(go.GetComponentInChildren<Text>().text);
                    //是否可以上阵
                    if (FightForManager.instance.IsPlayerAvailableToPlace(num))
                    {
                        int index = WarsUIManager.instance.FindDataFromCardsDatas(gameObject);
                        if (index != -1)
                        {
                            mgr.GetCardList(true)[num].posIndex = num;
                            //Debug.Log(">>>>>>>" + num);
                            FightForManager.instance.OnBoardReactSet(mgr.GetCardList(true)[num], num, true, true);
                        }
                        transform.position = go.transform.position;
                        transform.GetChild(8).gameObject.SetActive(true);
                        FightCard.posIndex = num;
                        //isFightCard = true;
                        FightController.instance.PlayAudioForSecondClip(85, 0);

                        EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                        //GameObject eftObj = EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                        //eftObj.transform.SetParent(FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                    }
                    else
                    {
                        //Debug.Log("上阵位已满");
                        transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
                        transform.GetChild(8).gameObject.SetActive(false);
                        FightCard.posIndex = -1;
                    }
                }
            }
            //放在 卡牌上
            if (go.CompareTag("PyCard"))
            {
                int goIndexPos = go.GetComponent<CardForDrag>().FightCard.posIndex;
                //目的地 卡牌在上阵位 并且 不是上锁卡牌
                if (goIndexPos != -1 && !go.GetComponent<CardForDrag>().IsLocked)
                {
                    //拖动牌原位置 在上阵位
                    if (FightCard.posIndex != -1)
                    {
                        FightForManager.instance.OnBoardReactSet(mgr.GetCardList(true)[FightCard.posIndex], FightCard.posIndex, true, false);
                        FightForManager.instance.OnBoardReactSet(mgr.GetCardList(true)[goIndexPos], goIndexPos, true, false);

                        transform.position = go.transform.position;
                        go.transform.position = FightForManager.instance.playerCardsPos[FightCard.posIndex].transform.position;
                        //WarsUIManager.instance.CardMoveToPos(go, FightForManager.instance.playerCardsPos[FightCard.posIndex].transform.position);
                        FightCardData dataTemp = mgr.GetCardList(true)[goIndexPos];
                        mgr.GetCardList(true)[goIndexPos].posIndex = goIndexPos;
                        mgr.GetCardList(true)[FightCard.posIndex].posIndex = FightCard.posIndex;
                        go.GetComponent<CardForDrag>().FightCard.posIndex = FightCard.posIndex;

                        FightForManager.instance.PlaceCardOnBoard(mgr.GetCardList(true)[FightCard.posIndex], FightCard.posIndex, true);
                        FightForManager.instance.PlaceCardOnBoard(mgr.GetCardList(true)[goIndexPos], goIndexPos, true);

                        EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                        //GameObject eftObj = EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                        EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[goIndexPos].transform);
                        //GameObject eftObj1 = EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[goIndexPos].transform);
                        //eftObj.transform.SetParent(FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                        //eftObj1.transform.SetParent(FightForManager.instance.playerCardsPos[goIndexPos].transform);

                        FightCard.posIndex = goIndexPos;
                        FightController.instance.PlayAudioForSecondClip(85, 0);
                    }
                    //拖动牌原位置 在备战位
                    else
                    {
                        if (FightForManager.instance.IsPlayerAvailableToPlace(goIndexPos))
                        {
                            transform.position = go.transform.position;
                            transform.GetChild(8).gameObject.SetActive(true);

                            FightForManager.instance.OnBoardReactSet(
                                FightForManager.instance.GetCardList(true)[goIndexPos], goIndexPos, true, true);

                            go.transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);

                            int index = WarsUIManager.instance.FindDataFromCardsDatas(gameObject);
                            if (index != -1)
                            {
                                mgr.GetCardList(true)[go.GetComponent<CardForDrag>().FightCard.posIndex].posIndex = go.GetComponent<CardForDrag>().FightCard.posIndex;
                            }
                            go.GetComponent<CardForDrag>().FightCard.posIndex = -1;
                            go.transform.GetChild(8).gameObject.SetActive(false);
                            FightCard.posIndex = goIndexPos;

                            FightForManager.instance.PlaceCardOnBoard(mgr.GetCardList(true)[FightCard.posIndex],
                                FightCard.posIndex, true);
                            FightController.instance.PlayAudioForSecondClip(85, 0);

                            EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                            //GameObject eftObj =  EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                            //eftObj.transform.SetParent(FightForManager.instance.playerCardsPos[FightCard.posIndex].transform);
                        } 
                        else
                        {
                            //Debug.Log("上阵位已满");
                            transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
                            transform.GetChild(8).gameObject.SetActive(false);
                            FightCard.posIndex = -1;
                        }
                    }
                }
                //目的地 卡牌在备战位
                else
                {
                    if (FightCard.posIndex != -1)
                    {
                        FightForManager.instance.PlaceCardOnBoard(mgr.GetCardList(true)[FightCard.posIndex],
                            FightCard.posIndex, false);
                    }
                    transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
                    transform.GetChild(8).gameObject.SetActive(false);
                    FightCard.posIndex = -1;
                }
            }
        }
        else //目的地为其他
        {
            if (FightCard.posIndex != -1) //原位置在上阵位
            {
                FightForManager.instance.RemoveCardFromBoard(mgr.GetCardList(true)[FightCard.posIndex], true);
            }
            transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
            transform.GetChild(8).gameObject.SetActive(false);
            FightCard.posIndex = -1;
        }
        transform.GetComponent<Image>().raycastTarget = true;
    }

    public override void ResetPos(int pos)
    {
        throw new System.NotImplementedException();
    }
    public override void Disable()
    {
        throw new System.NotImplementedException();
    }

}