namespace YounBot.WynnCraftAPI4CSharp.Utils;

public class WynnCraftException : Exception
{
    public int StatusCode { get; }

    public WynnCraftException(int statusCode, string message, Exception? innerException = null)
        : base($"[{statusCode}] {message}", innerException)
    {
        StatusCode = statusCode;
    }
}