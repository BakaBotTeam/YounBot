using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Logging;
using YounBot.Utils;

namespace YounBot.Command.Implements;

public class TldrCommand
{
    [Command("tldr", "量子速读")]
    public async Task Tldr(BotContext context, MessageChain chain)
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
                        JsonArray singleData = new JsonArray
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
                                string imageBase64 = Convert.ToBase64String(imageBytes);
                                singleData.Add(new JsonObject
                                {
                                    ["type"] = "image_url",
                                    ["image_url"] = new JsonObject
                                    {
                                        ["url"] = "data:image/jpeg;base64," + imageBase64,
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
}