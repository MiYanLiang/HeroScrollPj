using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerCardDrag : DragController
{
    private const string PyCardTag = "PyCard";
    private const string CardPosTag = "CardPos";
    [SerializeField] private Image CardBody;
    [SerializeField] private WarGameCardUi Ui;

    public override void BeginDrag(BaseEventData data)
    {
        if (IsMoveable) return;

        Vector2 deltaPosition = Vector2.zero;
#if UNITY_EDITOR
        float delta_x = Input.GetAxis(Mouse_X);
        float delta_y = Input.GetAxis(Mouse_Y);
        deltaPosition = new Vector2(delta_x, delta_y);

#elif UNITY_ANDROID || UNITY_IPHONE
        deltaPosition = Input.GetTouch(0).deltaPosition;
#endif
        if (transform.parent == WarsUIManager.instance.PlayerCardsRack.transform &&
            !(Mathf.Abs(deltaPosition.x) / 2 < Mathf.Abs(deltaPosition.y)))
        {
            WarsUIManager.instance.isDragDelegated = false;
            var eventData = data as PointerEventData;
            //调用Scroll的OnBeginDrag方法，有了区分，就不会被item的拖拽事件屏蔽
            Rack.OnBeginDrag(eventData);
            return;
        }

        WarsUIManager.instance.isDragDelegated = true;

        transform.SetParent(Parent);
        transform.SetAsLastSibling(); //设置为同父物体的最从底层，也就是不会被其同级遮挡。
        if (CardBody.raycastTarget)
            CardBody.raycastTarget = false;
    }

    public override void OnDrag(BaseEventData data)
    {
        if (IsMoveable) return;

        if (!WarsUIManager.instance.isDragDelegated)
        {
            var eventData = data as PointerEventData;
            Rack.OnDrag(eventData);
            return;
        }
        if (FightController.instance.recordWinner != 0)
        {
            EndDrag(data);
            return;
        }
        transform.position = Input.mousePosition;
    }

    public override void EndDrag(BaseEventData data)
    {
        if (IsMoveable) return;

        if (!(data is PointerEventData eventData)) return;

        if (!WarsUIManager.instance.isDragDelegated)    //判断是否拖动的是滑动列表
        {
            Rack.OnEndDrag(eventData);
            return;
        }

        if (FightController.instance.isRoundBegin) //战斗回合突然开始
        {
            ResetCardToRack();
            return;
        }


        var go = eventData.pointerCurrentRaycast.gameObject;    //释放时鼠标透过拖动的Image后的物体
        var fMgr = FightForManager.instance;
        //是否拖放在 战斗格子 或 卡牌上

        if (go != null && (go.CompareTag(CardPosTag) || go.CompareTag(PyCardTag)))
        {
            //放在 战斗格子上
            if (go.CompareTag(CardPosTag))
            {
                //拖动牌原位置 在上阵位
                if (PosIndex != -1)
                {
                    var num = int.Parse(go.GetComponentInChildren<Text>().text);    //目标位置编号
                    if (PosIndex != num)    //上阵位置改变
                    {
                        fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[PosIndex], PosIndex, fMgr.playerFightCardsDatas, false);

                        fMgr.playerFightCardsDatas[num] = fMgr.playerFightCardsDatas[PosIndex];
                        fMgr.playerFightCardsDatas[num].posIndex = num;
                        fMgr.playerFightCardsDatas[PosIndex] = null;
                        PosIndex = num;

                        fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[PosIndex], PosIndex, fMgr.playerFightCardsDatas, true);
                    }
                    transform.SetParent(fMgr.playerCardsBox);
                    transform.position = go.transform.position;
                    FightController.instance.PlayAudioForSecondClip(85, 0);

                    EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[PosIndex].transform);
                    //GameObject eftObj = EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[posIndex].transform);
                    //eftObj.transform.SetParent(fMgr.playerCardsPos[posIndex].transform);
                }
                //拖动牌原位置 在备战位
                else
                {
                    //是否可以上阵
                    if (fMgr.PlaceOrRemoveCard(true))
                    {
                        int num = int.Parse(go.GetComponentInChildren<Text>().text);
                        int index = WarsUIManager.instance.FindDataFromCardsDatas(gameObject);
                        if (index != -1)
                        {
                            fMgr.playerFightCardsDatas[num] = WarsUIManager.instance.playerCardsDatas[index];
                            fMgr.playerFightCardsDatas[num].posIndex = num;
                            //Debug.Log(">>>>>>>" + num);
                            fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[num], num, fMgr.playerFightCardsDatas, true);
                        }
                        transform.SetParent(fMgr.playerCardsBox);
                        transform.position = go.transform.position;
                        transform.GetChild(8).gameObject.SetActive(true);
                        PosIndex = num;
                        //isFightCard = true;
                        FightController.instance.PlayAudioForSecondClip(85, 0);

                        EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[PosIndex].transform);
                        //GameObject eftObj = EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[posIndex].transform);
                        //eftObj.transform.SetParent(fMgr.playerCardsPos[posIndex].transform);
                    }
                    else
                    {
                        //Debug.Log("上阵位已满");
                        transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
                        transform.GetChild(8).gameObject.SetActive(false);
                        PosIndex = -1;
                    }
                }
            }
            //放在 卡牌上
            if (go.CompareTag(PyCardTag))
            {
                int goIndexPos = go.GetComponent<PlayerCardDrag>().PosIndex;
                //目的地 卡牌在上阵位 并且 不是上锁卡牌
                if (goIndexPos != -1 && !go.GetComponent<PlayerCardDrag>().IsMoveable)
                {
                    //拖动牌原位置 在上阵位
                    if (PosIndex != -1)
                    {
                        fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[PosIndex], PosIndex, fMgr.playerFightCardsDatas, false);
                        fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[goIndexPos], goIndexPos, fMgr.playerFightCardsDatas, false);

                        transform.SetParent(fMgr.playerCardsBox);
                        transform.position = go.transform.position;
                        go.transform.position = fMgr.playerCardsPos[PosIndex].transform.position;
                        //WarsUIManager.instance.CardMoveToPos(go, fMgr.playerCardsPos[posIndex].transform.position);
                        FightCardData dataTemp = fMgr.playerFightCardsDatas[goIndexPos];
                        fMgr.playerFightCardsDatas[goIndexPos] = fMgr.playerFightCardsDatas[PosIndex];
                        fMgr.playerFightCardsDatas[goIndexPos].posIndex = goIndexPos;
                        fMgr.playerFightCardsDatas[PosIndex] = dataTemp;
                        fMgr.playerFightCardsDatas[PosIndex].posIndex = PosIndex;
                        go.GetComponent<PlayerCardDrag>().PosIndex = PosIndex;

                        fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[PosIndex], PosIndex, fMgr.playerFightCardsDatas, true);
                        fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[goIndexPos], goIndexPos, fMgr.playerFightCardsDatas, true);

                        EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[PosIndex].transform);
                        //GameObject eftObj = EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[posIndex].transform);
                        EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[goIndexPos].transform);
                        //GameObject eftObj1 = EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[goIndexPos].transform);
                        //eftObj.transform.SetParent(fMgr.playerCardsPos[posIndex].transform);
                        //eftObj1.transform.SetParent(fMgr.playerCardsPos[goIndexPos].transform);

                        PosIndex = goIndexPos;
                        FightController.instance.PlayAudioForSecondClip(85, 0);
                    }
                    //拖动牌原位置 在备战位
                    else
                    {
                        fMgr.PlaceOrRemoveCard(false);
                        if (fMgr.PlaceOrRemoveCard(true))
                        {
                            transform.SetParent(fMgr.playerCardsBox);
                            transform.position = go.transform.position;
                            transform.GetChild(8).gameObject.SetActive(true);

                            fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[goIndexPos], goIndexPos, fMgr.playerFightCardsDatas, false);

                            go.transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);

                            int index = WarsUIManager.instance.FindDataFromCardsDatas(gameObject);
                            if (index != -1)
                            {
                                fMgr.playerFightCardsDatas[go.GetComponent<PlayerCardDrag>().PosIndex] = WarsUIManager.instance.playerCardsDatas[index];
                                fMgr.playerFightCardsDatas[go.GetComponent<PlayerCardDrag>().PosIndex].posIndex = go.GetComponent<PlayerCardDrag>().PosIndex;
                            }
                            go.GetComponent<PlayerCardDrag>().PosIndex = -1;
                            go.transform.GetChild(8).gameObject.SetActive(false);
                            PosIndex = goIndexPos;

                            fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[PosIndex], PosIndex, fMgr.playerFightCardsDatas, true);
                            FightController.instance.PlayAudioForSecondClip(85, 0);

                            EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[PosIndex].transform);
                            //GameObject eftObj =  EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f, fMgr.playerCardsPos[posIndex].transform);
                            //eftObj.transform.SetParent(fMgr.playerCardsPos[posIndex].transform);
                        }
                        else
                        {
                            fMgr.PlaceOrRemoveCard(true);
                            //Debug.Log("上阵位已满");
                            transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
                            transform.GetChild(8).gameObject.SetActive(false);
                            PosIndex = -1;
                        }
                    }
                }
                //目的地 卡牌在备战位
                else
                {
                    if (PosIndex != -1)
                    {
                        fMgr.PlaceOrRemoveCard(false);
                        fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[PosIndex], PosIndex, fMgr.playerFightCardsDatas, false);
                        fMgr.playerFightCardsDatas[PosIndex] = null;
                    }
                    transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
                    transform.GetChild(8).gameObject.SetActive(false);
                    PosIndex = -1;
                }
            }

            CardBody.raycastTarget = true;
            return;
        }

        //目的地为其他
        if (PosIndex != -1) //原位置在上阵位
        {
            fMgr.PlaceOrRemoveCard(false);
            fMgr.CardGoIntoBattleProcess(fMgr.playerFightCardsDatas[PosIndex], PosIndex, fMgr.playerFightCardsDatas, false);
            fMgr.playerFightCardsDatas[PosIndex] = null;
        }
        ResetCardToRack();
        CardBody.raycastTarget = true;
    }

    private void ResetCardToRack()
    {
        transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
        CardBody.raycastTarget = true;
        Ui.SetSelected(false);
        PosIndex = -1;
    }
}