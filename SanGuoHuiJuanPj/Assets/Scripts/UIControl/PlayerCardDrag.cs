//using System.Linq;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public interface ICardDrag 
//{
//    int PosIndex { get; }
//    void UpdatePos(int pos);
//}
//public class PlayerCardDrag : MonoBehaviour
//{
//    [SerializeField] private Image CardBody;
//    [SerializeField] private WarGameCardUi Ui;
//    protected const string Mouse_X = "Mouse X";
//    protected const string Mouse_Y = "Mouse Y";

//    private ChessPos LastPos { get; set; }
//    public int PosIndex => FightCard.posIndex;  //上场位置记录
//    public bool IsLocked { get; set; } = false; //记录是否是上阵卡牌

//    protected Transform CardListTransform { get; private set; }
//    /// <summary>
//    /// Scroll View上的Scroll Rect组件
//    /// </summary>
//    protected ScrollRect ScrollRect { get; private set; }

//    protected FightCardData FightCard { get; private set; }

//    public virtual void Init(FightCardData fightCard, Transform cardListTrans, ScrollRect scrollRect)
//    {
//        CardListTransform = cardListTrans;
//        ScrollRect = scrollRect;
//        FightCard = fightCard;
//    }
//    public void BeginDrag(BaseEventData data)
//    {
//        if (IsLocked) return;
//        if (WarsUIManager.instance.isDragDisable) return;

//        Vector2 deltaPosition = Vector2.zero;
//#if UNITY_EDITOR
//        float delta_x = Input.GetAxis(Mouse_X);
//        float delta_y = Input.GetAxis(Mouse_Y);
//        deltaPosition = new Vector2(delta_x, delta_y);

//#elif UNITY_ANDROID || UNITY_IPHONE
//        deltaPosition = Input.GetTouch(0).deltaPosition;
//#endif
//        if (transform.parent == WarsUIManager.instance.PlayerCardsRack.transform &&
//            !(Mathf.Abs(deltaPosition.x) / 2 < Mathf.Abs(deltaPosition.y)))
//        {
//            WarsUIManager.instance.isDragDelegated = false;
//            var eventData = data as PointerEventData;
//            //调用Scroll的OnBeginDrag方法，有了区分，就不会被item的拖拽事件屏蔽
//            ScrollRect.OnBeginDrag(eventData);
//            return;
//        }

//        WarsUIManager.instance.isDragDelegated = true;

//        transform.SetParent(CardListTransform);
//        transform.SetAsLastSibling(); //设置为同父物体的最从底层，也就是不会被其同级遮挡。
//        if (CardBody.raycastTarget)
//            CardBody.raycastTarget = false;
//    }

//    public void OnDrag(BaseEventData data)
//    {
//        if (IsLocked) return;
//        if (WarsUIManager.instance.isDragDisable) return;

//        if (!WarsUIManager.instance.isDragDelegated)
//        {
//            var eventData = data as PointerEventData;
//            ScrollRect.OnDrag(eventData);
//            return;
//        }
//        transform.position = Input.mousePosition;
//    }

//    public void EndDrag(BaseEventData data)
//    {
//        if (IsLocked) return;
//        if (WarsUIManager.instance.isDragDisable)
//        {
//            ResetPos();
//            return;
//        }

//        if (!(data is PointerEventData eventData)) return;

//        if (!WarsUIManager.instance.isDragDelegated) //判断是否拖动的是滑动列表
//        {
//            ScrollRect.OnEndDrag(eventData);
//            return;
//        }

//        if (eventData.pointerCurrentRaycast.gameObject == null || 
//            WarsUIManager.instance.chessboardManager.IsBusy) //战斗回合突然开始
//        {
//            ResetPos();
//            return;
//        }

//        var posObj = eventData.pointerCurrentRaycast.gameObject; //释放时鼠标透过拖动的Image后的物体
//        if (posObj == null)
//        {
//            ResetPos();
//            return;
//        }

