using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;

namespace YounBot.Utils;

public class BotUtils
{
    public static async Task<GroupMemberPermission> GetSelfPermissionInGroup(uint group)
    {
        List<BotGroupMember> members = await YounBotApp.Client!.FetchMembers(group);
        return members.FindLast(member => member.Uin == YounBotApp.Client!.BotUin)!.Permission;
    }
    
    public static async Task<bool> HasEnoughPermission(MessageChain chain)
    {
        GroupMemberPermission selfPermission = await GetSelfPermissionInGroup(chain.GroupUin!.Value);
        GroupMemberPermission targetPermission = chain.GroupMemberInfo!.Permission;
        if (targetPermission == GroupMemberPermission.Owner) return false;
        else if ((targetPermission == GroupMemberPermission.Admin || targetPermission == GroupMemberPermission.Member) && selfPermission == GroupMemberPermission.Owner) return true;
        else if (targetPermission == GroupMemberPermission.Member && selfPermission == GroupMemberPermission.Admin) return true;
        else return false;
    }

    public static async Task RefreshAllCache()
    {
        List<BotGroup> groups = await YounBotApp.Client!.FetchGroups(true);
        IEnumerable<Task> fetchMembersTasks = groups.Select(group => YounBotApp.Client!.FetchMembers(group.GroupUin, true));
        await Task.WhenAll(fetchMembersTasks);

        await YounBotApp.Client!.FetchFriends(true);
    }
}