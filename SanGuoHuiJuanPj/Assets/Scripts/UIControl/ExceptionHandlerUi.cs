using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class ExceptionHandlerUi : MonoBehaviour
{
    public Text ExceptionMessage;
    public Text StackTrace;

    public void Init()
    {
#if UNITY_EDITOR
#endif
        XDebug.SubscribeHandler(this);
    }

    public void OnLogReceived(string condition, string stacktrace, LogType type)
    {
        var now = SysTime.Now;
        var timeText = $"{now.Year - 2000}.{now.Month}.{now.Day}";
        var username = GamePref.Username;
        var filtered = username.Split("yx").LastOrDefault();
        filtered = filtered?.Split("hj").LastOrDefault();
        var userid = int.TryParse(filtered, out var id)? id - 10000 : 0;
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                gameObject.SetActive(true);
                ExceptionMessage.text = condition;
                StackTrace.text = $"v{Application.version}.{timeText}.{userid}\n{stacktrace}";
                Time.timeScale = 0;
                break;
        }
    }

    public void ApplicationQuit() => Application.Quit();
}
