using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Logging;
using YounBot.Utils;
using Permission = YounBot.Permissions.Permission;

namespace YounBot.Command.Implements;

public class TldrCommand
{
    private bool _isUsingTldr = false;
    private CooldownUtils _cooldownUtils = new(120000);
    
    [Command("tldr", "量子速读")]
    public async Task Tldr(BotContext context, MessageChain chain)
    {
        if (_isUsingTldr)
        {
            await MessageUtils.SendMessage(context, chain, "正在处理上一个请求，请稍后再试。");
            return;
        }
        if (!_cooldownUtils.IsTimePassed(chain.FriendUin) && !Permission.HasPermission(chain))
        {
            if (_cooldownUtils.ShouldSendCooldownNotice(chain.FriendUin))
            {
                await MessageUtils.SendMessage(context, chain, $"冷却中，请在 {_cooldownUtils.GetLeftTime(chain.FriendUin) / 1000} 秒后再试。");
            }
            return;
        }
        _isUsingTldr = true;
        try
        {
            await Task.WhenAll(chain.Select(async entity =>
            {
                if (entity is not ForwardEntity forwardEntity) return;
                MessageResult preMessage = await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("让我找找...").Build());
                MessageChain? forwardMessage = (await context.GetGroupMessage(chain.GroupUin.Value, forwardEntity.Sequence,
                    forwardEntity.Sequence + 1)).FirstOrDefault(_e => _e.Sequence == forwardEntity.Sequence);
                if (forwardMessage == null)
                {
                    await MessageUtils.SendMessage(context, chain, "Failed to get message.");
                    return;
                }
                forwardMessage.ForEach(async void (messageEntity) =>
                {
                    try
                    {
                        if (messageEntity is not MultiMsgEntity multiEntity) return;
                        JsonArray data = new()
                        {
                            new JsonObject()
                            {
                                ["role"] = "system",
                                ["content"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["type"] = "text",
                                        ["text"] = "你是一个量子速读机器人，你的任务是总结以下群聊聊天记录，但请不要解读图片内的文本（如果有），大致赅括图片是什么类型的即可（如漫画，猫，狗，女孩子等，可适当添加形容词），避免回复政治敏感，色情，广告内容，但不要在消息内加上类似“没有涉及政治敏感、色情或广告内容。”的语句。"
                                    }
                                }
                            }
                        };
                        foreach (MessageChain messageChain in multiEntity.Chains)
                        {
                            if (data.Count >= 250)
                            {
                                await MessageUtils.SendMessage(context, chain, "太多消息了... 可能总结会不完整哦");
                                break;
                            }
                            string plainText = $"{messageChain.GroupMemberInfo.MemberName}: {MessageUtils.GetPlainTextForCheck(messageChain)}";
                            MultiMsgEntity? innerMultiMsgEntity = chain.FirstOrDefault(entity => entity is MultiMsgEntity) as MultiMsgEntity;
                            if (innerMultiMsgEntity != null)
                            {
                                List<string> innerChain = MessageUtils.GetMultiMessageContent(innerMultiMsgEntity);
                                if (innerChain.Count > 0)
                                {
                                    plainText = innerChain.Select(inner => $"  {inner}").Aggregate(plainText, (current, se) => current + $"\n{se}");
                                }
                            }
                            JsonArray singleData = new()
                            {
                                new JsonObject
                                {
                                    ["type"] = "text",
                                    ["text"] = plainText
                                }
                            };
                            List<ImageEntity> images = new();
                            foreach (IMessageEntity entity in messageChain)
                            {
                                if (entity is ImageEntity imageEntity)
                                {
                                    images.Add(imageEntity);
                                }
                            }
                            if (images.Count > 0)
                            {
                                foreach (ImageEntity imageEntity in images)
                                {
                                    byte[] imageBytes = await HttpUtils.GetBytes(imageEntity.ImageUrl);
                                    if (imageBytes.Length > 10 * 1024 * 1024) continue;
                                    singleData.Add(new JsonObject
                                    {
                                        ["type"] = "image_url",
                                        ["image_url"] = new JsonObject
                                        {
                                            ["url"] = imageEntity.ImageUrl,
                                            ["detail"] = "high"
                                        }
                                    });
                                }
                            }
                            data.Add(new JsonObject
                            {
                                ["role"] = "user",
                                ["content"] = singleData
                            });
                        }

                        JsonObject jsonObject = new()
                        {
                            ["model"] = "grok-2-vision-latest",
                            ["messages"] = data
                        };
                        await context.RecallGroupMessage(chain.GroupUin.Value, preMessage);
                        preMessage = await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("让我思考一下...").Build());
                        _cooldownUtils.Flag(chain.FriendUin);
                        JsonObject response = await CloudFlareApiInvoker.InvokeGrokTask(jsonObject);
                        await context.RecallGroupMessage(chain.GroupUin.Value, preMessage);
                        await MessageUtils.SendMessage(context, chain, response["choices"][0]["message"]["content"].GetValue<string>());
                    }
                    catch (Exception e)
                    {
                        await MessageUtils.SendMessage(context, chain, "Failed to process message: " + e.Message);
                        LoggingUtils.Logger.LogWarning(e, "Failed to process message.");
                    }
                });
            }));
        }
        finally
        {
            _isUsingTldr = false;
        }
    }
    
    [Command("tldrlast", "总结一定时间内的消息")]
    public async Task TldrLast(BotContext context, MessageChain chain, string duration)
    {
        if (_isUsingTldr)
        {
            await MessageUtils.SendMessage(context, chain, "正在处理上一个请求，请稍后再试。");
            return;
        }
        if (!_cooldownUtils.IsTimePassed(chain.FriendUin) && !Permission.HasPermission(chain))
        {
            if (_cooldownUtils.ShouldSendCooldownNotice(chain.FriendUin))
            {
                await MessageUtils.SendMessage(context, chain, $"冷却中，请在 {_cooldownUtils.GetLeftTime(chain.FriendUin) / 1000} 秒后再试。");
            }
            return;
        }
        _isUsingTldr = true;
        try
        {
            if (!TryParseTimeSpan(duration, out TimeSpan timeSpan))
            {
                await MessageUtils.SendMessage(context, chain, "无效的时间格式。支持的格式例如：1d（1天）、3h（3小时）、30m（30分钟）等。");
                return;
            }
            DateTime endTime = DateTime.UtcNow;
            DateTime startTime = endTime - timeSpan;
            if (timeSpan > TimeSpan.FromDays(3))
            {
                await MessageUtils.SendMessage(context, chain, "时间跨度过大，请限制在3天内。");
                return;
            }
            MessageResult preMessage = await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("让我找找...").Build());
            List<MessageChain> allMessage = new();
            uint sequence = chain.Sequence - 1;
            DateTime minTime = DateTime.Parse("1970/1/1 8:00:00");
            int noMessageCount = 0;
            while (sequence > 0)
            {
                if (noMessageCount >= 3)
                {
                    LoggingUtils.Logger.LogInformation("Failed to get messages, stopping");
                    break;
                }
                LoggingUtils.Logger.LogInformation($"Fetching messages from sequence {sequence} to {Math.Max(sequence - 25, 0)}");
                List<MessageChain>? messageChains = await context.GetGroupMessage(chain.GroupUin.Value, Math.Max(sequence - 25, 0), sequence);
                if (messageChains == null || messageChains.Count == 0)
                {
                    sequence -= 20;
                    LoggingUtils.Logger.LogInformation($"Found no messages, moving to sequence {sequence}");
                    noMessageCount++;
                    continue;
                }
                messageChains = messageChains.Where(m => m.Time != minTime).ToList();
                MessageChain? firstMessage = messageChains.FirstOrDefault(m => m.Time < startTime);
                if (firstMessage != null)
                {
                    LoggingUtils.Logger.LogInformation($"Meets the condition, first message time: {firstMessage.Time}, cutting off");
                    allMessage.AddRange(messageChains.Where(m => m.Time >= firstMessage.Time && m.Time <= endTime));
                    LoggingUtils.Logger.LogInformation($"Found {allMessage.Count} messages");
                    break;
                }
                allMessage.AddRange(messageChains.Where(m => m.Time >= startTime && m.Time <= endTime));
                sequence = messageChains.Min(m => m.Sequence) - 1;
                LoggingUtils.Logger.LogInformation($"Found {messageChains.Count} messages, moving to sequence {sequence}");
                if (allMessage.Count > 5000)
                {
                    LoggingUtils.Logger.LogInformation("Found too many messages, cutting off");
                    break;
                }
            }
            // filter out invalid messages
            allMessage = allMessage.Where(m => m.GroupUin != null && m.GroupMemberInfo != null).ToList();
            allMessage.Sort((messageChain, chain1) => messageChain.Time < chain1.Time ? -1 : 1);
            if (allMessage.Count == 0)
            {
                await MessageUtils.SendMessage(context, chain, "没有找到符合条件的消息，可能是群权限限制机器人无法查看历史消息，或者本时间段没有任何消息。");
                return;
            }
            try
            {
                JsonArray data = new()
                {
                    new JsonObject()
                    {
                        ["role"] = "system",
                        ["content"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "text",
                                ["text"] = "你是一个量子速读机器人，你的任务是总结以下群聊聊天记录，但请不要解读图片内的文本（如果有），大致赅括图片是什么类型的即可（如漫画，猫，狗，女孩子等，可适当添加形容词），避免回复政治敏感，色情，广告内容，但不要在消息内加上类似“没有涉及政治敏感、色情或广告内容。”的语句。"
                            }
                        }
                    }
                };
                foreach (MessageChain messageChain in allMessage)
                {
                    if (data.Count >= 5000)
                    {
                        await MessageUtils.SendMessage(context, chain, "太多消息了... 可能总结会不完整哦");
                        break;
                    }
                    string plainText = $"[{messageChain.Time.ToString()}] {messageChain.GroupMemberInfo.MemberName}({messageChain.TargetUin}): {MessageUtils.GetPlainTextForCheck(messageChain)}";
                    MultiMsgEntity? innerMultiMsgEntity = chain.FirstOrDefault(entity => entity is MultiMsgEntity) as MultiMsgEntity;
                    if (innerMultiMsgEntity != null)
                    {
                        List<string> innerChain = MessageUtils.GetMultiMessageContent(innerMultiMsgEntity);
                        if (innerChain.Count > 0)
                        {
                            plainText = innerChain.Select(inner => $"  {inner}").Aggregate(plainText, (current, se) => current + $"\n{se}");
                        }
                    }
                    JsonArray singleData = new()
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = plainText
                        }
                    };
                    /*List<ImageEntity> images = new();
                    foreach (IMessageEntity entity in messageChain)
                    {
                        if (entity is ImageEntity imageEntity)
                        {
                            images.Add(imageEntity);
                        }
                    }
                    if (images.Count > 0)
                    {
                        foreach (ImageEntity imageEntity in images)
                        {
                            byte[] imageBytes = await HttpUtils.GetBytes(imageEntity.ImageUrl);
                            if (imageBytes.Length > 10 * 1024 * 1024) continue;
                            singleData.Add(new JsonObject
                            {
                                ["type"] = "image_url",
                                ["image_url"] = new JsonObject
                                {
                                    ["url"] = imageEntity.ImageUrl,
                                    ["detail"] = "high"
                                }
                            });
                        }
                    }*/
                    data.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = singleData
                    });
                }

                JsonObject jsonObject = new()
                {
                    ["model"] = "grok-2-vision-latest",
                    ["messages"] = data,
                    ["max_tokens"] = 16384
                };
                await context.RecallGroupMessage(chain.GroupUin.Value, preMessage);
                preMessage = await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("让我思考一下...").Build());
                _cooldownUtils.Flag(chain.FriendUin);
                JsonObject response = await CloudFlareApiInvoker.InvokeGrokTask(jsonObject);
                uint minSequence = allMessage.Min(m => m.Sequence);
                uint maxSequence = allMessage.Max(m => m.Sequence);
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).MultiMsg([
                    MessageBuilder.Friend(10000).Text($"共找到 {allMessage.Count} 条消息 (seq: {minSequence} -> {maxSequence})，以下是总结：").Time(DateTime.MaxValue)
                        .Build(),
                    MessageBuilder.Friend(10000).Text(response["choices"][0]["message"]["content"].GetValue<string>())
                        .Time(DateTime.MaxValue).Build()
                ]).Build());
                await context.RecallGroupMessage(chain.GroupUin.Value, preMessage);
            }
            catch (Exception e)
            {
                await MessageUtils.SendMessage(context, chain, "Failed to process message: " + e.Message);
                LoggingUtils.Logger.LogWarning(e, "Failed to process message.");
            }
        }
        finally
        {
            _isUsingTldr = false;
        }
    }
    
    private bool TryParseTimeSpan(string input, out TimeSpan result)
    {
        result = TimeSpan.Zero;
    
        if (string.IsNullOrWhiteSpace(input))
            return false;
    
        // 尝试标准TimeSpan解析
        if (TimeSpan.TryParse(input, out result))
            return true;
    
        // 简化格式解析
        input = input.Trim().ToLower();
    
        // 支持简单格式 如 1d, 3h, 30m, 45s
        if (input.EndsWith("d") && double.TryParse(input[..^1], out double days))
        {
            result = TimeSpan.FromDays(days);
            return true;
        }
        if (input.EndsWith("h") && double.TryParse(input[..^1], out double hours))
        {
            result = TimeSpan.FromHours(hours);
            return true;
        }
        if (input.EndsWith("m") && double.TryParse(input[..^1], out double minutes))
        {
            result = TimeSpan.FromMinutes(minutes);
            return true;
        }
        if (input.EndsWith("s") && double.TryParse(input[..^1], out double seconds))
        {
            result = TimeSpan.FromSeconds(seconds);
            return true;
        }
    
        return false;
    }
}