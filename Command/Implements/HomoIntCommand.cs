using Lagrange.Core;
using Lagrange.Core.Message;
using static YounBot.Utils.MessageUtils;
using static YounBot.Utils.HomoIntUtils;
namespace YounBot.Command.Implements;

public class HomoIntCommand
{
    [Command("homoint", "")]
    public async void HomoInt(BotContext context, MessageChain chain, long number)
    {
        await SendMessage(context, chain, getInt(number));
    }
    
}