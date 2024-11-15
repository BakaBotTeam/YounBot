using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace YounBot.Command;

public abstract class Command(string name, string description)
{
    public readonly string Name = name;
    public readonly string Description = description;
    public abstract Task Execute(BotContext context, MessageChain chain, string[] args);

    protected async Task SendMessage(BotContext context, MessageChain chain, string message, bool mention = false, bool reply = false)
    {
        var messageBuilder = MessageBuilder.Group(chain.GroupUin!.Value);
        if (reply)
        {
            messageBuilder.Forward(chain);
        }
        if (mention)
        {
            messageBuilder.Mention(chain.FriendUin!);
        }
        await context.SendMessage(messageBuilder.Text($" {message}").Build());
    }
}