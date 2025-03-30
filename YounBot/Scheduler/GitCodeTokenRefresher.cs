using System.Net;
using System.Text.Json.Nodes;

namespace YounBot.Scheduler;

public class GitCodeTokenRefresher
{
    public static async Task Refresh()
    {
        HttpClient client = new();
        string refreshToken = YounBotApp.Configuration["GitCodeRefreshToken"] ?? throw new NullReferenceException("GitCodeRefreshToken is not configured.");
        HttpRequestMessage request = new(HttpMethod.Get, "https://web-api.gitcode.com/uc/api/v1/user/oauth/token");
        request.Headers.Add("origin", "https://gitcode.com");
        request.Headers.Add("referer", "https://gitcode.com/");
        request.Headers.Add("cookies", "GITCODE_REFRESH_TOKEN=" + refreshToken);
        request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.0.0");
        request.Headers.Add("accept", "application/json");
        HttpResponseMessage response = client.Send(request);
        response.EnsureSuccessStatusCode();
        JsonObject json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        string accessToken = json["access_token"].GetValue<string>();
        refreshToken = json["refresh_token"].GetValue<string>();
        YounBotApp.Configuration["GitCodeAccessToken"] = accessToken;
        YounBotApp.Configuration["GitCodeRefreshToken"] = refreshToken;
    }
}