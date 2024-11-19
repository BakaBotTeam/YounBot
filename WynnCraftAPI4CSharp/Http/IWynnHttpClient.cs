namespace YounBot.WynnCraftAPI4CSharp.Http;

public interface IWynnHttpClient
{
    public const string DefaultUserAgent = "WynnCraft4CSharp/1.0";

    Task<WynnCraftHttpResponse> MakeGetRequest(string url);
    
    Task<WynnCraftHttpResponse> MakePostRequest(string url, string payload);

    void Shutdown();
}