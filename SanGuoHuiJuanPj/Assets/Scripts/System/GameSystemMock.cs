using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameSystemMock : GameSystem
{
    [SerializeField] private UnityEvent StartEvent;
    [SerializeField] private bool AutoStart;
    void Start()
    {
#if UNITY_EDITOR
        if (AutoStart) Init();
#endif
        SignalRClient.Init();
    }

#if UNITY_EDITOR
    public override void Init()
    {
        base.Init();
        StartCoroutine(MockInit());
    }
#endif

    IEnumerator MockInit()
    {
        yield return new WaitUntil(() => IsInit);
        if(UIManager.instance!=null) UIManager.instance.Init(false);
        //EffectsPoolingControl.instance.Init();
        StartEvent.Invoke();
    }
}