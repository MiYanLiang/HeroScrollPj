using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChessboardInputController : DragInputControlController<FightCardData>
{
    private bool IsDragAble(FightCardData card) => !WarsUIManager.instance.isDragDisable && !card.IsLock;
    private ChessPos lastPos;
    public override void StartDrag(BaseEventData eventData, FightCardData card)
    {
        if(!IsDragAble(card))return;
        lastPos = GetChessPos(eventData as PointerEventData);
    }

    public override void OnDrag(BaseEventData eventData, FightCardData card)
    {
        if(!IsDragAble(card))return;
        if (!(eventData is PointerEventData pointer)) return;
        card.cardObj.transform.position = pointer.position;
    }

    public override void EndDrag(BaseEventData eventData, FightCardData card)
    {
        if(!IsDragAble(card))return;
        if (!(eventData is PointerEventData pointer)) return;
        var pos = GetChessPos(pointer);
        if (pos == null)
        {
            card.posIndex = -1;
            WarsUIManager.instance.PlaceCard(card);
            return;
        }
        if (pos.Card == null || pos.Card.Status.IsDeath)
        {
            if (pos.Card != null && !pos.Card.IsLock)
            {
                pos.Card.posIndex = lastPos.Pos;
                WarsUIManager.instance.PlaceCard(pos.Card);
                WarsUIManager.instance.PlaceCard(card);
                return;
            }

            card.posIndex = pos.Pos;
            WarsUIManager.instance.PlaceCard(card);
            return;
        }
        ResetPos(card);
    }

    private ChessPos GetChessPos(PointerEventData pointer)
    {
        var obj = GetRayCastResults(pointer)
            .FirstOrDefault(r => r.gameObject.CompareTag(GameSystem.CardPos)).gameObject;
        return obj == null ? null : obj.GetComponent<ChessPos>();
    }

    private void ResetPos(FightCardData card)
    {
        card.cardObj.transform.localPosition = Vector3.zero;
    }
}