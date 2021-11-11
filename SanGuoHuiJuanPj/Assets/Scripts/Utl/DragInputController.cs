using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 拖动控制器，透过<see cref="DragObjectSender{T}"/>传来的拖动物件信息，操作或控制各种状态的拖动指令。
/// 以便满足不同控件的的交互与应用场景的解耦关系。
/// </summary>
public abstract class DragInputControlController<T> : MonoBehaviour, IDragInputControl<T>
{
    protected const string Mouse_X = "Mouse X";
    protected const string Mouse_Y = "Mouse Y";

    public abstract void StartDrag(BaseEventData eventData, T obj);
    public abstract void OnDrag(BaseEventData eventData, T obj);
    public abstract void EndDrag(BaseEventData eventData, T obj);
    public virtual void PointerDown(BaseEventData data, T obj) { }
    public virtual void PointerUp(BaseEventData data, T obj) { }

    protected List<RaycastResult> GetRayCastResults(PointerEventData pointer)
    {
        var list = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, list);
        return list;
    }
}
/// <summary>
/// 拖动发射器接口，透过<see cref="DragInputControlController{T}"/>信息
/// 来控制输出结果。
/// </summary>
public interface IDragInputControl<in T>
{
    void StartDrag(BaseEventData eventData, T obj);
    void OnDrag(BaseEventData eventData, T obj);
    void EndDrag(BaseEventData eventData, T obj);
    void PointerDown(BaseEventData data, T obj);
    void PointerUp(BaseEventData data, T obj);
}