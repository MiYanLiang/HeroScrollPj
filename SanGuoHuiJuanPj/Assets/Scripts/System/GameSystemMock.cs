using System.Collections;
using UnityEngine;

public class GameSystemMock : GameSystem
{
    void Start()
    {
        Init();
        StartCoroutine(MockInit());
    }

    IEnumerator MockInit()
    {
        yield return new WaitUntil(() => IsInit);
        if(UIManager.instance!=null) UIManager.instance.Init();
        //EffectsPoolingControl.instance.Init();
    }
}