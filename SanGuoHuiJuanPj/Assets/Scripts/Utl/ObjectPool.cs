using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public interface IPoolObject
{
    void ObjReset();
}

public class ObjectPool<TObj> where TObj : IPoolObject
{
    public event UnityAction<TObj> OnGet;
    public event UnityAction<TObj> OnRecycle;
    public ObjectPool(Func<TObj> instanceFunc)
    {
        Instance = instanceFunc;
    }
    //key = obj, value = in used
    private Dictionary<TObj,bool> Pool { get; set; } = new Dictionary<TObj,bool>();

    public TObj Get()
    {
        var o = Pool.FirstOrDefault(e => !e.Value);
        var obj = o.Key;
        if (obj == null)
        {
            obj = Instance.Invoke();
            Pool.Add(obj, true);
        }
        else Pool[obj] = true;
        OnGet?.Invoke(obj);
        return obj;
    }

    public virtual void Recycle(TObj obj)
    {
        if (obj == null) return;
        OnRecycle?.Invoke(obj);
        obj.ObjReset();
        Pool[obj] = false;
    }

    private Func<TObj> Instance { get; }
}