using System.Text;
using System.Text.Json.Nodes;

namespace YounBot.Utils;

public static class CloudFlareApiInvoker
{
    private static HttpClient _httpClient = new();
    public static async Task<string> InvokeAiTask(string content, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        Exception lastException = new("Failed to invoke AI task");
        string raw = "";
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                string url =
                    $"https://gateway.ai.cloudflare.com/v1/{YounBotApp.Config!.CloudFlareAccountID}/{YounBotApp.Config!.CloudFlareGatewayID}/workers-ai/@cf/qwen/qwen1.5-14b-chat-awq";
                string auth = $"Bearer {YounBotApp.Config!.CloudFlareAuthToken}";
                JsonObject data = new()
                {
                    ["messages"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["role"] = "system",
                            ["content"] = "你是一个文本内容检测专家 你的任务是分析给定的消息并判断其是否包含以下类型的违规内容\n1 广告\n2 政治敏感内容\n\n你的输出必须严格遵循以下要求\n1 如果消息包含违规内容 请输出 true|违规类型|判断理由\n2 如果消息不包含违规内容 请输出 false|无\n\n注意\n违规类型只能是广告或政治敏感内容 判断理由必须清晰简洁 不得包含消息中的任何原句文本 对于政治敏感内容的判断 不得使用任何可能的政治敏感关键词 仅需描述判断的逻辑依据或模式 你的回复必须使用中文\n如果只是一些游戏平台的网址则不应该被判断为广告\n你的输出必须严格遵循以下要求\n1 如果消息包含违规内容 请输出 true|违规类型|判断理由\n2 如果消息不包含违规内容 请输出 false|无"
                        },
                        new JsonObject
                        {
                            ["role"] = "user",
                            ["content"] = content
                        }
                    }
                };
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Authorization", auth);
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                raw = await response.Content.ReadAsStringAsync();
                JsonObject responseJson = JsonObject.Parse(raw).AsObject();
                return responseJson["result"]["response"].GetValue<string>();
            }
            catch (Exception e)
            {
                lastException = new Exception("Failed to invoke AI task: " + raw, e);
            }
            finally
            {
                await Task.Delay(delayMilliseconds);
            }
        }

        throw new Exception("Failed to invoke AI task", lastException);
    }
    
    public static async Task<byte[]> InvokeCustomAiTask(string model, JsonObject data, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        Exception lastException = new("Failed to invoke AI task");
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                string url =
                    $"https://gateway.ai.cloudflare.com/v1/{YounBotApp.Config!.CloudFlareAccountID}/{YounBotApp.Config!.CloudFlareGatewayID}/workers-ai/{model}";
                string auth = $"Bearer {YounBotApp.Config!.CloudFlareAuthToken}";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Authorization", auth);
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
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