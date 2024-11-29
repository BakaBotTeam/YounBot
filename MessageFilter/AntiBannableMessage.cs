using System.Reflection;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using YounBot.Utils;

namespace YounBot.MessageFilter;

public class AntiBannableMessage
{
    private static String[] bannableregexes { get; set; }
    
    public static void Init()
    {
        var assem = Assembly.GetExecutingAssembly();
        var resourceStream = assem.GetManifestResourceStream("YounBot.Resources.bannable.txt")!;
        var reader = new StreamReader(resourceStream);
        var lines = reader.ReadToEnd().Split("\n");
        bannableregexes = new String[lines.Length];
        for (var i = 0; i < lines.Length; i++)
        {
            bannableregexes[i] = lines[i].Replace("\n", "").Replace("\r", "");
        }
    }

    public static async Task OnGroupMessage(BotContext context, GroupMessageEvent @event)
    {
        var text = MessageUtils.GetPlainTextForCheck(@event.Chain);
        foreach (var keyword in bannableregexes)
        {
            if (text.Contains(keyword))
            {
                await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, @event.Chain.Sequence);
                var message = MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                    .Text("[消息过滤器] ").Mention(@event.Chain.FriendUin)
                    .Text($" Flagged FalseMessage | 你在聊啥?!");
                await context.MuteGroupMember(@event.Chain.GroupUin!.Value, @event.Chain.FriendUin, 3600);
                return;
            }
        }
    }
}