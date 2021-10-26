using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 物件拖动信息的发射器，配合<see cref="IDragInputControl{T}"/>接口或者<see cref="DragInputControlController{T}"/>，发射给指定的控制器拖动指令来决定物件的操作和行为。
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class DragObjectSender<T> : MonoBehaviour
{
    protected IDragInputControl<T> Controller { get; private set; }
    protected abstract T ThisObj { get; }
    public EventTrigger EventTrigger { get; private set; }
    public virtual void Init()
    {
        if (EventTrigger)return;
        EventTrigger = gameObject.AddComponent<EventTrigger>();
        EventTrigger.triggers.Add(InstanceEntry(EventTriggerType.BeginDrag, BeginDrag));
        EventTrigger.triggers.Add(InstanceEntry(EventTriggerType.Drag, OnDrag));
        EventTrigger.triggers.Add(InstanceEntry(EventTriggerType.EndDrag, EndDrag));
    }

    private EventTrigger.Entry InstanceEntry(EventTriggerType type, UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(action);
        return entry;
    }

    public void SetController(IDragInputControl<T> controller) => Controller = controller;
    public virtual void BeginDrag(BaseEventData data) => Controller.StartDrag(data, ThisObj);
    public virtual void OnDrag(BaseEventData data) => Controller.OnDrag(data, ThisObj);
    public virtual void EndDrag(BaseEventData data) => Controller.EndDrag(data, ThisObj);
}