//        ChessPos targetPos = null;
//        var targetPosIndex = -1;
//        if (posObj.CompareTag(GameSystem.CardPos))
//        {
//            targetPos = posObj.GetComponent<ChessPos>();
//            targetPosIndex = targetPos.Pos;

//            if (targetPosIndex == PosIndex ||
//                !WarsUIManager.instance.IsPlayerAvailableToPlace(targetPosIndex, PosIndex < 0))
//            {
//                ResetPos();
//                return;
//            }
//        }
//        else
//        {
//            var war = posObj.GetComponent<GameCardWarUiOperation>();
//            if (!war)
//            {
//                ResetPos(-1);
//                return;
//            }
//            if(war && war.baseUi.CompareTag(GameSystem.PyCard)) targetPosIndex = war.DragController.PosIndex;
//            if (targetPosIndex == -1)
//            {
//                ResetPos(targetPosIndex);
//                return;
//            }
//            if (this != war.DragController)//当有别的棋子存在
//            {
//                if (!war.DragController.IsLocked)//棋子是可移动的
//                {
//                    war.DragController.ResetPos(PosIndex);
//                    PlaceCard(WarsUIManager.instance.Chessboard.GetChessPos(targetPosIndex, true));
//                    return;
//                }

//                ResetPos();
//                return;
//            }
//        }


//        //目标位置不是棋格
//        if (targetPosIndex == -1)
//        {
//            if (PosIndex != targetPosIndex)//从在棋盘上摘下来
//            {
//                LastPos = null;
//                WarsUIManager.instance.RemoveCardFromBoard(FightCard);
//            }

//            ResetPos();
//            return;
//        }

//        //原位置在架子上，目标是棋格
//        if (PosIndex == -1 && targetPos != null)
//        {
//            //是否可以上阵
//            if (!WarsUIManager.instance.IsPlayerAvailableToPlace(targetPosIndex, PosIndex < 0))
//            {
//                //Debug.Log("上阵位已满");
//                ResetPos();
//                return;
//            }

//            PlaceCard(targetPos);
//            return;
//        }

//        //目标位置编号
//        if (PosIndex == targetPos?.Pos) //上阵位置没改变
//        {
//            ResetPos();
//            return;
//        }

//        //到这里是原位是棋格上，但是位置变化了
//        PlaceCard(targetPos);
//    }

//    private void PlaceCard(ChessPos pos)
//    {
//        FightCard.posIndex = pos.Pos;
//        LastPos = pos;
//        WarsUIManager.instance.UpdateCardTempScope(FightCard);
//        //Ui.SetHighLight(true);
//        UpdateSelectionUi();
//        WarsUIManager.instance.PlayAudioForSecondClip(85, 0);

//        EffectsPoolingControl.instance.GetEffectToFight1("toBattle", 0.7f,
//            WarsUIManager.instance.Chessboard.GetChessPos(pos.Pos, true).transform);
//        CardBody.raycastTarget = true;
//    }

//    /***
//     * 重置位置
//     * 1.知道上一个位置，恢复到那个位置的状态：
//     * -a.回到架子上(pos = -1)
//     * -b.回到地块上(pos >= 0)
//     * -2 = 取消位置
//     */
//    public void ResetPos(int pos = -2)
//    {
//        if (pos == -2)
//            pos = LastPos != null ? LastPos.Pos : -1;
//        else FightCard.UpdatePos(pos);

//        if (WarsUIManager.instance != null && transform != null)
//        {
//            WarsUIManager.instance.UpdateCardTempScope(FightCard);
//            transform.SetParent(PosIndex == -1
//                ? ScrollRect.content.transform
//                : WarsUIManager.instance.Chessboard.GetChessPos(pos, true).transform);
//            UpdateSelectionUi();
//            CardBody.raycastTarget = true;
//            transform.localPosition = Vector3.zero;
//        }
//    }

//    public void Disable()
//    {
//        CardBody.raycastTarget = false;
//        IsLocked = true;
//    }

//    public void UpdateSelectionUi() => Ui.SetSelected(!IsLocked && PosIndex > -1);
//}