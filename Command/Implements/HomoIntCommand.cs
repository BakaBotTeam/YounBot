using Lagrange.Core;
using Lagrange.Core.Message;
using static YounBot.Utils.MessageUtils;
using static YounBot.Utils.HomoIntUtils;
namespace YounBot.Command.Implements;

public class HomoIntCommand
{
    [Command("homoint", "随处可见的Homo（恼")]
    public async void HomoInt(BotContext context, MessageChain chain, long number)
    {
        await SendMessage(context, chain, getInt(number));
    }
    
}