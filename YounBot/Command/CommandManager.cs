using System.Reflection;
using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.Logging;
using YounBot.Command.Implements;
using YounBot.Utils;

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
    public readonly Dictionary<string, string> Descriptions = new();
    
    public void InitializeCommands()
    {
        RegisterCommand(new HomoIntCommand());
        RegisterCommand(new AcgCommand());
        RegisterCommand(new HttpCatCommand());
        RegisterCommand(new YounkooCommand());
        RegisterCommand(new HelpCommand());
        RegisterCommand(new WynnCommand());
        RegisterCommand(new VvCommand());
        RegisterCommand(new MFaceCommand());
        RegisterCommand(new RAnimalCommand());
        RegisterCommand(new BrowserCommand());
        RegisterCommand(new QueryCommand());
        RegisterCommand(new SAuthCommand());
    }

    public static string GetCommandPrefix()
    {
        return YounBotApp.Configuration["CommandPrefix"] ?? "/";
    }

    public string GetCommandArgs(string commandName)
    {
        if (!_commands.TryGetValue(commandName, out (object Instance, MethodInfo Method) command)) return "";
        (object instance, MethodInfo method) = command;
        IEnumerable<ParameterInfo> args = method.GetParameters().Skip(2);
        return string.Join(" ", args.Select(arg =>
            arg.IsOptional ? $"[{arg.Name}]" : $"<{arg.Name}>"
        ));
    }
    
    
    private void RegisterCommand(object commandClassInstance)
    {
        MethodInfo[] methods = commandClassInstance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (MethodInfo method in methods)
        {
            CommandAttribute? attribute = method.GetCustomAttribute<CommandAttribute>();
            if (attribute == null) continue;
            
            Descriptions[attribute.PrimaryName.ToLower()] = attribute.Description;
            _commands[attribute.PrimaryName.ToLower()] = (commandClassInstance, method);
            
        }
    }
    
    public async Task ExecuteCommand(BotContext context, MessageChain chain, string input)
    {
        string[] args = input.Split(" ");
        string commandName = args[0].ToLower();
        if (!_commands.TryGetValue(commandName, out (object Instance, MethodInfo Method) command)) return;

        (object instance, MethodInfo method) = command;
        string[] stringArray = args.Skip(1).ToArray();
        object[] objectArray = new object[] { context, chain };
        ParameterInfo[] argTypes = method.GetParameters().Skip(2).ToArray();
        int index = 0;

        try
        {
            foreach (ParameterInfo type in argTypes)
            {
                object? parsedArg;
                if (type.IsOptional && (index >= stringArray.Length || stringArray[index] == "-"))
                {
                    parsedArg = type.DefaultValue;
                    index++;
                }
                else
                {
                    if (index >= stringArray.Length) throw new ArgumentException("参数不足");
                    string argStr = stringArray[index];

                    try
                    {
                        parsedArg = ConvertArgument(context, chain, type.ParameterType, argStr);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException($"位置 {index + 2} 的参数错误: {e.Message}", e);
                    }

                    index++;
                }
                objectArray = objectArray.Append(parsedArg).ToArray();
            }

            await (Task)(method.Invoke(instance, objectArray) ?? Task.CompletedTask);
        }
        catch (Exception e)
        {
            context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text($"指令运行错误: \n{e.Message}").Build());
            context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).MultiMsg(new[]
            {
                MessageBuilder.Friend(context.BotUin).Text("堆载信息: ").Time(DateTime.MaxValue).Build(),
                MessageBuilder.Friend(context.BotUin).Text(e.ToString()).Time(DateTime.MaxValue).Build()
            }).Build());
            LoggingUtils.Logger.LogError($"指令运行错误: \n{e.Message}\n{e.StackTrace}");
        }
    }

    private object ConvertArgument(BotContext context, MessageChain chain, Type argType, string argStr)
    {
        if (argType == typeof(string))
        {
            if (argStr.StartsWith('\"') && argStr.EndsWith('\"'))
            {
                return argStr.Substring(1, argStr.Length - 2);
            }

            return argStr;
        }

        if (argType == typeof(BotGroupMember))
        {
            return ParseBotGroupMember(context, chain, argStr);
        }

        try
        {
            return Convert.ChangeType(argStr, argType);
        }
        catch (FormatException)
        {
            try
            {
                // 尝试使用Parse方法进行转换
                return argType.GetMethod("Parse", new[] { typeof(string) })!.Invoke(null, new object[] { argStr })!;
            }
            catch (Exception)
            {
                throw new ArgumentException($"无法解析参数 {argType.Name}");
            }
        }
    }

    private BotGroupMember ParseBotGroupMember(BotContext context, MessageChain chain, string argStr)
    {
        if (argStr.StartsWith('@'))
        {
            argStr = argStr.Substring(1);
        }

        if (string.IsNullOrEmpty(argStr))
        {
            throw new ArgumentException("群成员解析: 无法格式化群成员");
        }

        string[] array = argStr.Split(".");
        uint memberUin;
        uint groupUin;

        if (array.Length == 1)
        {
            if (chain.GroupUin == null)
            {
                throw new ArgumentException("群成员解析: 无法格式化群成员");
            }

            if (array[0] == "$")
            {
                List<BotGroupMember> members = context.FetchMembers(chain.GroupUin!.Value).Result;
                return members[new Random().Next(members.Count)];
            }

            try
            {
                memberUin = uint.Parse(array[0]);
            }
            catch (FormatException)
            {
                throw new ArgumentException("群成员解析: 无法格式化群成员");
            }

            BotGroupMember? member = context.FetchMembers(chain.GroupUin!.Value).Result
                .Find(groupMember => groupMember.Uin == memberUin);
            if (member == null)
            {
                throw new ArgumentException("群成员解析: 无法格式化群成员");
            }

            return member;
        }

        if (array.Length >= 3)
        {
            throw new ArgumentException("群成员解析: 给予了太多参数");
        }

        try
        {
            groupUin = uint.Parse(array[0]);
        }
        catch (FormatException)
        {
            throw new ArgumentException("群解析: 无法格式化群");
        }

        if (array[1] == "$")
        {
            List<BotGroupMember> members = context.FetchMembers(chain.GroupUin!.Value).Result;
            return members[new Random().Next(members.Count)];
        }

        try
        {
            memberUin = uint.Parse(array[1]);
        }
        catch (FormatException)
        {
            throw new ArgumentException("群成员解析: 无法格式化群成员");
        }

        BotGroupMember? memberz = context.FetchMembers(groupUin).Result
            .Find(botMember => botMember.Uin == memberUin);
        if (memberz == null)
        {
            throw new ArgumentException("群成员解析: 无法格式化群成员");
        }

        return memberz;
    }
}
