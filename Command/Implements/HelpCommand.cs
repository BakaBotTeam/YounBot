using Lagrange.Core;
using Lagrange.Core.Message;

namespace YounBot.Command.Implements;

public class HelpCommand : Command
{
    public HelpCommand() : base("help", "List all the commands") {}

    public override async Task Execute(BotContext context, MessageChain chain, string[] args)
    {
        await SendMessage(context, chain, 
            $"Available commands:\n{CommandManager.Instance.Commands.Select(command => $"/{command.Name}: {command.Description}")
            .Aggregate((current, next) => current + "\n" + next)}");
    }
}