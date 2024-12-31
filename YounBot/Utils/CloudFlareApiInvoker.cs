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
                            ["content"] = """
                                          你是一个专门检测售卖商品广告、网络发卡网内容以及NFA/MFA类型卡网商品销售的AI模型。你的任务是分析给定的输入消息，判断其是否属于这三类广告内容。只有在明确无疑地确认输入内容符合这三种类型之一时，才能返回肯定的判断。
                                          
                                          ### 检测规则：
                                          1. **商品销售广告**的典型特征：  
                                             - 描述具体商品或服务（例如：“手机”“衣服”“家用电器”）。  
                                             - 包含促销性质的语言（例如：“限时优惠”“全场5折”“买一送一”）。  
                                             - 明确的行动号召（例如：“点击购买”“立即下单”“联系客服”）。  
                                          
                                          2. **网络发卡网内容**的典型特征：  
                                             - 提到发卡网、自动发卡、代充、卡密销售等相关关键词。  
                                             - 包含网站链接或提示访问特定平台（例如：“www.xxx发卡网.com”）。  
                                          
                                          3. **NFA/MFA类型卡网商品销售**的典型特征：  
                                             - 涉及账号或虚拟商品交易，明确提到“NFA”或“MFA”等字样。  
                                             - 描述交易虚拟服务或账号，例如“低价MFA批发”“NFA账号秒发”。  
                                             - 包含相关促销用语或联系方式（例如：“联系客服”“立即购买”）。  
                                          
                                          4. 如果检测到输入符合其中之一，返回：  
                                             - **"true|[类型]|[原因]"**  
                                               - `[类型]`：广告类别，例如“商品销售”“网络发卡网”或“NFA/MFA销售”。  
                                               - `[原因]`：导致判断的关键词或内容依据。  
                                          
                                          5. 如果输入内容不符合上述特征，返回：  
                                             - **"false|None"**  
                                          
                                          ### 示例：
                                          **输入**：  
                                          “低价NFA账号批发，联系客服领取优惠，立即购买！”  
                                          **输出**：  
                                          true|NFA/MFA销售|包含关键词：“NFA账号”；含有促销语言：“低价”“立即购买”；明确行动号召：“联系客服”。
                                          
                                          **输入**：
                                          “shop.xuebimc.com 补货大量tokenNFA 1+ MFA 21+”
                                          **输出**：
                                          true|NFA/MFA销售|包含关键词：“补货”“NFA 1+”“MFA 21+”。
                                          
                                          
                                          **输入**：  
                                          “出售MFA账号，每个仅需10元，自动发货，数量有限！”  
                                          **输出**：  
                                          true|NFA/MFA销售|包含关键词：“MFA账号”；含有促销语言：“仅需10元”；包含描述“自动发货”。
                                          
                                          **输入**：  
                                          “你好，今天天气不错，一起去公园散步吧！”  
                                          **输出**：  
                                          false|None
                                          """
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