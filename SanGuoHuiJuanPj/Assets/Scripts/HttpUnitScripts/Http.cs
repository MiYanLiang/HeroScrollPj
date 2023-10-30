using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CorrelateLib;
using UnityEngine.Networking;

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
    public static async void Post(string url,string content, Action<string> callbackAction,string callerLock,CancellationTokenSource cancellationTokenSource = null)
    {
        if(CallerIsLocked(callerLock))return;
        SetCallerLock(callerLock, true);
        var token = cancellationTokenSource?.Token ?? new CancellationTokenSource().Token;
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

    public static async Task<HttpResponseMessage> GetAsync(string url, CancellationToken token = default) =>
        await SendAsync(HttpMethod.Get, url, string.Empty, token);
    public static async Task<T> PostAsync<T>(string url, string content) where T : class
    {
        var response = await PostAsync(url, content);
        return response.IsSuccess() ? Json.Deserialize<T>(await response.Content.ReadAsStringAsync()) : null;
    }

    public static async Task<HttpResponseMessage> PostAsync(string url, string content,
        CancellationToken token = default) => await SendAsync(HttpMethod.Post, url, content, token);

    private static async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string content, CancellationToken token = default)
    {
        try
        {
            var client = Server.InstanceClient();
            var request = new HttpRequestMessage(method, url);
            if(HttpMethod.Get != method)
                request.Content = new StringContent(content, Encoding.UTF8, "application / json");
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            throw;
#endif
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }
    
    public static async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string content,(string key,string value)[] headers,CancellationToken token = default)
    {
        try
        {
            var client = Server.InstanceClient();
            foreach ((string key, string value) header in headers)
                client.DefaultRequestHeaders.Add(header.key, header.value);
            var request = new HttpRequestMessage(method, url);
            if(HttpMethod.Get != method)
                request.Content = new StringContent(content, Encoding.UTF8, "application / json");
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            throw;
#endif
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }

}

public static class HttpResponseMessageExtension
{
    public static bool IsSuccess(this HttpResponseMessage response) =>
        (response.IsSuccessStatusCode && response.StatusCode == 0) || response.IsSuccessStatusCode;
}