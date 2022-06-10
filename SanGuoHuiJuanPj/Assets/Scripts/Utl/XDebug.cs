using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Unity调试帮助类
/// </summary>
public static class XDebug
{
    private static bool isInit;
    private static ExceptionHandlerUi exceptionHandlerUi;
    public static void Init()
    {
        if (isInit)return;
        Application.logMessageReceived += Application_logMessageReceived;
        //打印log附加代码 
        AppDebugClass.DeleteOldAppLog();   //删除原先DebugFile 
    }

    public static void SubscribeHandler(ExceptionHandlerUi handler) => exceptionHandlerUi = handler;
    private static void Application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                exceptionHandlerUi.OnLogReceived(condition, stackTrace, type);
                break;
            case LogType.Assert:
                break;
            case LogType.Warning:
                break;
            case LogType.Log:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        AppDebugClass.LogForUnityLog(condition, stackTrace, type);
    }

    public static void StopWatch(Action action, string actionName = null, [CallerMemberName] string methodName = null)
    {
        var sw = new Stopwatch();
        sw.Start();
        action.Invoke();
        sw.Stop();
        Debug.Log($"{methodName}[{actionName}]() Time spend(ms): {sw.ElapsedMilliseconds}");
    }

    public static void Log<T>(string message,[CallerMemberName] string methodName = null,T type = default) where T : class
    {
        Debug.Log($"{type?.GetType().Name}:{methodName}() {message}");
    }
    public static void LogError<T>(string message,[CallerMemberName] string methodName = null,T type = default) where T : class
    {
        Debug.LogError($"{type?.GetType().Name}:{methodName}() {message}");
    }
    public static void LogError(string message,string typeName,[CallerMemberName] string methodName = null) 
    {
        Debug.LogError($"{typeName}:{methodName}() {message}");
    }
    public static void Log(Type type,string message,[CallerMemberName] string methodName = null)
    {
        Debug.Log($"{type.Name}:{methodName}() {message}");
    }

    public static XDebugException Throw<T>(string message, [CallerMemberName] string methodName = null,
        T type = default) where T : class => Throw(message, type?.GetType().Name, methodName);
    public static XDebugException Throw(string message,string typeName,[CallerMemberName] string methodName = null)
    {
        return new XDebugException(message, methodName, typeName);
    }
    public static XDebugException Throw(Exception exception,string message,string typeName,[CallerMemberName] string methodName = null)
    {
        return new XDebugException(exception, message, methodName, typeName);
    }

    public class XDebugException : Exception
    {
        public XDebugException(string message = null,string methodName = null,string typeName = null): base($"{typeName}.{methodName}():{message}")
        {
        }

        public XDebugException(Exception exception = null, string message = null, string methodName = null,
            string typeName = null) : base($"{typeName}.{methodName}():{message}", exception)
        {

        }
    }

}