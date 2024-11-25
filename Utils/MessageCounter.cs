namespace YounBot.Utils;

public class MessageCounter
{
    public static List<long> MessageReceived = new();
    public static List<long> MessageSent = new();
    public static long AllMessageReceived = 0;
    public static long AllMessageSent = 0;
    
    public static void AddMessageReceived(long time)
    {
        MessageReceived.Add(time);
        if (MessageReceived.Count > 20000)
        {
            MessageReceived.Remove(0);
        }

        AllMessageReceived++;
    }

    public static double GetReceivedMessageLastMinutes(int minutes = 1)
    {
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        return MessageReceived.Count(time => time >= now - (60 * minutes));
    }
    
    public static double GetSentMessageLastMinutes(int minutes = 1)
    {
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        return MessageSent.Count(time => time >= now - (60 * minutes));
    }
}