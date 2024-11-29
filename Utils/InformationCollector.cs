namespace YounBot.Utils;

public class InformationCollector
{
    public static Dictionary<long, long> MessageInvokeCount = new();
    
    public static double GetAvgMessageInvokeCountMinutes(int minutes = 1)
    {
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        var count = MessageInvokeCount.Count(time => time.Key >= now - (60 * minutes));
        var sum = MessageInvokeCount.Where(time => time.Key >= now - (60 * minutes)).Sum(time => time.Value);
        return Math.Round((double)sum / count, 2);
    }
}