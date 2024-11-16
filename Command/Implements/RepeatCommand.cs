using Lagrange.Core;
using Lagrange.Core.Message;

namespace YounBot.Command.Implements;
using static Utils.MessageUtils;

public class RepeatCommand
{
    
    [Command("repeat", "Repeat what you've said")]
    public async void Repeat(BotContext context, MessageChain chain, string num)
    {
        await SendMessage(context, chain, num);
    }
}