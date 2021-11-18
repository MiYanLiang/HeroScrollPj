using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CorrelateLib;
using Newtonsoft.Json;
using UnityEngine;

public static class Http
{
    public static async Task<T> GetAsync<T>(string url) where T : class
    {
        var response = await GetAsync(url);
        return response.IsSuccess() ? Json.Deserialize<T>(await response.Content.ReadAsStringAsync()) : null;
    }

    public static async Task<HttpResponseMessage> GetAsync(string url)
    {
        try
        {
            var client = Server.InstanceClient();
            return await client.GetAsync(url);
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