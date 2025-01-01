using System.Net.Http.Headers;
using System.Text;

namespace YounBot.WynnCraftAPI4CSharp.Http.Implements;

public class DefaultHttpClient : IWynnHttpClient
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public void Shutdown()
    {
        _httpClient.Dispose();
    }

    public async Task<WynnCraftHttpResponse> MakeGetRequest(string url)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", IWynnHttpClient.DefaultUserAgent);
        return await GetResponseAsync(request);
    }

    public async Task<WynnCraftHttpResponse> MakePostRequest(string url, string payload)
    {
        HttpRequestMessage request = new(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("User-Agent", IWynnHttpClient.DefaultUserAgent);
        return await GetResponseAsync(request);
    }

    private async Task<WynnCraftHttpResponse> GetResponseAsync(HttpRequestMessage request)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            RateLimit rateLimit = CreateRateLimit(response);
            return new WynnCraftHttpResponse((int)response.StatusCode, responseBody, rateLimit);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error during HTTP request", ex);
        }
    }

    private RateLimit CreateRateLimit(HttpResponseMessage response)
    {
        HttpResponseHeaders headers = response.Headers;
        return new RateLimit(
            int.Parse(headers.GetValues("RateLimit-Remaining").FirstOrDefault() ?? "0"),
            int.Parse(headers.GetValues("RateLimit-Reset").FirstOrDefault() ?? "0"),
            int.Parse(headers.GetValues("RateLimit-Limit").FirstOrDefault() ?? "0")
        );
    }
}