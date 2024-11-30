namespace YounBot.WynnCraftAPI4CSharp.Http;

public class RateLimit
{
    private readonly int _remaining;
    private readonly int _reset;
    private readonly int _limit;
    private readonly DateTime _resetAt;

    public RateLimit(int remaining, int reset, int limit)
    {
        _remaining = remaining;
        _reset = reset;
        _limit = limit;
        _resetAt = DateTime.UtcNow.AddSeconds(reset);
    }

    public int Remaining => _remaining;

    public int Reset => _reset;

    public int Limit => _limit;

    public DateTime ResetAt => _resetAt;
}