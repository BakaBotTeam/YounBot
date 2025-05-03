using Lagrange.Core;
using Lagrange.Core.Message;
using static YounBot.Utils.MessageUtils;
namespace YounBot.Command.Implements;

public class HelpCommand
{
    [Command("help", "列出所有指令")]
    public async Task Help(BotContext context, MessageChain chain)
    {
        await SendMessage(context, chain, CommandManager.Instance.Descriptions.Select(command => $"{CommandManager.GetCommandPrefix()}{command.Key} {CommandManager.Instance.GetCommandArgs(command.Key)}: {command.Value}").Aggregate((current, next) => $"{current}\n{next}"));
    }
    
}