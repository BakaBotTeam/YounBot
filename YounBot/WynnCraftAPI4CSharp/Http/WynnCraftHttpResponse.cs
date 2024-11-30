namespace YounBot.WynnCraftAPI4CSharp.Http;

public class WynnCraftHttpResponse
{
    public int StatusCode { get; }
    public string Body { get; }
    public RateLimit RateLimit { get; }
    
    public WynnCraftHttpResponse(int statusCode, string body, RateLimit rateLimit)
    {
        StatusCode = statusCode;
        Body = body;
        RateLimit = rateLimit;
    }
}