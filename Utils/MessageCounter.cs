using HarmonyLib;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace YounBot.Utils;

public class MessageCounter
{
    public static List<long> MessageReceived = new();
    public static List<long> MessageSent = new();
    public static long AllMessageReceived = 0;
    public static long AllMessageSent = 0;

    public static void Init()
    {
        YounBotApp.Client!.Invoker.OnGroupMessageReceived += (_, _) => AddMessageReceived(DateTimeOffset.Now.ToUnixTimeSeconds());
        YounBotApp.Client!.Invoker.OnFriendMessageReceived += (_, _) => AddMessageReceived(DateTimeOffset.Now.ToUnixTimeSeconds());
        YounBotApp.Client!.Invoker.OnTempMessageReceived += (_, _) => AddMessageReceived(DateTimeOffset.Now.ToUnixTimeSeconds());
        
        var harmony = new Harmony("YounBot.MessageCounter");
        harmony.PatchAll();
    }
    
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
    
    [HarmonyPatch(typeof(OperationExt), "SendMessage")]
    public class SendMessagePatch
    {
        public static void Postfix(ref BotContext bot, ref MessageChain chain)
        {
            MessageSent.Add(DateTimeOffset.Now.ToUnixTimeSeconds());
            if (MessageSent.Count > 20000)
            {
                MessageSent.Remove(0);
            }

            AllMessageSent++;
        }
    }
}