using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using YounBot.Permissions;

namespace YounBot.Command.Implements;

public class QueryCommand
{
    [Command("addplace", "新增排卡队列")]
    public async Task AddPlace(BotContext context, MessageChain chain, string place, string shortName = "")
    {
        if (!Permission.HasPermission(chain.GroupMemberInfo!) &&
            chain.GroupMemberInfo!.Permission != GroupMemberPermission.Owner &&
            chain.GroupMemberInfo!.Permission != GroupMemberPermission.Admin)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text("你没有权限使用该指令").Build());
            return;
        }

        if (string.IsNullOrWhiteSpace(place))
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text("请输入有效的地点名称").Build());
            return;
        }

        if (string.IsNullOrWhiteSpace(shortName))
        {
            shortName = place[0].ToString(); // 默认简写为地点名称的首字
        }

        if (YounBotApp.QueryPlaceManager!.AddQueryPlace(place, shortName, chain.GroupUin!.Value))
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value)
                .Text($"成功新增排卡队列: {place} ({shortName})").Build());
        }
        else
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("排卡队列已存在").Build());
        }
    }

    [Command("removeplace", "删除排卡队列")]
    public async Task RemovePlace(BotContext context, MessageChain chain, string place)
    {
        if (!Permission.HasPermission(chain.GroupMemberInfo!) &&
            chain.GroupMemberInfo!.Permission != GroupMemberPermission.Owner &&
            chain.GroupMemberInfo!.Permission != GroupMemberPermission.Admin)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text("你没有权限使用该指令").Build());
            return;
        }

        if (string.IsNullOrWhiteSpace(place))
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text("请输入有效的地点名称").Build());
            return;
        }

        if (YounBotApp.QueryPlaceManager!.RemoveQueryPlace(place, chain.GroupUin!.Value))
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text($"成功删除排卡队列: {place}").Build());
        }
        else
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("排卡队列不存在").Build());
        }
    }
}