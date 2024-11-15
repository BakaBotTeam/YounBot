using System.Globalization;
using System.Reflection;
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
    
    private readonly Dictionary<string, (object Instance, MethodInfo Method)> _commands = new();
    
    public void InitializeCommands()
    {
        RegisterCommand(new RepeatCommand());
    }
    
    
    private void RegisterCommand(object commandClassInstance)
    {
        var methods = commandClassInstance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<CommandAttribute>();
            if (attribute != null)
            {
                _commands[attribute.PrimaryName.ToLower()] = (commandClassInstance, method);
            }
        }
    }
    
    private object ParseString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input string cannot be null or whitespace.");

        if (bool.TryParse(input, out bool boolResult))
            return boolResult;

        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intResult))
            return intResult;

        if (long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longResult))
            return longResult;

        if (double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double doubleResult))
            return doubleResult;

        return input;
    }
    
    public Task ExecuteCommand(BotContext context, MessageChain chain, string input)
    {
        var args = input.Substring(1).Split(" ");
        var commandName = args[0].ToLower();
        if (_commands.TryGetValue(commandName, out var command))
        {
            var (instance, method) = command;
            string[] stringArray = args.Skip(1).ToArray();

            object[] objectArray = new object[] { context, chain }.ToArray();
            if (stringArray.Length == 0)
            {
                method.Invoke(instance, objectArray);
            }
            else
            {
                objectArray = new object[] { context, chain, ParseString(stringArray[0]) }.ToArray();
                method.Invoke(instance, objectArray);
            }
        }
        else
        {
            Console.WriteLine("Command not found.");
        }
        
        return Task.CompletedTask;
    }

    // public async Task ExecuteCommand(BotContext context, MessageChain chain, string message)
    // {
    //     if (message.Length > 1)
    //     {
    //         var args = message.Substring(1).Split(" ");
    //         var command = GetCommand(args[0]);
    //         if (command != null)
    //         {
    //             await command.Execute(context, chain, args.Skip(1).ToArray());
    //         }
    //     }
    // }

    // public Command? GetCommand(string prefix) => Commands.Find(command => command.Name.Equals(prefix));
    
}