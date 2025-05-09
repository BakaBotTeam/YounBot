using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace YounBot.Command.Implements;

public class HelpCommand
{
    [Command("help", "列出所有指令")]
    public async Task Help(BotContext context, MessageChain chain)
    {
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).MultiMsg(MessageBuilder.Friend(10000)
            .Text(CommandManager.Instance.Descriptions
                .Select(command =>
                    $"{CommandManager.GetCommandPrefix()}{command.Key} {CommandManager.Instance.GetCommandArgs(command.Key)}: {command.Value}")
                .Aggregate((current, next) => $"{current}\n{next}")).Build()).Build());
    }
    
}