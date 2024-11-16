﻿using System.Reflection;
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
        RegisterCommand(new RepeatCommand());
        RegisterCommand(new HomoIntCommand());
        RegisterCommand(new AcgCommand());
        RegisterCommand(new HttpCatCommand());
        RegisterCommand(new YounkooCommand());
        RegisterCommand(new HelpCommand());
    }
    
    
    private void RegisterCommand(object commandClassInstance)
    {
        var methods = commandClassInstance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<CommandAttribute>();
            if (attribute != null)
            {
                Descriptions[attribute.PrimaryName.ToLower()] = attribute.Description;
                _commands[attribute.PrimaryName.ToLower()] = (commandClassInstance, method);
            }
        }
    }
    
    public Task ExecuteCommand(BotContext context, MessageChain chain, string input)
    {
        var args = input.Substring(1).Split(" ");
        var commandName = args[0].ToLower();
        if (_commands.TryGetValue(commandName, out var command))
        {
            var (instance, method) = command;
            var stringArray = args.Skip(1).ToArray();
            var objectArray = new object[] { context, chain }.ToArray();
            var argTypes = method.GetParameters().Skip(2).ToArray();
            var index = 0;
            try
            {
                try
                {
                    foreach (var type in argTypes)
                    {
                        var argType = type.ParameterType;
                        if (argType == typeof(string))
                        {
                            var str = stringArray[index];
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
                            var str = stringArray[index];
                            if (str.StartsWith('@'))
                            {
                                str = str.Substring(1);
                            }

                            if (str == "") 
                                throw new ArgumentException("群成员解析: 无法格式化群成员");
                            var array = str.Split(".");
                            BotGroupMember? member;
                            uint memberUin;
                            uint groupUin;
                            
                            if (array.Length == 1)
                            {
                                if (chain.GroupUin == null) 
                                    throw new ArgumentException("群成员解析: 无法格式化群成员");

                                if (array[0] == "$")
                                {
                                    var members = context.FetchMembers(chain.GroupUin!.Value).Result;
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
                                var members = context.FetchMembers(chain.GroupUin!.Value).Result;
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
                            throw new ArgumentException($"无法解析的参数类型 {argType.Name}");

                        index++;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ArgumentException("参数不足");
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException($"位置 {index + 2} 的参数错误: {e.Message}");
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"位置 {index + 2} 的参数错误: ${e.Message}");
                }
                
                method.Invoke(instance, objectArray);
            }
            catch (Exception e)
            {
                context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text($"指令运行错误: \n{e}").Build());
            }
        }
        
        return Task.CompletedTask;
    }
}