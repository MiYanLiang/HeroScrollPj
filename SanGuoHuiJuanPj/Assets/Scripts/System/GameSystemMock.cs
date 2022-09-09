using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameSystemMock : GameSystem
{
    [SerializeField] private UnityEvent StartEvent;
    void Start()
    {
        Init();
        StartCoroutine(MockInit());
    }

    IEnumerator MockInit()
    {
        yield return new WaitUntil(() => IsInit);
        if(UIManager.instance!=null) UIManager.instance.Init(false);
        //EffectsPoolingControl.instance.Init();
        StartEvent.Invoke();
    }
}