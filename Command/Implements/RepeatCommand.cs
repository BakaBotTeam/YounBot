using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace YounBot.Command.Implements;

public class RepeatCommand : Command
{
    public RepeatCommand() : base("repeat", "Just repeat what you've said") {}

    public override void Execute(BotContext context, MessageChain chain, string[] args)
    { 
        SendMessage(context, chain, args[0], mention: true);
    }
}