using System.Diagnostics;
using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.Logging;
using PrivateBinSharp;
using YounBot.Data;
using YounBot.Permissions;
using YounBot.Scheduler;
using YounBot.Signer;
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

    [Command("findperson", "盒！")]
    public async Task FindPerson(BotContext context, MessageChain chain, uint uin)
    {
        if (!HasPermission(chain)) return;
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
            .Forward(chain)
            .Text("请稍等...").Build());
        List<string> sameGroup = [];

        foreach (BotGroup group in await context.FetchGroups())
        {
            BotGroupMember? member = (await context.FetchMembers(group.GroupUin)).Find(member => member.Uin == uin);
            if (member == null) continue;
            string role = member.Permission switch
            {
                GroupMemberPermission.Admin => "管理员",
                GroupMemberPermission.Owner => "群主",
                _ => "成员"
            };
            sameGroup.Add($"{group.GroupName}[{group.GroupUin}] -> {role} | 上次发言时间: {member.LastMsgTime}");
        }
        
        if (sameGroup.Count == 0)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text($"没有找到 {uin} 的说...").Build());
            return;
        }
        string groups = sameGroup.Aggregate("", (current, group) => current + (group + "\n"));
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
            .MultiMsg([MessageBuilder.Friend(10000).Text($"找到 {uin} 了:\n{groups}").Build()]).Build());
    }
    
    [Command("findpersonbyname", "盒！")]
    public async Task FindPerson(BotContext context, MessageChain chain, string name)
    {
        if (!HasPermission(chain)) return;
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
            .Forward(chain)
            .Text("请稍等...").Build());
        List<string> sameGroup = [];

        foreach (BotGroup group in await context.FetchGroups())
        {
            List<BotGroupMember> members = (await context.FetchMembers(group.GroupUin)).FindAll(member => member.MemberName != null && member.MemberName.Contains(name, StringComparison.OrdinalIgnoreCase));
            foreach (BotGroupMember member in members)
            {
                string role = member.Permission switch
                {
                    GroupMemberPermission.Admin => "管理员",
                    GroupMemberPermission.Owner => "群主",
                    _ => "成员"
                };
                sameGroup.Add($"{group.GroupName}[{group.GroupUin}] -> ({role}){member.MemberName}({member.Uin}) | 上次发言时间: {member.LastMsgTime}");
            }
        }
        
        if (sameGroup.Count == 0)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text($"没有找到 {name} 的说...").Build());
            return;
        }
        string groups = sameGroup.Aggregate("", (current, group) => current + (group + "\n"));
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
            .MultiMsg([MessageBuilder.Friend(10000).Text($"找到 {name} 了:\n{groups}").Build()]).Build());
    }
    
    [Command("qsign", "测测你的签")]
    public async Task QSign(BotContext context, MessageChain chain)
    {
        Random rnd = new();
        string signUrl = YounBotApp.Configuration["SignServerUrl"] ?? "https://sign.lagrangecore.org/api/sign/25765";
        string cmd = "wtlogin.login";
        uint seq = (uint)rnd.Next(12000, 36000);
        byte[] src = new byte[] { 1, 1 };
        OneBotSigner? signer = (OneBotSigner?)context.Config.CustomSignProvider;
        if (signer != null)
        {
            DateTime startTime = DateTime.Now;
            byte[]? result = signer.Sign(cmd, seq, src, out byte[]? _, out string? _);
            DateTime endTime = DateTime.Now;
            Debug.Assert(signer._info != null, "signer._info != null");
            Debug.Assert(result != null, nameof(result) + " != null");
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value)
                .Forward(chain)
                .Text($"Its working! {signer._info.CurrentVersion}({signer._info.AppId}) ({Math.Round((endTime - startTime).TotalMilliseconds, 2)}ms)\nsign: {Convert.ToHexString(result)}").Build());
        }
    }
}