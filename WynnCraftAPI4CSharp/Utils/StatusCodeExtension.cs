namespace YounBot.WynnCraftAPI4CSharp.Utils;

public static class StatusCodeExtensions
{
    public static int GetStatusCode(this StatusCode statusCode)
    {
        return (int)statusCode;
    }

    public static bool Is(this StatusCode current, int statusCode)
    {
        return (int)current == statusCode;
    }
}