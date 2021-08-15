using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class ChessmanOperator : MonoBehaviour
//{
//    public static ChessmanOperator Instance { get; private set; }
//    [SerializeField] private Chessboard Chessboard;
//    private Queue<IEnumerator> QueueOperation = new Queue<IEnumerator>();

//    public void Init()
//    {
//        Instance = this;
//    }

//    public IEnumerator MainOperation(ChessmanOperation operation)
//    {
//        yield return operation.PreAction();//Begin first operation
//        yield return operation.MainAction();//Invoke the main operation
//        while (QueueOperation.Count > 0)//Invoke interactive operations
//            yield return QueueOperation.Dequeue();
//        yield return operation.PostAction();
//    }

//    public void SubscribeOperation(IEnumerator operation) => QueueOperation.Enqueue(operation);
//}

//public abstract class ChessmanOperation
//{
//    public FightCardData Unit { get; private set; }
//    public GameObject Obj => Unit.cardObj.gameObject;
//    public RectTransform Rect { get; private set; }

//    public void SetUnit(FightCardData unit)
//    {
//        Unit = unit;
//        Rect = unit.cardObj.GetComponent<RectTransform>();
//    }
//    public abstract IEnumerator PreAction();
//    public abstract IEnumerator MainAction();
//    public abstract IEnumerator Respond(ChessmanOperation obj);
//    public abstract IEnumerator PostAction();
//}

//public abstract class GameCardOperation : ChessmanOperation
//{
//    public GameCardOperation Target { get; set; }
//    public IEnumerator MainOperation;

//    public IEnumerator RespondOperation;

//}

//public class HeroOperation : GameCardOperation
//{
//    [SerializeField]
//    float yuanChengShakeTimeToGo = 0.5f;
//    [SerializeField]
//    float yuanChengShakeTimeToBack = 0.5f;
//    [SerializeField]
//    float yuanChengShakeTime = 0.1f;
//    public float attackShakeTimeToGo = 0.534f;  //移动时间
//    public float attackShakeTimeToBack = 0.167f;  //移动时间
//    [SerializeField]
//    private float attackIntervalTime = 0.334f;   //间隔时间

//    [SerializeField] private float chessPosOffset = 0.5f;
//    public float forward = 0.2f;   //去
//    public float back = 0.3f;   //回
//    public float charge = 0.4f; //等
//    public float hit = 0.1f;  //去
//    public float posFloat;

//    //武将行动
//    public override IEnumerator MainAction()
//    {
//        yield return new WaitForSeconds(FightController.instance.attackShakeTimeToGo);
//        yield return HitTarget();
//        yield return Target.Respond(this);
//        yield return new WaitForSeconds(FightController.instance.attackShakeTimeToGo);
//    }

//    public override IEnumerator Respond(ChessmanOperation obj)
//    {
//        var ui = Unit.cardObj;
//        var align = Rect.sizeDelta.y * (Unit.isPlayerCard ? -1 : 1) * 0.1f;
//        var oriPos = ui.transform.position;
//        var stepBack = new Vector3(oriPos.x, oriPos.y + align, oriPos.z);
//        yield return DOTween.Sequence()
//            .Append(ui.transform.DOMove(stepBack, 0.3f))
//            .Append(ui.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10)))
//            .WaitForCompletion();
//        yield return ui.transform.DOMove(oriPos, FightController.instance.attackShakeTimeToGo * hit).SetEase(Ease.Unset)
//            .WaitForCompletion();
//    }

//    public override IEnumerator PreAction() => MoveToFrontOfCard();

//    public override IEnumerator PostAction() => MoveBack();

//    IEnumerator HitTarget()
//    {
//        var ui = Unit.cardObj;
//        var align = Rect.sizeDelta.y * (Unit.isPlayerCard ? -1 : 1);
//        var oriPos = ui.transform.position;
//        var stepBack = new Vector3(oriPos.x, oriPos.y + align, oriPos.z);
//        yield return ui.transform.DOMove(stepBack, FightController.instance.attackShakeTimeToGo * back).SetEase(Ease.Unset).WaitForCompletion();
//        yield return ui.transform.DOMove(ui.transform.position, FightController.instance.attackShakeTimeToGo * charge).SetEase(Ease.Unset).WaitForCompletion();
//        yield return ui.transform.DOMove(oriPos, FightController.instance.attackShakeTimeToGo * hit).SetEase(Ease.Unset)
//            .WaitForCompletion();
//    }

//    IEnumerator MoveToFrontOfCard()
//    {
//        var ui = Unit.cardObj;
//        var targetPos = Target.Obj.transform.position;
//        var targetSize = Rect.sizeDelta * Rect.lossyScale;
//        var yPos = targetPos.y + (Unit.isPlayerCard ? -1 : 1) * targetSize.y;
//        var targetFrontPos = new Vector3(targetPos.x, yPos, targetPos.z);
//        yield return ui.transform.DOMove(targetFrontPos, attackShakeTimeToGo * forward).SetEase(Ease.Unset).WaitForCompletion();
//    }

//    IEnumerator MoveBack()
//    {
//        yield return Unit.cardObj.transform.DOLocalMove(Vector3.zero, back).SetEase(Ease.Unset).WaitForCompletion();
//    }
//}

//public class MeleeOperation : GameCardOperation
//{
//    [SerializeField]
//    float yuanChengShakeTimeToGo = 0.5f;
//    [SerializeField]
//    float yuanChengShakeTimeToBack = 0.5f;
//    [SerializeField]
//    float yuanChengShakeTime = 0.1f;
//    public float attackShakeTimeToGo = 0.534f;  //移动时间
//    public float attackShakeTimeToBack = 0.167f;  //移动时间
//    [SerializeField]
//    private float attackIntervalTime = 0.334f;   //间隔时间

//    [SerializeField] private float chessPosOffset = 0.5f;
//    public float forward = 0.2f;   //去
//    public float back = 0.3f;   //回
//    public float wait = 0.4f; //等
//    public float go = 0.1f;  //去
//    public float posFloat;

//    //武将行动
//    public override IEnumerator MainAction()
//    {
//        if (MainOperation == null)
//            yield return MainOperation;
//        yield return Target.Respond(this);
//    }

//    public override IEnumerator Respond(ChessmanOperation obj)
//    {
//        yield return RespondOperation;
//    }

//    public override IEnumerator PreAction() => MoveToFrontOfCard();

//    public override IEnumerator PostAction() => MoveBack();

//    IEnumerator MoveToFrontOfCard()
//    {
//        var ui = Unit.cardObj;
//        var targetPos = Target.Obj.transform.position;
//        var rect = Target.Obj.GetComponent<RectTransform>();
//        var targetSize = rect.sizeDelta * rect.lossyScale;
//        var yPos = targetPos.y + (Unit.isPlayerCard ? -1 : 1) * targetSize.y;
//        var targetFrontPos = new Vector3(targetPos.x, yPos, targetPos.z);
//        var tween = ui.transform.DOMove(targetFrontPos, attackShakeTimeToGo * forward).SetEase(Ease.Unset);
//        yield return new DOTweenCYInstruction.WaitForCompletion(tween);
//    }

//    IEnumerator MoveBack()
//    {
//        var tween = Unit.cardObj.transform.DOLocalMove(Vector3.zero, back).SetEase(Ease.Unset);
//        yield return new DOTweenCYInstruction.WaitForCompletion(tween);
//    }
//}