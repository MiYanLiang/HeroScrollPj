using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CorrelateLib;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class Http
{
    public static IReadOnlyDictionary<string, bool> BusyLock => busyLock;
    private static Dictionary<string, bool> busyLock = new Dictionary<string, bool>();
    public static async void Get(string url, Action<string> callbackAction,bool isApi,string callerLock)
    {
        if(CallerIsLocked(callerLock))return;
        SetCallerLock(callerLock, true);
        var token = new CancellationTokenSource().Token;
        token.Register(() => SetCallerLock(callerLock, false));
        var result = await GetAsync(url, isApi);
        var msg = result.data;
        SetCallerLock(callerLock, false);
        callbackAction(msg);
    }
    public static async void Post(string url,string content, Action<string> callbackAction,bool isApi,string callerLock,CancellationTokenSource cancellationTokenSource = null)
    {
        if(CallerIsLocked(callerLock))return;
        SetCallerLock(callerLock, true);
        var token = cancellationTokenSource?.Token ?? new CancellationTokenSource().Token;
        token.Register(() => SetCallerLock(callerLock, false));
        var result = await PostAsync(url, content, isApi);
        var msg = result.data;
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

    public static async Task<T> GetAsync<T>(string url, bool isApi) where T : class
    {
        var response = await GetAsync(url, isApi);
        return response.isSuccess ? Json.Deserialize<T>(response.data) : null;
    }

    public static async Task<(bool isSuccess, HttpStatusCode code, string data)> GetAsync(string url, bool isApi) =>
        await SendAsync(HttpMethod.Get, url, string.Empty);
    public static async Task<T> PostAsync<T>(string url, string content,bool isApi) where T : class
    {
        var response = await PostAsync(url, content, isApi);
        return response.isSuccess ? Json.Deserialize<T>(response.data) : null;
    }

    public static async Task<(bool isSuccess, HttpStatusCode code, string data)> PostAsync(string url, string content,
        bool isApi) => await SendAsync(HttpMethod.Post, url, content);

    public static async Task<(bool isSuccess, HttpStatusCode code, string data)> SendAsync(HttpMethod method,
        string url, string content)
    {
        UnityWebRequest request = null;

        try
        {
            // 构建完整的 URL
            var serverUrl = Server.GameServer.Trim("/api/".ToCharArray());
            serverUrl += "/api/";
            // 构建完整的 URL
            string fullUrl = url.StartsWith("http") ? url : serverUrl + url;
            // 创建 UnityWebRequest 实例
            if (method == HttpMethod.Get)
            {
                request = UnityWebRequest.Get(fullUrl);
            }
            else if (method == HttpMethod.Post)
            {
                request = new UnityWebRequest(fullUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(content);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
            }
            // 你可以根据需要添加更多的 HTTP 方法处理

            request.certificateHandler = new BypassCertificate(); // 忽略证书验证
            // 发送请求
            await request.SendWebRequest();

            // 检查请求是否有错误
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error: {request.error}");
                return (false, HttpStatusCode.ServiceUnavailable, request.error); // 返回错误状态码和错误信息
            }

            // 返回状态码和响应数据
            HttpStatusCode statusCode = GetResponseCode(request);
            string responseData = request.downloadHandler.text;
            return (true, statusCode, responseData);
        }
        catch (UnityWebRequestException e)
        {
            return (false, (HttpStatusCode)e.ResponseCode, null);// new UnityWebRequest { result = UnityWebRequest.Result.ConnectionError }; // 返回错误的响应
        }
        catch (Exception e)
        {
            return (false, (HttpStatusCode)UnityWebRequest.Result.ConnectionError, null);// new UnityWebRequest { result = UnityWebRequest.Result.ConnectionError }; // 返回错误的响应
        }
        finally
        {
            // 确保请求在处理完后被释放
            request?.Dispose();
        }
    }

    private static HttpStatusCode GetResponseCode(UnityWebRequest request) => (HttpStatusCode)request.responseCode;
    //    private static async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string content, CancellationToken token = default)
    //    {
    //        try
    //        {
    //            var client = new HttpClient(); //Server.InstanceClient();
    //            client.BaseAddress = new Uri(Server.GameServer);
    //            var request = new HttpRequestMessage(method, url);
    //            if(HttpMethod.Get != method)
    //                request.Content = new StringContent(content, Encoding.UTF8, "application / json");
    //            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
    //        }
    //        catch (Exception e)
    //        {
    //#if UNITY_EDITOR
    //            throw;
    //#endif
    //            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    //        }
    //    }

    public static async Task<(bool isSuccess, HttpStatusCode code, string data)> SendAsync(HttpMethod method, string url, string content, (string key, string value)[] headers)
    {
        UnityWebRequest request = null;

        try
        {
            var serverUrl = Server.GameServer.Trim("/api/".ToCharArray());
            serverUrl += "/api/";
            // 构建完整的 URL
            string fullUrl = url.StartsWith("http") ? url : serverUrl + url;

            // 创建 UnityWebRequest 实例
            if (method == HttpMethod.Get)
            {
                request = UnityWebRequest.Get(fullUrl);
            }
            else if (method == HttpMethod.Post)
            {
                request = new UnityWebRequest(fullUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(content);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
            }
            // 你可以根据需要添加更多的 HTTP 方法处理

            // 添加请求头
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.key, header.value);
            }

            request.certificateHandler = new BypassCertificate(); // 忽略证书验证
            // 发送请求
            var operation = request.SendWebRequest();

            // 等待请求完成
            while (!operation.isDone)
            {
                await Task.Yield(); // 使用Task.Yield()以避免阻塞主线程
            }

            var code = GetResponseCode(request);
            // 检查请求是否有错误
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error: {request.error}");
                return (false,code,request.downloadHandler.text); // 或者根据你的需要返回特定的错误处理
            }

            // 返回请求对象，包含响应数据
            return (true, GetResponseCode(request), request.downloadHandler.text);
        }
        catch (UnityWebRequestException e)
        {
            return (false, (HttpStatusCode)e.ResponseCode, null);// new UnityWebRequest { result = UnityWebRequest.Result.ConnectionError }; // 返回错误的响应
        }
        catch (Exception e)
        {
            return (false, (HttpStatusCode)UnityWebRequest.Result.ConnectionError, null);// new UnityWebRequest { result = UnityWebRequest.Result.ConnectionError }; // 返回错误的响应
        }
        finally
        {
            // 确保请求在处理完后被释放
            request?.Dispose();
        }
    }

    //    public static async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string content,(string key,string value)[] headers,CancellationToken token = default)
    //    {
    //        try
    //        {
    //            var client = Server.InstanceClient();
    //            foreach ((string key, string value) header in headers)
    //                client.DefaultRequestHeaders.Add(header.key, header.value);
    //            var request = new HttpRequestMessage(method, url);
    //            if(HttpMethod.Get != method)
    //                request.Content = new StringContent(content, Encoding.UTF8, "application / json");
    //            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
    //        }
    //        catch (Exception e)
    //        {
    //#if UNITY_EDITOR
    //            throw;
    //#endif
    //            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    //        }
    //    }
    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // 在此处加载你的服务器证书并进行验证
            return true; // 接受所有证书，开发阶段使用
        }
    }
}

public static class HttpResponseMessageExtension
{
    public static bool IsSuccess(this HttpResponseMessage response) =>
        (response.IsSuccessStatusCode && response.StatusCode == 0) || response.IsSuccessStatusCode;
}