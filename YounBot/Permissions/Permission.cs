using Lagrange.Core.Common.Entity;
using Lagrange.Core.Message;

namespace YounBot.Permissions;

public static class Permission
{
    
    public static bool HasPermission(BotGroupMember member)
    {
        return IsBotOwner(member) || IsBotAdmin(member);
    }
    
    public static bool HasPermission(MessageChain chain)
    {
        return IsBotOwner(chain) || IsBotAdmin(chain);
    }
    
    public static bool IsBotOwner(BotGroupMember member)
    {
        return member.Uin == YounBotApp.Config!.BotOwner;
    }
    
    public static bool IsBotOwner(MessageChain chain)
    {
        return chain.FriendUin == YounBotApp.Config!.BotOwner;
    }
    
    public static bool IsBotAdmin(BotGroupMember member)
    {
        return YounBotApp.Config!.BotAdmins!.Contains(member.Uin);
    }
    
    public static bool IsBotAdmin(MessageChain chain)
    {
        return YounBotApp.Config!.BotAdmins!.Contains(chain.FriendUin);
    }
}