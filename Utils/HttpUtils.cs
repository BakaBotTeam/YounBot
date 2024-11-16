using System.Text;
using System.Text.Json.Nodes;

namespace YounBot.Utils;

public class HttpUtils
{
    private static readonly HttpClient HttpClient = new HttpClient();

    public static async Task<JsonObject> GetJsonObject(string url, string? auth = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrEmpty(auth))
        {
            string basicAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(auth));
            request.Headers.Add("Authorization", basicAuth);
        }

        HttpResponseMessage response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string raw = await response.Content.ReadAsStringAsync();
        return (JsonObject) JsonObject.Parse(raw);
    }
}