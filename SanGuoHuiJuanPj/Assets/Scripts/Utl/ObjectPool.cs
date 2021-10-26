using System;
using System.Collections.Generic;
using System.Linq;
public interface IPoolObject
{
    void ObjReset();
}
public class ObjectPool<TObj> where TObj : IPoolObject
{
    public ObjectPool(Func<TObj> instanceFunc)
    {
        Instance = instanceFunc;
    }
    public List<TObj> Pool { get; set; } = new List<TObj>();
    public List<TObj> Uses { get; set; } = new List<TObj>();

    public TObj Get()
    {
        var obj = Pool.FirstOrDefault();
        if (obj == null) obj = Instance.Invoke();
        Uses.Add(obj);
        return obj;
    }

    public void Recycle(TObj obj)
    {
        obj.ObjReset();
        Uses.Remove(obj);
        Pool.Add(obj);
    }

    private Func<TObj> Instance { get; }
}