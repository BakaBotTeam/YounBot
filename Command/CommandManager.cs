using Lagrange.Core;
using Lagrange.Core.Message;
using YounBot.Command.Implements;

namespace YounBot.Command;

public class CommandManager
{
    private static CommandManager? _instance;
    
    public static CommandManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            
            _instance = new CommandManager();
            
            return _instance;
        }
    }
    
    public List<Command> Commands { get; } = new();

    public void InitializeCommands()
    {
        Commands.Add(new RepeatCommand());
        Commands.Add(new HelpCommand());
    }

    public async Task ExecuteCommand(BotContext context, MessageChain chain, string message)
    {
        if (message.Length > 1)
        {
            var args = message.Substring(1).Split(" ");
            var command = GetCommand(args[0]);
            if (command != null)
            {
                await command.Execute(context, chain, args.Skip(1).ToArray());
            }
        }
    }

    public Command? GetCommand(string prefix) => Commands.Find(command => command.Name.Equals(prefix));
    
}