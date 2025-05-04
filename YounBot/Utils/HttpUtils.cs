using System.Text;
using System.Text.Json.Nodes;

namespace YounBot.Utils;

public class HttpUtils
{
    private static readonly HttpClient HttpClient = new();
    private static readonly Dictionary<string, KeyValuePair<long, string>> cache = new();

    public static async Task<JsonObject> GetJsonObject(string url, string? auth = null, Dictionary<string, string> headers = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url);
        
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
        request.Headers.Add("Connection", "keep-alive");

        if (!string.IsNullOrEmpty(auth))
        {
            string basicAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(auth));
            request.Headers.Add("Authorization", basicAuth);
        }
        
        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        
        if (cache.ContainsKey(url))
        {
            long now = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (now - cache[url].Key < 600)
            {
                return JsonObject.Parse(cache[url].Value).AsObject();
            }
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string raw = await response.Content.ReadAsStringAsync();
        cache[url] = new KeyValuePair<long, string>(DateTimeOffset.Now.ToUnixTimeSeconds(), raw);
        return JsonObject.Parse(raw).AsObject();
    }
    
    public static async Task<JsonObject> PostJsonObject(string url, string? auth = null, Dictionary<string, string> headers = null, JsonObject data = null)
    {
        HttpRequestMessage request = new(HttpMethod.Post, url);

        if (!string.IsNullOrEmpty(auth))
        {
            string basicAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(auth));
            request.Headers.Add("Authorization", basicAuth);
        }
        
        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        if (data != null)
        {
            request.Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string raw = await response.Content.ReadAsStringAsync();
        cache[url] = new KeyValuePair<long, string>(DateTimeOffset.Now.ToUnixTimeSeconds(), raw);
        return JsonObject.Parse(raw).AsObject();
    }
    
    public static async Task<JsonArray> GetJsonArray(string url, string? auth = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url);

        if (!string.IsNullOrEmpty(auth))
        {
            string basicAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(auth));
            request.Headers.Add("Authorization", basicAuth);
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string raw = await response.Content.ReadAsStringAsync();
        return JsonArray.Parse(raw).AsArray();
    }
    
    public static async Task<byte[]> GetBytes(string url, string? auth = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url);
        
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
        request.Headers.Add("Connection", "keep-alive");

        if (!string.IsNullOrEmpty(auth))
        {
            string basicAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(auth));
            request.Headers.Add("Authorization", basicAuth);
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }
    
    public static async Task<string> GetString(string url, string? auth = null, Dictionary<string, string>? headers = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url);

        if (!string.IsNullOrEmpty(auth))
        {
            string basicAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(auth));
            request.Headers.Add("Authorization", basicAuth);
        }
        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}