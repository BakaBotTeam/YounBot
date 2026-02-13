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
                JsonObject data = new()
                {
                    ["model"] = "grok-3-fast",
                    ["messages"] = new JsonArray
                    {
                        new JsonObject()
                        {
                            ["role"] = "system",
                            ["content"] = """
                                          请严格判断以下内容是否为广告或包含政治敏感内容。检测标准：
                                          包含推广产品/服务/群组的信息
                                          出现网址、联系方式或邀请加入群组
                                          使用促销性描述（如"价格优惠""补货""最新"等）
                                          列举产品功能优势（带+或✓符号列表）
                                          涉及游戏作弊工具/外挂内容（如Bypass、Killaura、Blink等术语）
                                          包含明确的诱导行为（如"进群""加我""获取"等）
                                          包含政治敏感内容（如涉及政府、政党、政策、领导人、敏感事件等）
                                          
                                          判断步骤：
                                          首先检查是否包含政治敏感内容，如果是则判定为true
                                          否则检查是否包含至少3个上述广告特征（前6项）
                                          确认主要目的是否为推广
                                          排除普通用户讨论产品的情况
                                          只是单纯的发个网址的情况不算广告
                                          
                                          返回格式（严格遵循）：
                                          [true|false]|[原因（不超过6字）]
                                          
                                          示例响应：
                                          true|推广作弊工具
                                          true|政治敏感
                                          false|无
                                          请严格按标准判断以下内容：
                                          """
                        },
                        new JsonObject
                        {
                            ["role"] = "user",
                            ["content"] = content
                        }
                    }
                };
                JsonObject response = await InvokeGrokTask(data);
                return response["choices"][0]["message"]["content"].GetValue<string>();
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
                HttpRequestMessage request = new(HttpMethod.Post, url)
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
    
    public static async Task<JsonObject> InvokeGrokTask(JsonObject data, int maxRetries = 3, int delayMilliseconds = 1000, string endpoint = "/v1/chat/completions")
    {
        Exception lastException = new("Failed to invoke AI task");
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                string url =
                    $"https://gateway.ai.cloudflare.com/v1/{YounBotApp.Config!.CloudFlareAccountID}/{YounBotApp.Config!.CloudFlareGatewayID}/grok{endpoint}";
                string auth = $"Bearer {YounBotApp.Config!.GrokApiKey}";
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
    
    public static async Task<JsonObject> InvokeDeepSeekTask(JsonObject data, int maxRetries = 3, int delayMilliseconds = 1000, string endpoint = "/chat/completions")
    {
        Exception lastException = new("Failed to invoke AI task");
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                string url =
                    $"https://gateway.ai.cloudflare.com/v1/{YounBotApp.Config!.CloudFlareAccountID}/{YounBotApp.Config!.CloudFlareGatewayID}/deepseek{endpoint}";
                string auth = $"Bearer {YounBotApp.Config!.DeepSeekApiKey}";
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

    /// <summary>
    /// 调用自定义 OpenAI 兼容端点
    /// </summary>
    /// <param name="data">请求数据，应包含 model 和 messages 等字段</param>
    /// <param name="maxRetries">最大重试次数</param>
    /// <param name="delayMilliseconds">重试延迟（毫秒）</param>
    /// <param name="endpoint">API 端点路径，默认为 /chat/completions</param>
    /// <param name="customEndpoint">自定义的完整端点 URL，如果提供则忽略 endpoint 参数</param>
    /// <param name="customApiKey">自定义 API Key，如果不提供则使用配置文件中的值</param>
    /// <returns>返回 API 响应的 JSON 对象</returns>
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