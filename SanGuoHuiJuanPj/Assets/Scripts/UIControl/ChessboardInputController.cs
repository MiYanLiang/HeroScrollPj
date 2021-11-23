using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ChessboardInputController : DragInputControlController<FightCardData>
{
    private WarBoardUi WarBoardUi { get; set; }
    public void Init(WarBoardUi warBoard) => WarBoardUi = warBoard;
    private bool IsDragAble(FightCardData card) => !WarBoardUi.IsDragDisable && !card.IsLock;
    private ChessPos lastPos;

    public override void PointerDown(BaseEventData data, FightCardData card) => WarBoardUi.DisplayCardInfo(card, true);

    public override void PointerUp(BaseEventData data, FightCardData card) => WarBoardUi.DisplayCardInfo(card, true);

    public override void StartDrag(BaseEventData eventData, FightCardData card)
    {
        if(!IsDragAble(card))return;
        lastPos = GetChessPos(eventData as PointerEventData);
        card.cardObj.transform.SetParent(WarBoardUi.Chessboard.transform);
    }

    public override void OnDrag(BaseEventData eventData, FightCardData card)
    {
        if(!IsDragAble(card))//强制归位
        {
            ResetPos(card);
            return;
        }
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
            SetPos(null, card);
            return;
        }

        if (pos == lastPos || pos.Pos == 17)
        {
            ResetPos(card);
            return;
        }

        var cards = WarBoardUi.PlayerScope.Where(c => c.Pos == pos.Pos).ToArray();
        if (cards.Any(c => !c.Status.IsDeath && c.IsLock))
        {
            ResetPos(card);
            return;
        }

        var replaceCard = cards.FirstOrDefault(c => !c.Status.IsDeath);
        if (replaceCard == null)
        {
            if (WarBoardUi.IsPlayerAvailableToPlace(pos.Pos, lastPos == null))
            {
                SetPos(pos, card);
                return;
            }

            ResetPos(card);
            return;
        }

        if (!replaceCard.IsLock)
        {
            SetPos(lastPos, replaceCard);
            SetPos(pos, card);
            return;
        }

        ResetPos(card);
    }

    private void SetPos(ChessPos pos,FightCardData card)
    {
        card.SetPos(pos == null ? -1 : pos.Pos);
        WarBoardUi.PlaceCard(card);
    }

    private ChessPos GetChessPos(PointerEventData pointer)
    {
        var obj = GetRayCastResults(pointer)
            .FirstOrDefault(r => r.gameObject.CompareTag(GameSystem.CardPos)).gameObject;
        return obj == null ? null : obj.GetComponent<ChessPos>();
    }

    private void ResetPos(FightCardData card)
    {
        if (card == null) return;
        if (card.Pos >= 0) card.cardObj.transform.SetParent(lastPos.transform);

        card.cardObj.transform.localPosition = Vector3.zero;
    }
}