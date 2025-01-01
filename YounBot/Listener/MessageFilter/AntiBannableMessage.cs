using System.Reflection;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using YounBot.Utils;

namespace YounBot.Listener.MessageFilter;

public class AntiBannableMessage
{
    private static String[] bannableregexes { get; set; }
    
    public static void Init()
    {
        Assembly assem = Assembly.GetExecutingAssembly();
        Stream resourceStream = assem.GetManifestResourceStream("YounBot.Resources.bannable.txt")!;
        StreamReader reader = new(resourceStream);
        string[] lines = reader.ReadToEnd().Split("\n");
        bannableregexes = new String[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            bannableregexes[i] = lines[i].Replace("\n", "").Replace("\r", "");
        }
    }

    public static async Task OnGroupMessage(BotContext context, GroupMessageEvent @event)
    {
        if (!(await BotUtils.HasEnoughPermission(@event.Chain))) return;
        
        string text = MessageUtils.GetPlainTextForCheck(@event.Chain);
        foreach (string keyword in bannableregexes)
        {
            if (text.Contains(keyword))
            {
                await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, @event.Chain.Sequence);
                MessageBuilder message = MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                    .Text("[消息过滤器] ").Mention(@event.Chain.FriendUin)
                    .Text($" Flagged FalseMessage | 你在聊啥?!");
                await context.MuteGroupMember(@event.Chain.GroupUin!.Value, @event.Chain.FriendUin, 3600);
                return;
            }
        }
    }
}