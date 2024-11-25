using HarmonyLib;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace YounBot.Utils;

public class Hookers
{
    public static void Init()
    {
        var harmony = new Harmony("YounBotHookers");
        harmony.PatchAll();
    }
    
    [HarmonyPatch(typeof(OperationExt), "SendMessage")]
    public class SendMessagePatch
    {
        public static void Postfix(ref BotContext bot, ref MessageChain chain)
        {
            MessageCounter.MessageSent.Add(DateTimeOffset.Now.ToUnixTimeSeconds());
            if (MessageCounter.MessageSent.Count > 20000)
            {
                MessageCounter.MessageSent.Remove(0);
            }

            MessageCounter.AllMessageSent++;
        }
    }
}