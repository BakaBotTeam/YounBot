using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using YounBot.Permissions;
using YounBot.Utils;

namespace YounBot.Command.Implements;
using static Permission;
using static FileUtils;
using static MessageUtils;
public class YounkooCommand
{
    [Command("ping", "检查机器人是否在线吧")]
    public async Task Ping(BotContext context, MessageChain chain)
    {
        await SendMessage(context, chain, "Pong!");
    }
    
    [Command("testdm", "测试私聊")]
    public async Task TestDm(BotContext context, MessageChain chain)
    {
        uint user = chain.FriendUin;
        BotFriend? friend = (await context.FetchFriends()).FindLast(f => f.Uin == user) ?? (await context.FetchFriends(true)).FindLast(f => f.Uin == user);
            
        if (friend == null)
        {
            await SendMessage(context, chain, "无好友");
            return;
        }
        
        await context.SendMessage(MessageBuilder.Friend(friend.Uin).Text("Hello! " + DateTimeOffset.Now.ToUnixTimeSeconds()).Build());
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
    
    [Command("mute", "把某人的嘴巴用胶布粘上")]
    public async Task Mute(BotContext context, MessageChain chain, BotGroupMember member, uint second = 600, uint group = 0, string reason = "No reason")
    {
        if (HasPermission(chain))
        {
            uint _group = (group != 0) ? group : chain.GroupUin!.Value;
            await context.MuteGroupMember(_group, member.Uin, second);
            await context.SendMessage(MessageBuilder.Group(_group)
                .Text("[滥权小助手] ").Mention(member.Uin)
                .Text($" 获得了来自 ").Mention(chain.FriendUin).Text(" 的禁言\n")
                .Text($"时长: {Math.Round(second / 60.0, 2)} 分钟\n")
                .Text($"理由: {reason}").Build());
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

    [Command("unmute", "把胶布从某人的嘴巴上撕下来")]
    public async Task UnMute(BotContext context, MessageChain chain, BotGroupMember member, uint group = 0)
    {
        if (HasPermission(chain))
        {
            uint _group = (group != 0) ? group : chain.GroupUin!.Value;
            await context.MuteGroupMember(_group, member.Uin, 0);
            await context.SendMessage(MessageBuilder.Group(_group)
                .Text("[滥权小助手] ").Mention(member.Uin)
                .Text($" 获得了来自 ").Mention(chain.FriendUin).Text(" 的解除禁言\n").Build());
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

    [Command("ban", "把某人从群聊封禁")]
    public async Task Ban(BotContext context, MessageChain chain, BotGroupMember member, uint group = 0, string reason = "No reason")
    {
        if (HasPermission(chain))
        {
            uint _group = (group != 0) ? group : chain.GroupUin!.Value;
            await context.KickGroupMember(_group, member.Uin, false, reason);
        }
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
}