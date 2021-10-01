using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface ICardDrag 
{
    int PosIndex { get; }
    void UpdatePos(int pos);
}
public class PlayerCardDrag : DragController
{
    [SerializeField] private Image CardBody;
    [SerializeField] private WarGameCardUi Ui;

    public override void BeginDrag(BaseEventData data)
    {
        if (IsLocked) return;
        if (WarsUIManager.instance.isDragDisable) return;

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
        if (IsLocked) return;
        if (WarsUIManager.instance.isDragDisable) return;

        if (!WarsUIManager.instance.isDragDelegated)
        {
            var eventData = data as PointerEventData;
            Rack.OnDrag(eventData);
            return;
        }
        transform.position = Input.mousePosition;
    }

    public override void EndDrag(BaseEventData data)
    {
        if (IsLocked) return;
        if (WarsUIManager.instance.isDragDisable)
        {
            ResetPos();
            return;
        }

        if (!(data is PointerEventData eventData)) return;

        if (!WarsUIManager.instance.isDragDelegated) //判断是否拖动的是滑动列表
        {
            Rack.OnEndDrag(eventData);
            return;
        }

        if (eventData.pointerCurrentRaycast.gameObject == null || 
            WarsUIManager.instance.chessboardManager.IsBusy) //战斗回合突然开始
        {
            ResetPos();
            return;
        }

        var posObj = eventData.pointerCurrentRaycast.gameObject; //释放时鼠标透过拖动的Image后的物体
        if (posObj == null)
        {
            ResetPos();
            return;
        }

        var targetPosIndex = -1;
        if (posObj.CompareTag(GameSystem.CardPos)) targetPosIndex = posObj.GetComponent<ChessPos>().Pos;
        else
        {
            var war = posObj.GetComponent<GameCardWarUiOperation>();
            if(war && war.baseUi.CompareTag(GameSystem.PyCard)) targetPosIndex = war.DragController.PosIndex;
        }


        //是否拖放在 战斗格子 或 卡牌上
        if (targetPosIndex == -1)
        {
            if (PosIndex != targetPosIndex)//从在棋盘上摘下来
            {
                WarsUIManager.instance.RemoveCardFromBoard(FightCard);
                return;
            }

            ResetPos();
            return;
        }

        //放在 战斗格子上
        //拖动牌原位置 在上阵位
        if (PosIndex == -1)
        {
            //是否可以上阵
            if (!WarsUIManager.instance.IsPlayerAvailableToPlace(targetPosIndex))
            {
                //Debug.Log("上阵位已满");
                ResetPos();
                return;
            }

            PlaceCard(targetPosIndex);
            return;
        }

        //目标位置编号
        if (PosIndex == targetPosIndex) //上阵位置没改变
        {
            ResetPos();
            return;
        }

        PlaceCard(targetPosIndex);
    }

    private void PlaceCard(int targetPos)
    {
        FightCard.posIndex = targetPos;
        WarsUIManager.instance.PlaceCardOnTemp(FightCard);
        Ui.SetHighLight(true);

        WarsUIManager.instance.PlayAudioForSecondClip(85, 0);

        EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f,
            WarsUIManager.instance.Chessboard.GetChessPos(targetPos, true).transform);
        CardBody.raycastTarget = !IsLocked;
    }

    public override void ResetPos()
    {
        if (WarsUIManager.instance != null && transform != null)
        {
            if (PosIndex == -1) transform.SetParent(WarsUIManager.instance.PlayerCardsRack.transform);
            Ui.SetSelected(false);
            CardBody.raycastTarget = !IsLocked;
        }
    }
}