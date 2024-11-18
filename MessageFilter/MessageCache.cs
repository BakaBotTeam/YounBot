using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using YounBot.Utils;

namespace YounBot.MessageFilter;

public static class MessageCache
{
    private static ConcurrentDictionary<uint, List<string>> MessageCacheDict = new();
    private static ConcurrentDictionary<uint, List<string>> GroupMessageCacheDict = new();
    
    public static void AddMessage(uint friendId, string message)
    {
        if (!MessageCacheDict.ContainsKey(friendId))
        {
            MessageCacheDict[friendId] = new List<string>();
        }
        MessageCacheDict[friendId].Add(message);
        if (MessageCacheDict[friendId].Count > YounBotApp.Config.MaxMessageCache)
        {
            MessageCacheDict[friendId].RemoveAt(0);
        }
    }
    
    public static void AddGroupMessage(uint groupId, string message)
    {
        if (!GroupMessageCacheDict.ContainsKey(groupId))
        {
            GroupMessageCacheDict[groupId] = new List<string>();
        }
        GroupMessageCacheDict[groupId].Add(message);
        if (GroupMessageCacheDict[groupId].Count > 25)
        {
            GroupMessageCacheDict[groupId].RemoveAt(0);
        }
    }
    
    public static string GetGroupMessage(uint groupId)
    {
        LoggingUtils.CreateLogger().LogInformation(GroupMessageCacheDict[groupId].Count.ToString());
        if (!GroupMessageCacheDict.ContainsKey(groupId))
        {
            return "";
        }
        return string.Join("\n", GroupMessageCacheDict[groupId]);
    }
    
    public static string GetCheckMessage(uint groupId, uint friendId)
    {
        return $"你要检查的是用户ID为{friendId}的消息\n" + GetGroupMessage(groupId);
    }
}