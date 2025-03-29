using System.Reflection;
using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
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
    public readonly Dictionary<string, string> Descriptions = new();
    
    public void InitializeCommands()
    {
        RegisterCommand(new HomoIntCommand());
        RegisterCommand(new AcgCommand());
        RegisterCommand(new HttpCatCommand());
        RegisterCommand(new YounkooCommand());
        RegisterCommand(new HelpCommand());
        RegisterCommand(new HypixelCommand());
        RegisterCommand(new WynnCommand());
        RegisterCommand(new VvCommand());
        RegisterCommand(new TldrCommand());
        RegisterCommand(new MFaceCommand());
        RegisterCommand(new RAnimalCommand());
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
    
    public Task ExecuteCommand(BotContext context, MessageChain chain, string input)
    {
        string[] args = input.Split(" ");
        string commandName = args[0].ToLower();
        if (_commands.TryGetValue(commandName, out (object Instance, MethodInfo Method) command))
        {
            (object instance, MethodInfo method) = command;
            string[] stringArray = args.Skip(1).ToArray();
            object[] objectArray = new object[] { context, chain }.ToArray();
            ParameterInfo[] argTypes = method.GetParameters().Skip(2).ToArray();
            int index = 0;
            try
            {
                try
                {
                    foreach (ParameterInfo type in argTypes)
                    {
                        try
                        {
                            if (type.IsOptional && stringArray[index] == "-")
                            {
                                index++;
                                continue;
                            }
                            Type argType = type.ParameterType;
                            if (argType == typeof(string))
                            {
                                string str = stringArray[index];
                                if (str.StartsWith('\"') && str.EndsWith('\"'))
                                {
                                    str = str.Substring(1, str.Length - 2);
                                }
                                else if (str.StartsWith('\"'))
                                {
                                    while (!str.EndsWith('\"'))
                                    {
                                        index++;
                                        str += " " + stringArray[index];
                                    }

                                    str = str.Substring(1, str.Length - 2);
                                }

                                objectArray = objectArray.Append(str).ToArray();
                            }
                            else if (argType == typeof(int))
                            {
                                objectArray = objectArray.Append(int.Parse(stringArray[index])).ToArray();
                            }
                            else if (argType == typeof(uint))
                            {
                                objectArray = objectArray.Append(uint.Parse(stringArray[index])).ToArray();
                            }
                            else if (argType == typeof(long))
                            {
                                objectArray = objectArray.Append(long.Parse(stringArray[index])).ToArray();
                            }
                            else if (argType == typeof(bool))
                            {
                                objectArray = objectArray.Append(bool.Parse(stringArray[index])).ToArray();
                            }
                            else if (argType == typeof(double))
                            {
                                objectArray = objectArray.Append(double.Parse(stringArray[index])).ToArray();
                            }
                            else if (argType == typeof(float))
                            {
                                objectArray = objectArray.Append(float.Parse(stringArray[index])).ToArray();
                            }
                            else if (argType == typeof(BotGroupMember))
                            {
                                string str = stringArray[index];
                                if (str.StartsWith('@'))
                                {
                                    str = str.Substring(1);
                                }

                                if (str == "") 
                                    throw new ArgumentException("群成员解析: 无法格式化群成员");
                                string[] array = str.Split(".");
                                BotGroupMember? member;
                                uint memberUin;
                                uint groupUin;
                            
                                if (array.Length == 1)
                                {
                                    if (chain.GroupUin == null) 
                                        throw new ArgumentException("群成员解析: 无法格式化群成员");

                                    if (array[0] == "$")
                                    {
                                        List<BotGroupMember> members = context.FetchMembers(chain.GroupUin!.Value).Result;
                                        objectArray = objectArray.Append(members[new Random().Next(members.Count)])
                                            .ToArray();
                                        ++index;
                                        continue;
                                    }
            
                                    try
                                    {
                                        memberUin = uint.Parse(array[0]);
                                    } catch (FormatException)
                                    {
                                        throw new ArgumentException("群成员解析: 无法格式化群成员");
                                    }
                                
                                    member = context.FetchMembers(chain.GroupUin!.Value).Result
                                        .Find(groupMember => groupMember.Uin == memberUin);
                                    if (member == null) 
                                        throw new ArgumentException("群成员解析: 无法格式化群成员");

                                    objectArray = objectArray.Append(member)
                                        .ToArray();
                                    ++index;
                                    continue;
                                } else if (array.Length >= 3) 
                                    throw new ArgumentException("群成员解析: 给予了太多参数");
                            
                                try
                                {
                                    groupUin = uint.Parse(array[0]);
                                } catch (FormatException)
                                {
                                    throw new ArgumentException("群解析: 无法格式化群");
                                }
        
                                if (array[1] == "$")
                                {
                                    List<BotGroupMember> members = context.FetchMembers(chain.GroupUin!.Value).Result;
                                    objectArray = objectArray.Append(members[new Random().Next(members.Count)])
                                        .ToArray();
                                    ++index;
                                    continue;
                                }
            
                                try
                                {
                                    memberUin = uint.Parse(array[1]);
                                } catch (FormatException)
                                {
                                    throw new ArgumentException("群成员解析: 无法格式化群成员");
                                }
        
                                member = context.FetchMembers(groupUin).Result
                                    .Find(botMember => botMember.Uin == memberUin);
                                if (member == null) 
                                    throw new ArgumentException("群成员解析: 无法格式化群成员");
                                objectArray = objectArray.Append(member)
                                    .ToArray();
                                ++index;
                                continue;
                            }
                            else
                            {
                                try
                                {
                                    objectArray = objectArray.Append(argType.GetMethod("Parse")!
                                        .Invoke(null, new object[] {stringArray[index]})!).ToArray();
                                } catch (Exception)
                                {
                                    throw new ArgumentException($"无法解析参数 {argType.Name}");
                                }
                            }
                        }
                        catch (Exception _e)
                        {
                            if (type.IsOptional)
                            {
                                objectArray = objectArray.Append(type.DefaultValue).ToArray()!;
                            }
                            else
                            {
                                throw;
                            }
                        }

                        index++;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ArgumentException("参数不足");
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException($"位置 {index + 2} 的参数错误: {e.Message}", e);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"位置 {index + 2} 的参数错误: ${e.Message}", e);
                }
                
                Task.Run(async () => await (Task)(method.Invoke(instance, objectArray) ?? Task.CompletedTask)).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text($"指令运行错误: \n{e.Message}").Build());
                context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).MultiMsg(new []
                {
                    MessageBuilder.Friend(context.BotUin).Text("堆载信息: ").Time(DateTime.MaxValue).Build(),
                    MessageBuilder.Friend(context.BotUin).Text(e.ToString()).Time(DateTime.MaxValue).Build()
                }).Build());
            }
        }
        
        return Task.CompletedTask;
    }
}