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

    void Start()
    {
        EventTrigger.enabled = true;
        Button.enabled = false;
    }
    public void OnDrag() => DebugLog();
    public void BeginDrag() => DebugLog();
    public void Drop() => DebugLog();
    public void EndDrag() => DebugLog();
    public void OnClick() => DebugLog();

    private void DebugLog([CallerMemberName] string methodName = null) => Debug.Log(methodName);
}
