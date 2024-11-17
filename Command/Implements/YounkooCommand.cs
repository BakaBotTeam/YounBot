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
    
    [Command("addAdmin", "添加一位机器人管理员")]
    public async Task AddAdmin(BotContext context, MessageChain chain, BotGroupMember member)
    {
        if (IsBotOwner(chain))
        {
            if (!YounBotApp.Config!.BotAdmins!.Contains(member.Uin))
                YounBotApp.Config!.BotAdmins!.Add(member.Uin);
            SaveConfig("younbot-config.json", YounBotApp.Config, true);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
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
                .Text("[滥权小助手] ").Mention(member.Uin)
                .Text(" 被取消管理员！ ").Build());
        }
    }
    
    [Command("mute", "把某人的嘴巴用胶布粘上")]
    public async Task Mute(BotContext context, MessageChain chain, BotGroupMember member, string duration, string reason)
    {
        if (HasPermission(chain))
        {
            var time = TimeUtils.ParseDuration(duration).Seconds;
            await context.MuteGroupMember(chain.GroupUin!.Value, member.Uin, (uint)time);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Text("[滥权小助手] ").Mention(member.Uin)
                .Text($" 获得了来自 ").Mention(chain.FriendUin).Text(" 的禁言\n")
                .Text($"时长: {Math.Round(time / 60.0 * 100.0) / 100.0} 分钟\n")
                .Text($"理由: {reason}").Build());
        }
    }

    [Command("unmute", "把胶布从某人的嘴巴上撕下来")]
    public async Task UnMute(BotContext context, MessageChain chain, BotGroupMember member)
    {
        if (HasPermission(chain))
        {
            await context.MuteGroupMember(chain.GroupUin!.Value, member.Uin, 0);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Text("[滥权小助手] ").Mention(member.Uin)
                .Text($" 获得了来自 ").Mention(chain.FriendUin).Text(" 的解除禁言\n").Build());
        }
    }
}