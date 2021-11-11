using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerCardRack : DragInputControlController<FightCardData>
{
    public ScrollRect ScrollRect;
    [SerializeField] private Chessboard Chessboard;
    public int LastPos { get; private set; }
    private bool IsDragDelegated { get; set; }

    public override void PointerDown(BaseEventData data, FightCardData card) => WarsUIManager.instance.DisplayCardInfo(card, true);

    public override void PointerUp(BaseEventData data, FightCardData card) =>
        WarsUIManager.instance.DisplayCardInfo(card, true);

    public override void StartDrag(BaseEventData data, FightCardData card)
    {
        Vector2 deltaPosition = Vector2.zero;
#if UNITY_EDITOR
        float delta_x = Input.GetAxis(Mouse_X);
        float delta_y = Input.GetAxis(Mouse_Y);
        deltaPosition = new Vector2(delta_x, delta_y);

#elif UNITY_ANDROID || UNITY_IPHONE
        deltaPosition = Input.GetTouch(0).deltaPosition;
#endif
        var pointer = data as PointerEventData;
        var ui = card.cardObj;
        if (ui.transform.parent == ScrollRect.content 
            && !(Mathf.Abs(deltaPosition.x) / 2 < Mathf.Abs(deltaPosition.y)))
        {
            IsDragDelegated = true;
            //调用Scroll的OnBeginDrag方法，有了区分，就不会被item的拖拽事件屏蔽
            ScrollRect.OnBeginDrag(pointer);
            return;
        }
        if (WarsUIManager.instance.isDragDisable) return;

        IsDragDelegated = false;

        ui.transform.SetParent(transform);
        ui.transform.SetAsLastSibling(); //设置为同父物体的最从底层，也就是不会被其同级遮挡。
        OnStartDrag(pointer);
    }

    public override void OnDrag(BaseEventData data, FightCardData card)
    {
        WarsUIManager.instance.DisplayCardInfo(card, false);
        if (IsDragDelegated)
        {
            var eventData = data as PointerEventData;
            ScrollRect.OnDrag(eventData);
            return;
        }
        if (WarsUIManager.instance.isDragDisable) return;

        card.cardObj.transform.position = Input.mousePosition;
    }

    public override void EndDrag(BaseEventData eventData, FightCardData card)
    {
        if (!(eventData is PointerEventData pointer))
        {
            ResetPos(card);
            return;
        }
        if (IsDragDelegated) //判断是否拖动的是滑动列表
        {
            ScrollRect.OnEndDrag(pointer);
            return;
        }

        if (WarsUIManager.instance.isDragDisable)
        {
            ResetPos(card);
            return;
        }
        if (!WarsUIManager.instance.isDragDisable)
        {
            var pos = GetRayCastResults(pointer).FirstOrDefault(r => r.gameObject.CompareTag(GameSystem.CardPos))
                .gameObject?.GetComponent<ChessPos>();
            if (pos != null)
            {
                PlaceOnInvocation(pos, card);
                return;
            }
        }

        ResetPos(card);
    }

    private void OnStartDrag(PointerEventData pointer)
    {
        if (GetRayCastResults(pointer).Any(r => r.gameObject == ScrollRect.gameObject))
        {
            LastPos = -1;
            return;
        }

        ChessPos chessPos = null;
        var obj = GetRayCastResults(pointer).Select(r => r.gameObject)
            .FirstOrDefault(o => o.CompareTag(GameSystem.CardPos));
        if (obj != null)
            chessPos = obj.GetComponent<ChessPos>();
        if (chessPos == null)
            LastPos = -1;
        else LastPos = chessPos.Pos;
    }

    private void ResetPos(FightCardData card)
    {
        var ui = card.cardObj.transform;
        if (LastPos == -1)
        {
            ui.SetParent(ScrollRect.content);
            return;
        }
        var chessPos = Chessboard.GetChessPos(LastPos, true);
        ui.SetParent(chessPos.transform);
        ui.localPosition = Vector3.zero;
    }

    private void PlaceOnInvocation(ChessPos pos, FightCardData card)
    {
        var playerScope = WarsUIManager.instance.PlayerScope;
        var isExchange = playerScope.Any(c => c.Pos == pos.Pos);
        if (!WarsUIManager.instance.IsPlayerAvailableToPlace(pos.Pos, !isExchange))
        {
            ResetPos(card);
            return;
        }

        var replaceCard = playerScope.FirstOrDefault(c => c != card && c.Pos == pos.Pos);
        if (replaceCard != null)
        {
            replaceCard.SetPos(-1);
            WarsUIManager.instance.PlaceCard(replaceCard);
        }
        card.SetPos(pos.Pos);
        WarsUIManager.instance.PlaceCard(card);
    }
}