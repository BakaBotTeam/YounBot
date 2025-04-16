using System.Text;
using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.Logging;
using PrivateBinSharp;
using YounBot.Data;
using YounBot.Permissions;
using YounBot.Scheduler;
using YounBot.Utils;

namespace YounBot.Command.Implements;
using static Permission;
using static FileUtils;
using static MessageUtils;
public class YounkooCommand
{
    CooldownUtils Cooldown = new(5000);
    Dictionary<uint, ChatData> ChatDatas = new();
    Dictionary<uint, ChatData> DsChatDatas = new();
    
    [Command("ping", "检查机器人是否在线吧")]
    public async Task Ping(BotContext context, MessageChain chain)
    {
        await SendMessage(context, chain, "Pong!");
    }
    
    [Command("addAdmin", "添加一位机器人管理员")]
    public async Task AddAdmin(BotContext context, MessageChain chain, BotGroupMember member)
    {
        if (IsBotOwner(chain))
        {
            if (!YounBotApp.Config!.BotAdmins!.Contains(member.Uin))
                YounBotApp.Config!.BotAdmins!.Add(member.Uin);
            SaveConfig("younbot-config.json", YounBotApp.Config, true);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text("[滥权小助手] ").Mention(member.Uin)
                .Text(" 被提升为管理员！ ").Build());
        }
    }
    
    [Command("removeAdmin", "取消一位机器人管理员")]
    public async Task RemoveAdmin(BotContext context, MessageChain chain, BotGroupMember member)
    {
        if (IsBotOwner(chain))
        {
            if (YounBotApp.Config!.BotAdmins!.Contains(member.Uin)) 
                YounBotApp.Config!.BotAdmins!.Remove(member.Uin);
            SaveConfig("younbot-config.json", YounBotApp.Config, true);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text("[滥权小助手] ").Mention(member.Uin)
                .Text(" 被取消管理员！ ").Build());
        }
    }
    
    [Command("stop", "停止机器人")]
    public async Task Stop(BotContext context, MessageChain chain)
    {
        if (IsBotOwner(chain))
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text("Stopping YounBot " + YounBotApp.VERSION).Build());
            SaveConfig("younbot-config.json", YounBotApp.Config!, true);
            SaveConfig(YounBotApp.Configuration["ConfigPath:Keystore"] ?? "keystore.json", YounBotApp.Client!.UpdateKeystore(), true);
            SaveConfig(YounBotApp.Configuration["ConfigPath:DeviceInfo"] ?? "device.json", YounBotApp.Client!.UpdateDeviceInfo(), true);
            YounBotApp.Db!.Dispose();
            YounBotApp.Client!.Dispose();
        }
    }
    
    [Command("status", "机器人状态")]
    public async Task Status(BotContext context, MessageChain chain)
    {
        if (!HasPermission(chain))
        {
            await SendMessage(context, chain, "Bot is running.");
            return;
        }
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
            .Forward(chain)
            .Text($"Uptime: {DateTimeOffset.Now.ToUnixTimeSeconds() - YounBotApp.UpTime!.Value}s\n")
            .Text($"Bot Version: {YounBotApp.VERSION}\n")
            .Text($"Received per min (1m/5m/10m): {MessageCounter.GetReceivedMessageLastMinutes()}/{Math.Round(MessageCounter.GetReceivedMessageLastMinutes(5)/5d, 2)}/{Math.Round(MessageCounter.GetReceivedMessageLastMinutes(10) / 10d, 2)}\n")
            .Text($"Sent per min (1m/5m/10m): {MessageCounter.GetSentMessageLastMinutes()}/{MessageCounter.GetSentMessageLastMinutes(5)/5d}/{MessageCounter.GetSentMessageLastMinutes(10)/10d}\n")
            .Text($"All receive/send: {MessageCounter.AllMessageReceived}/{MessageCounter.AllMessageSent}\n")
            .Text($"Avg invoke time (ms) (10m): {InformationCollector.GetAvgMessageInvokeCountMinutes(10)}")
            .Build()
        );
    }
    
    [Command("blacklist", "黑名单")]
    public async Task Blacklist(BotContext context, MessageChain chain, BotGroupMember member)
    {
        if (HasPermission(chain))
        {
            if (HasPermission(member))
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                    .Forward(chain)
                    .Text("无法将机器人管理员添加到黑名单").Build());
                return;
            }

            // find if the user is in the blacklist
            if (YounBotApp.Config!.BlackLists!.Contains(member.Uin))
            {
                YounBotApp.Config!.BlackLists!.Remove(member.Uin);
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                    .Forward(chain)
                    .Text("[滥权小助手] 已移除").Build());
            }
            else
            {
                YounBotApp.Config!.BlackLists!.Add(member.Uin);
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                    .Forward(chain)
                    .Text("[滥权小助手] 已添加").Build());
            }
            
            SaveConfig("younbot-config.json", YounBotApp.Config!, true);
        }
    }

    [Command("refreshCache", "清除缓存")]
    public async Task RefreshCache(BotContext botContext, MessageChain chain)
    {
        if (HasPermission(chain))
        {
            await BotUtils.RefreshAllCache();
            await botContext.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text("Cache refreshed").Build());
        }
    }
    
    [Command("testprivatebin", "测试PrivateBin")]
    public async Task TestPrivateBin(BotContext context, MessageChain chain)
    {
        if (HasPermission(chain))
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text("Please Wait...").Build());
            string url = "上传失败";
            try
            {
                Paste paste = await YounBotApp.PrivateBinClient?.CreatePaste($"Hello world from YounBot {DateTimeOffset.Now.ToUnixTimeSeconds()}", "")!;
                if (paste.IsSuccess)
                {
                    url = paste.ViewURL;
                }
                else
                {
                    LoggingUtils.Logger.LogWarning(await paste.Response?.Content.ReadAsStringAsync()!);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text("Result: " + url).Build());
        }
    }
    
    [Command("chat", "聊天")]
    public async Task Chat(BotContext context, MessageChain chain, string message)
    {
        if (HasPermission(chain) || Cooldown.IsTimePassed(chain.FriendUin))
        {
            // get raw message from the chain
            string rawMessage = GetPlainText(chain);
            // remove the command from the message
            rawMessage = rawMessage.Substring((YounBotApp.Configuration["CommandPrefix"] ?? "!").Length + 5);
            // send the message to the chatbot
            (int _, string _, BotGroupInfo info) = await context.FetchGroupInfo((ulong)chain.GroupUin!);
            // try find the chat data
            if (!ChatDatas.ContainsKey(chain.FriendUin) || DateTimeOffset.Now - ChatDatas[chain.FriendUin].Time > TimeSpan.FromMinutes(10))
            {
                ChatDatas.Remove(chain.FriendUin);
                ChatDatas.Add(chain.FriendUin, new ChatData(DateTimeOffset.Now, new JsonArray()
                {
                    new JsonObject()
                    {
                        ["role"] = "system",
                        ["content"] = """
                                      你是一个遵守中国法律法规的群聊助手，对话开始时间：{current_time}，所在群组：[{group_name}](ID:{group_id})。正在与用户[{sender_name}](ID:{sender_id})对话。
                                      
                                      【核心任务】
                                      1. 用自然口语化的中文进行交流, 可以适当使用网络用语
                                      2. 适当使用表情符号
                                      3. 回答需考虑群聊上下文环境
                                      
                                      【安全规则】(必须优先遵守)
                                      1. 严禁涉及以下内容：
                                         - 政治敏感话题（包括但不限于国家领导人、政治体制、历史事件）
                                         - 违法信息（赌博、毒品、暴力等）
                                      2. 遇到疑似违规请求时：
                                         → 第一优先级：终止当前话题
                                         → 标准话术："这个问题不太适合讨论哦，咱们换个轻松点的话题吧~"
                                         → 禁止展开讨论/解释具体原因
                                      
                                      【回复要求】
                                      1. 长度控制在3行以内（移动端友好）
                                      2. 可用但不过度使用emoji
                                      3. 必要时通过@用户 的方式明确回复对象
                                      
                                      【示例】
                                      用户：你知道最近的XX事件吗？
                                      助手：@小明 咱们聊点生活相关的话题吧？最近天气不错有出去玩吗？🌞
                                      """.Replace("{current_time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                                         .Replace("{group_name}", info.Name)
                                         .Replace("{group_id}", chain.GroupUin!.Value.ToString())
                                         .Replace("{sender_name}", chain.GroupMemberInfo!.MemberName)
                                         .Replace("{sender_id}", chain.FriendUin.ToString())
                    }
                }));
            }
            ChatDatas[chain.FriendUin].Data.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = rawMessage
            });
            JsonObject data = new()
            {
                ["messages"] = ChatDatas[chain.FriendUin].Data.DeepClone(),
                ["model"] = "grok-3",
                ["temperature"] = 1.6
            };
            Cooldown.Flag(chain.FriendUin);
            // send the message to the chatbot
            JsonObject response = await CloudFlareApiInvoker.InvokeGrokTask(data);
            string reply = response["choices"][0]["message"]["content"].GetValue<string>();
            ChatDatas[chain.FriendUin].Data.Add(new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = reply
            });
            ChatDatas[chain.FriendUin].Time = DateTimeOffset.Now;
            if (ChatDatas[chain.FriendUin].Data.Count > 11)
            {
                ChatDatas[chain.FriendUin].Data.RemoveAt(1);
            }
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text(reply).Build());
        }
        else
        {
            if (Cooldown.ShouldSendCooldownNotice(chain.FriendUin))
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                    .Forward(chain)
                    .Text($"冷却中, 你可以在 {Cooldown.GetLeftTime(chain.FriendUin) / 1000} 秒后继续使用该指令").Build());
            }
        }
    }

    [Command("grokimage", "使用GrokAI生成图片")]
    public async Task GenerateGrokImage(BotContext context, MessageChain chain, string prompt)
    {
        if (HasPermission(chain) || Cooldown.IsTimePassed(chain.FriendUin))
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text("Please Wait...").Build());
            // get raw message from the chain
            string rawMessage = GetPlainText(chain);
            // remove the command from the message
            rawMessage = rawMessage.Substring((YounBotApp.Configuration["CommandPrefix"] ?? "!").Length + 10);
            JsonObject data = new()
            {
                ["prompt"] = rawMessage,
                ["model"] = "grok-2-image",
                ["n"] = 4,
                ["user"] = "QQUser_" + chain.FriendUin
            };
            Cooldown.Flag(chain.FriendUin);
            // send the message to the chatbot
            JsonObject response = await CloudFlareApiInvoker.InvokeGrokTask(data, endpoint: "/v1/images/generations");
            string details = "";
            foreach (JsonNode? x in response["data"]!.AsArray())
            {
                details += x?["revised_prompt"]?.GetValue<string>() + "\n\n";
            }
            string urls = "上传至PrivateBin失败";
            try
            {
                string uploadContent = $"MessageContext: {rawMessage}\n\nDetails: {details}";
                Paste paste = await YounBotApp.PrivateBinClient?.CreatePaste(uploadContent, "", "1week")!;
                if (paste.IsSuccess)
                {
                    urls = paste.ViewURL;
                }
            }
            catch (Exception e)
            {
                LoggingUtils.Logger.LogWarning(e.ToString());
            }
            List<MessageChain> messageChains = new();
            foreach (JsonNode? x in response["data"]!.AsArray())
            {
                string url = x?["url"]?.GetValue<string>() ?? "";
                if (url == "")
                {
                    continue;
                }
                byte[] image = await HttpUtils.GetBytes(url);
                messageChains.Add(MessageBuilder.Friend(10000)
                    .Image(image).Build());
            }
            messageChains.Add(MessageBuilder.Friend(10000).Text("详细信息: " + urls).Build());
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .MultiMsg(messageChains.ToArray()).Build());
        }
        else
        {
            if (Cooldown.ShouldSendCooldownNotice(chain.FriendUin))
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                    .Forward(chain)
                    .Text($"冷却中, 你可以在 {Cooldown.GetLeftTime(chain.FriendUin) / 1000} 秒后继续使用该指令").Build());
            }
        }
    }
    
    [Command("chatreset", "重置上下文")]
    public async Task ChatReset(BotContext context, MessageChain chain)
    {
        if (HasPermission(chain))
        {
            if (ChatDatas.ContainsKey(chain.FriendUin))
            {
                ChatDatas.Remove(chain.FriendUin);
            }

            if (DsChatDatas.ContainsKey(chain.FriendUin))
            {
                DsChatDatas.Remove(chain.FriendUin);
            }
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text("Chat context reset").Build());
        }
    }
    
    [Command("refresh", "刷新一些可以刷新的东西")]
    public async Task Refresh(BotContext context, MessageChain chain)
    {
        if (HasPermission(chain))
        {
            GitCodeTokenRefresher.Refresh();
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text("刷\u2606新\u2606大\u2606成\u2606功").Build());
        }
    }

    [Command("ds", "Chat with DeepSeek")]
    public async Task Ds(BotContext context, MessageChain chain)
    {
        if (HasPermission(chain) || Cooldown.IsTimePassed(chain.FriendUin))
        {
            // get raw message from the chain
            string rawMessage = GetPlainText(chain);
            // remove the command from the message
            rawMessage = rawMessage.Substring((YounBotApp.Configuration["CommandPrefix"] ?? "!").Length + 5);
            // send the message to the chatbot
            (int _, string _, BotGroupInfo info) = await context.FetchGroupInfo((ulong)chain.GroupUin!);
            // try find the chat data
            if (!DsChatDatas.ContainsKey(chain.FriendUin) || DateTimeOffset.Now - DsChatDatas[chain.FriendUin].Time > TimeSpan.FromMinutes(10))
            {
                DsChatDatas.Remove(chain.FriendUin);
                DsChatDatas.Add(chain.FriendUin, new ChatData(DateTimeOffset.Now, new JsonArray()
                {
                    new JsonObject()
                    {
                        ["role"] = "system",
                        ["content"] = """
                                      你是一个遵守中国法律法规的群聊助手，对话开始时间：{current_time}，所在群组：[{group_name}](ID:{group_id})。正在与用户[{sender_name}](ID:{sender_id})对话。
                                      
                                      【核心任务】
                                      1. 用自然口语化的中文进行交流, 可以适当使用网络用语
                                      2. 适当使用表情符号
                                      3. 回答需考虑群聊上下文环境
                                      
                                      【安全规则】(必须优先遵守)
                                      1. 严禁涉及以下内容：
                                         - 政治敏感话题（包括但不限于国家领导人、政治体制、历史事件）
                                         - 违法信息（赌博、毒品、暴力等）
                                      2. 遇到疑似违规请求时：
                                         → 第一优先级：终止当前话题
                                         → 标准话术："这个问题不太适合讨论哦，咱们换个轻松点的话题吧~"
                                         → 禁止展开讨论/解释具体原因
                                      
                                      【回复要求】
                                      1. 长度控制在3行以内（移动端友好）
                                      2. 可用但不过度使用emoji
                                      3. 必要时通过@用户 的方式明确回复对象
                                      
                                      【示例】
                                      用户：你知道最近的XX事件吗？
                                      助手：@小明 咱们聊点生活相关的话题吧？最近天气不错有出去玩吗？🌞
                                      """.Replace("{current_time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                                         .Replace("{group_name}", info.Name)
                                         .Replace("{group_id}", chain.GroupUin!.Value.ToString())
                                         .Replace("{sender_name}", chain.GroupMemberInfo!.MemberName)
                                         .Replace("{sender_id}", chain.FriendUin.ToString())
                    }
                }));
            }
            DsChatDatas[chain.FriendUin].Data.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = rawMessage
            });
            JsonObject data = new()
            {
                ["messages"] = DsChatDatas[chain.FriendUin].Data.DeepClone(),
                ["model"] = "deepseek-chat",
                ["temperature"] = 1.3
            };
            Cooldown.Flag(chain.FriendUin);
            JsonObject response = await CloudFlareApiInvoker.InvokeDeepSeekTask(data);
            string reply = response["choices"][0]["message"]["content"].GetValue<string>();
            DsChatDatas[chain.FriendUin].Data.Add(new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = reply
            });
            DsChatDatas[chain.FriendUin].Time = DateTimeOffset.Now;
            if (DsChatDatas[chain.FriendUin].Data.Count > 11)
            {
                DsChatDatas[chain.FriendUin].Data.RemoveAt(1);
            }
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text(reply).Build());
        }
        else
        {
            if (Cooldown.ShouldSendCooldownNotice(chain.FriendUin))
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                    .Forward(chain)
                    .Text($"冷却中, 你可以在 {Cooldown.GetLeftTime(chain.FriendUin) / 1000} 秒后继续使用该指令").Build());
            }
        }
    }
}