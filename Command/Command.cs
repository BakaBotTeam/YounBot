using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace YounBot.Command;

public abstract class Command(string name, string description)
{
    public readonly string Name = name;
    public readonly string Description = description;
    public abstract Task Execute(BotContext context, MessageChain chain, string[] args);

    protected async Task SendMessage(BotContext context, MessageChain chain, string message, bool mention = false)
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