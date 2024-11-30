using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Microsoft.Extensions.Logging;
using YounBot.Utils;

namespace YounBot.MessageFilter;

public class AntiSpammer
{
    private static readonly Dictionary<long, List<long>> LastMessageTimes = new();
    private static readonly Dictionary<long, List<long>> LastEmptyMessageTimes = new();
    private static readonly Dictionary<long, List<uint>> LastEmptyMessageSeqs = new();
    private static readonly Dictionary<long, List<string>> LastMessages = new();
    private static readonly Dictionary<long, List<uint>> LastMessageSeqs = new();
    private static readonly Dictionary<long, long> LastMuteTime = new();
    private static readonly long AllowDelay = 1000;
    private static readonly int MaxMessageStore = 16;
    private static readonly int MaxTextMessageStore = 16;
    
    public static async Task OnGroupMessage(BotContext context, GroupMessageEvent @event)
    {
        try
        {
            List<BotGroupMember> members = await context.FetchMembers(@event.Chain.GroupUin!.Value);
            GroupMemberPermission selfPermission = members.FindLast(member => member.Uin == context.BotUin)!.Permission;
            GroupMemberPermission targetPermission = @event.Chain.GroupMemberInfo!.Permission;
            if (selfPermission <= targetPermission)
            {
                return;
            }

            uint userUin = @event.Chain.FriendUin!;
            if (!LastMessages.ContainsKey(userUin))
            {
                LastMessages.Add(userUin, new List<string>());
            }
            if (!LastMessageTimes.ContainsKey(userUin))
            {
                LastMessageTimes.Add(userUin, new List<long>());
            }
            if (!LastMessageSeqs.ContainsKey(userUin))
            {
                LastMessageSeqs.Add(userUin, new List<uint>());
            }
            if (!LastMuteTime.ContainsKey(userUin))
            {
                LastMuteTime.Add(userUin, 0);
            }
            
            if (LastMessages[userUin].Count > MaxTextMessageStore)
            {
                LastMessages[userUin].RemoveAt(0);
            }
            if (LastMessageTimes[userUin].Count > MaxMessageStore)
            {
                LastMessageTimes[userUin].RemoveAt(0);
            }
            if (LastMessageSeqs[userUin].Count > MaxMessageStore)
            {
                LastMessageSeqs[userUin].RemoveAt(0);
            }

            string message = MessageUtils.GetPlainTextForCheck(@event.Chain);
            long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            if (message.Replace("\n", "").Replace(" ", "").Length < 1)
            {
                if (!LastEmptyMessageTimes.ContainsKey(userUin))
                {
                    LastEmptyMessageTimes.Add(userUin, new List<long>());
                }
                if (!LastEmptyMessageSeqs.ContainsKey(userUin))
                {
                    LastEmptyMessageSeqs.Add(userUin, new List<uint>());
                }
                // if stored 8 messages, remove the oldest one
                if (LastEmptyMessageTimes[userUin].Count > MaxMessageStore)
                {
                    LastEmptyMessageTimes[userUin].RemoveAt(0);
                    LastEmptyMessageSeqs[userUin].RemoveAt(0);
                }
                LastEmptyMessageSeqs[userUin].Add(@event.Chain.Sequence);
                LastEmptyMessageTimes[userUin].Add(currentTime);
                if (LastEmptyMessageTimes[userUin].Count > 3)
                {
                    long eightyPrecentEmptyMessageDelay = 0L;
                    List<long> emptyMessageDelays = new();
                    for (int i = 1; i < LastEmptyMessageTimes[userUin].Count; i++)
                    {
                        emptyMessageDelays.Add(LastEmptyMessageTimes[userUin][i] - LastEmptyMessageTimes[userUin][i - 1]);
                    }
                    emptyMessageDelays.Sort();
                    for (int i = 0; i < emptyMessageDelays.Count; i++)
                    {
                        if (i < emptyMessageDelays.Count * 0.8)
                        {
                            eightyPrecentEmptyMessageDelay += emptyMessageDelays[i];
                        }
                    }
                    eightyPrecentEmptyMessageDelay /= (long)(emptyMessageDelays.Count * 0.8);
                    if (eightyPrecentEmptyMessageDelay < AllowDelay)
                    {
                        await context.MuteGroupMember(@event.Chain.GroupUin!.Value, userUin, 60);
                        if (LastMuteTime.ContainsKey(userUin) && currentTime - LastMuteTime[userUin] > 10000)
                        {
                            LastMuteTime[userUin] = currentTime;
                            await context.SendMessage(MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                                .Text("[消息过滤器] ").Mention(userUin)
                                .Text($" Flagged Spamming(C) | delay: {eightyPrecentEmptyMessageDelay}")
                                .Build());
                        }
                        // recall all messages, copy list to avoid concurrent modification
                        List<uint> emptyMessageSeqs = LastEmptyMessageSeqs[userUin].ToList();
                        // clear history
                        LastEmptyMessageSeqs[userUin].Clear();
                        LastEmptyMessageTimes[userUin].Clear();
                        for (int i = 0; i < emptyMessageSeqs.Count; i++)
                        {
                            await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, emptyMessageSeqs[i]);
                            Thread.Sleep(500);
                        }
                        return;
                    }
                }
            }
            else
            {
                LastMessages[userUin].Add(message);
            }
            
            LastMessageTimes[userUin].Add(currentTime);
            LastMessageSeqs[userUin].Add(@event.Chain.Sequence);
            if (LastMessageTimes[userUin].Count > 1)
            {
                long eightyPrecentMessageDelay = 0L;
                List<long> messageDelays = new();
                for (int i = 1; i < LastMessageTimes[userUin].Count; i++)
                {
                    messageDelays.Add(LastMessageTimes[userUin][i] - LastMessageTimes[userUin][i - 1]);
                }
                messageDelays.Sort();
                for (int i = 0; i < messageDelays.Count; i++)
                {
                    if (i < messageDelays.Count * 0.8)
                    {
                        eightyPrecentMessageDelay += messageDelays[i];
                    }
                }
                eightyPrecentMessageDelay /= (long)(messageDelays.Count * 0.8);

                if (eightyPrecentMessageDelay < AllowDelay)
                {
                    await context.MuteGroupMember(@event.Chain.GroupUin!.Value, userUin, 60);
                    if (LastMuteTime.ContainsKey(userUin) && currentTime - LastMuteTime[userUin] > 10000)
                    {
                        LastMuteTime[userUin] = currentTime;
                        await context.SendMessage(MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                            .Text("[消息过滤器] ").Mention(userUin)
                            .Text($" Flagged Spamming(A) | delay: {eightyPrecentMessageDelay}")
                            .Build());
                    }
                    // recall all messages, copy list to avoid concurrent modification
                    List<uint> messageSeqs = LastMessageSeqs[userUin].ToList();
                    // clear history
                    LastMessageTimes[userUin].Clear();
                    LastMessages[userUin].Clear();
                    LastMessageSeqs[userUin].Clear();
                    for (int i = 0; i < messageSeqs.Count; i++)
                    {
                        await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, messageSeqs[i]);
                        Thread.Sleep(500);
                    }
                    return;
                }
                
                if (LastMessages[userUin].Count > 3)
                {
                    double eightyPrecentMessageSimilarity = 0.0;
                    List<double> messageSimilarities = new();
                    for (int i = 1; i < LastMessages[userUin].Count; i++)
                    {
                        // find most similar message from all messages
                        double maxSimilarity = 0.0;
                        foreach (string _msg in LastMessages[userUin])
                        {
                            maxSimilarity = Math.Max(maxSimilarity, LevenshteinDistance.FindSimilarity(LastMessages[userUin][i], _msg));
                        }
                    }
                    messageSimilarities.Sort();
                    for (int i = 0; i < messageSimilarities.Count; i++)
                    {
                        if (i < messageSimilarities.Count * 0.8)
                        {
                            eightyPrecentMessageSimilarity += messageSimilarities[i];
                        }
                    }
                    eightyPrecentMessageSimilarity /= messageSimilarities.Count * 0.8;
                    if (eightyPrecentMessageSimilarity > 0.76)
                    {
                        await context.MuteGroupMember(@event.Chain.GroupUin!.Value, userUin, 60);
                        if (LastMuteTime.ContainsKey(userUin) && currentTime - LastMuteTime[userUin] > 10000)
                        {
                            LastMuteTime[userUin] = currentTime;
                            await context.SendMessage(MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                                .Text("[消息过滤器] ").Mention(userUin)
                                .Text($" Flagged Spamming(B) | similarity: {eightyPrecentMessageSimilarity}")
                                .Build());
                        }
                        // recall all messages, copy list to avoid concurrent modification
                        List<uint> messageSeqs = LastMessageSeqs[userUin].ToList();
                        // clear history
                        LastMessageTimes[userUin].Clear();
                        LastMessages[userUin].Clear();
                        LastMessageSeqs[userUin].Clear();
                        for (int i = 0; i < messageSeqs.Count; i++)
                        {
                            await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, messageSeqs[i]);
                            Thread.Sleep(500);
                        }
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            LoggingUtils.Logger.LogWarning(e.ToString());
        }
    }
}