﻿using System.Globalization;
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
    
    public void InitializeCommands()
    {
        RegisterCommand(new RepeatCommand());
        RegisterCommand(new HomoIntCommand());
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
    
    public Task ExecuteCommand(BotContext context, MessageChain chain, string input)
    {
        var args = input.Substring(1).Split(" ");
        var commandName = args[0].ToLower();
        if (_commands.TryGetValue(commandName, out var command))
        {
            var (instance, method) = command;
            string[] stringArray = args.Skip(1).ToArray();
            object[] objectArray = new object[] { context, chain }.ToArray();
            ParameterInfo[] argTypes = method.GetParameters().Skip(2).ToArray();
            objectArray = new object[] { context, chain }.ToArray();
            int index = 0;
            try
            {
                try
                {
                    foreach (var _argType in argTypes)
                    {
                        var argType = _argType.ParameterType;
                        if (argType == typeof(string))
                        {
                            var str = stringArray[index];
                            if (str.StartsWith("\"") && str.EndsWith("\""))
                            {
                                str = str.Substring(1, str.Length - 2);
                            }
                            else if (str.StartsWith("\""))
                            {
                                while (!str.EndsWith("\""))
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
                            string str = input;
                            if (str.StartsWith("@"))
                            {
                                str = str.Substring(1);
                            }

                            if (str == "") throw new ArgumentException("群成员解析: 无法格式化群成员");
                            string[] array = str.Split(".");
                            BotGroupMember member;
                            if (array.Length == 1)
                            {
                                if (chain.GroupUin == null) throw new ArgumentException("群成员解析: 无法格式化群成员");

                                if (array[0] == "$")
                                {
                                    var members = context.FetchMembers(chain.GroupUin!.Value).Result;
                                    objectArray = objectArray.Append(members[new Random().Next(members.Count)])
                                        .ToArray();
                                    break;
                                }
            
                                try
                                {
                                    int.Parse(array[0]);
                                } catch (FormatException)
                                {
                                    throw new ArgumentException("群成员解析: 无法格式化群成员");
                                }
                                member = context.FetchMembers(chain.GroupUin!.Value).Result
                                    .Find(groupMember => groupMember.Uin == long.Parse(array[0]));
                                if (member == null) throw new ArgumentException("群成员解析: 无法格式化群成员");

                                objectArray = objectArray.Append(member)
                                    .ToArray();
                                break;
                            } else if (array.Length >= 3) throw new ArgumentException("群成员解析: 给予了太多参数");
        
                            try
                            {
                                int.Parse(array[0]);
                            } catch (FormatException)
                            {
                                throw new ArgumentException("群成员解析: 无法格式化群成员");
                            }
        
                            if (array[1] == "$")
                            {
                                var members = context.FetchMembers(chain.GroupUin!.Value).Result;
                                objectArray = objectArray.Append(members[new Random().Next(members.Count)])
                                    .ToArray();
                                break;
                            }
            
                            try
                            {
                                int.Parse(array[1]);
                            } catch (FormatException)
                            {
                                throw new ArgumentException("群成员解析: 无法格式化群成员");
                            }
        
                            member = context.FetchMembers(uint.Parse(array[0])).Result
                                .Find((BotGroupMember member) => member.Uin == long.Parse(array[1]));
                            if (member == null) throw new ArgumentException("群成员解析: 无法格式化群成员");
                            objectArray = objectArray.Append(member)
                                .ToArray();
                            break;
                        }
                        else throw new ArgumentException($"无法解析的参数类型 {argType.Name}");

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
                catch (Exception)
                {
                    throw new ArgumentException($"位置 {index + 2} 的参数错误");
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