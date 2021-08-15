using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonTest : MonoBehaviour
{
    public EventTrigger EventTrigger;
    public Button Button;
    public int test;

    void Start()
    {
        StartCoroutine(coroutineA());
    }

    IEnumerator coroutineA()
    {
        // wait for 1 second
        Debug.Log($"coroutineA created..{test++}");
        yield return new WaitForSeconds(1.0f);
        yield return coroutineB();
        Debug.Log($"coroutineA running again{test++}");
    }

    IEnumerator coroutineB()
    {
        Debug.Log($"coroutineB created{test++}");
        yield return new WaitForSeconds(2.5f);
        Debug.Log($"coroutineB enables coroutineA to run{test++}");
    }
}