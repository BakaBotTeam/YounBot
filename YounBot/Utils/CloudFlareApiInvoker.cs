namespace YounBot.Utils;

public static class CloudFlareApiInvoker
{
    public static async Task<string> InvokeAiTask(string content, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        string? basicAuth = YounBotApp.Config!.WorkersAiBasicAuth;
        string? url = YounBotApp.Config!.WorkersAiUrl;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                return (await HttpUtils.GetJsonArray($"{url}/?content={content}", basicAuth))[0]["response"]["response"].GetValue<string>();
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= maxRetries)
                {
                    throw new Exception($"Failed after {maxRetries} attempts", ex);
                }
                await Task.Delay(delayMilliseconds);
            }
        }

        throw new Exception("Unexpected error in retry logic");
    }
}