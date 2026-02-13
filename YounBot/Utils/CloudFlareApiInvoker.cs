using System.Text;
using System.Text.Json.Nodes;

namespace YounBot.Utils;

public static class CloudFlareApiInvoker
{
    private static HttpClient _httpClient = new();

    public static async Task<JsonObject> InvokeCustomOpenAiTask(
        JsonObject data,
        int maxRetries = 3,
        int delayMilliseconds = 1000,
        string endpoint = "/chat/completions",
        string? customEndpoint = null,
        string? customApiKey = null)
    {
        Exception lastException = new("Failed to invoke AI task");
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                // 使用自定义端点或配置中的端点
                string baseUrl = customEndpoint ?? YounBotApp.Config!.CustomOpenAiEndpoint;
                // 确保 URL 不以斜杠结尾
                baseUrl = baseUrl.TrimEnd('/');
                // 确保 endpoint 以斜杠开头
                if (!endpoint.StartsWith('/'))
                {
                    endpoint = "/" + endpoint;
                }
                string url = baseUrl + endpoint;

                // 使用自定义 API Key 或配置中的 API Key
                string apiKey = customApiKey ?? YounBotApp.Config!.CustomOpenAiApiKey;
                string auth = $"Bearer {apiKey}";

                HttpRequestMessage request = new(HttpMethod.Post, url)
                {
                    Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Authorization", auth);

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string raw = await response.Content.ReadAsStringAsync();
                return JsonObject.Parse(raw).AsObject();
            }
            catch (Exception e)
            {
                lastException = new Exception("Failed to invoke AI task", e);
            }
            finally
            {
                await Task.Delay(delayMilliseconds);
            }
        }

        throw new Exception("Failed to invoke AI task", lastException);
    }
}