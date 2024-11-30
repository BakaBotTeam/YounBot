namespace YounBot.Utils;

public class InformationCollector
{
    public static Dictionary<long, long> MessageInvokeCount = new();
    
    public static double GetAvgMessageInvokeCountMinutes(int minutes = 1)
    {
        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        int count = MessageInvokeCount.Count(time => time.Key >= now - (60 * minutes));
        long sum = MessageInvokeCount.Where(time => time.Key >= now - (60 * minutes)).Sum(time => time.Value);
        return Math.Round((double)sum / count, 2);
    }
}