using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CorrelateLib;

public static class Http
{
    public static IReadOnlyDictionary<string, bool> BusyLock => busyLock;
    private static Dictionary<string, bool> busyLock = new Dictionary<string, bool>();
    public static async void Get(string url, Action<string> callbackAction,string callerLock)
    {
        if(CallerIsLocked(callerLock))return;
        SetCallerLock(callerLock, true);
        var token = new CancellationTokenSource().Token;
        token.Register(() => SetCallerLock(callerLock, false));
        var result = await GetAsync(url, token);
        var msg = await result.Content.ReadAsStringAsync();
        SetCallerLock(callerLock, false);
        callbackAction(msg);
    }
    public static async void Post(string url,string content, Action<string> callbackAction,string callerLock)
    {
        if(CallerIsLocked(callerLock))return;
        SetCallerLock(callerLock, true);
        var token = new CancellationTokenSource().Token;
        token.Register(() => SetCallerLock(callerLock, false));
        var result = await PostAsync(url, content, token);
        var msg = await result.Content.ReadAsStringAsync();
        SetCallerLock(callerLock, false);
        callbackAction(msg);
    }

    private static void SetCallerLock(string callerLock, bool isLocked)
    {
        if (!busyLock.ContainsKey(callerLock))
            busyLock.Add(callerLock, default);
        busyLock[callerLock] = isLocked;
    }

    private static bool CallerIsLocked(string callerLock) => busyLock.ContainsKey(callerLock) && busyLock[callerLock];

    public static async Task<T> GetAsync<T>(string url) where T : class
    {
        var response = await GetAsync(url);
        return response.IsSuccess() ? Json.Deserialize<T>(await response.Content.ReadAsStringAsync()) : null;
    }

    public static async Task<HttpResponseMessage> GetAsync(string url,CancellationToken token = default)
    {
        try
        {
            var client = Server.InstanceClient();
            return await client.GetAsync(url, token);
        }
        catch (Exception)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

    }

    public static async Task<T> PostAsync<T>(string url, string content) where T : class
    {
        var response = await PostAsync(url, content);
        return response.IsSuccess() ? Json.Deserialize<T>(await response.Content.ReadAsStringAsync()) : null;
    }

    public static async Task<HttpResponseMessage> PostAsync(string url, string content, CancellationToken token = default)
    {
        try
        {
            var client = Server.InstanceClient();
            return await client.PostAsync(url, new StringContent(content, Encoding.UTF8, "application / json"), token);
        }
        catch (Exception)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }

}

public static class HttpResponseMessageExtension
{
    public static bool IsSuccess(this HttpResponseMessage response) =>
        (response.IsSuccessStatusCode && response.StatusCode == 0) || response.IsSuccessStatusCode;
}