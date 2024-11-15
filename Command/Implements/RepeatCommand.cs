using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace YounBot.Command.Implements;

public class RepeatCommand
{
    
    [Command("repeat", "Repeat what you've said")]
    public async void Repeat(BotContext context, MessageChain chain, string num)
    {
        await SendMessage(context, chain, num);
    }
    
    private async Task SendMessage(BotContext context, MessageChain chain, string message, bool mention = false)
    {
        if (mention)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Mention(chain.FriendUin).Text($" {message}").Build());
        }
        else
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text(message).Build());
        }
    }
}