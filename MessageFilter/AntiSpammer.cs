using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
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
    
    public static async Task OnGroupMessage(BotContext context, GroupMessageEvent @event)
    {
        try
        {
            var members = await context.FetchMembers(@event.Chain.GroupUin!.Value);
            var selfPermission = members.FindLast(member => member.Uin == context.BotUin)!.Permission;
            var targetPermission = @event.Chain.GroupMemberInfo!.Permission;
            if (selfPermission <= targetPermission)
            {
                return;
            }

            var userUin = @event.Chain.FriendUin!;
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

            var message = MessageUtils.GetPlainTextForCheck(@event.Chain);
            var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
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
                LastEmptyMessageSeqs[userUin].Add(@event.Chain.Sequence);
                LastEmptyMessageTimes[userUin].Add(currentTime);
                if (LastEmptyMessageTimes[userUin].Count > 3)
                {
                    var eightyPrecentEmptyMessageDelay = 0L;
                    var emptyMessageDelays = new List<long>();
                    for (var i = 1; i < LastEmptyMessageTimes[userUin].Count; i++)
                    {
                        emptyMessageDelays.Add(LastEmptyMessageTimes[userUin][i] - LastEmptyMessageTimes[userUin][i - 1]);
                    }
                    emptyMessageDelays.Sort();
                    for (var i = 0; i < emptyMessageDelays.Count; i++)
                    {
                        if (i < emptyMessageDelays.Count * 0.8)
                        {
                            eightyPrecentEmptyMessageDelay += emptyMessageDelays[i];
                        }
                    }
                    eightyPrecentEmptyMessageDelay /= (long)(emptyMessageDelays.Count * 0.8);
                    if (eightyPrecentEmptyMessageDelay < 500)
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
                        // recall all messages
                        for (var i = 0; i < LastEmptyMessageSeqs[userUin].Count; i++)
                        {
                            await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, LastEmptyMessageSeqs[userUin][i]);
                        }
                        // clear history
                        LastEmptyMessageSeqs[userUin].Clear();
                        LastEmptyMessageTimes[userUin].Clear();
                        return;
                    }
                }
                
                return;
            }
            
            LastMessageTimes[userUin].Add(currentTime);
            LastMessages[userUin].Add(message);
            LastMessageSeqs[userUin].Add(@event.Chain.Sequence);
            if (LastMessageTimes[userUin].Count > 1)
            {
                var eightyPrecentMessageDelay = 0L;
                var messageDelays = new List<long>();
                for (var i = 1; i < LastMessageTimes[userUin].Count; i++)
                {
                    messageDelays.Add(LastMessageTimes[userUin][i] - LastMessageTimes[userUin][i - 1]);
                }
                messageDelays.Sort();
                for (var i = 0; i < messageDelays.Count; i++)
                {
                    if (i < messageDelays.Count * 0.8)
                    {
                        eightyPrecentMessageDelay += messageDelays[i];
                    }
                }
                eightyPrecentMessageDelay /= (long)(messageDelays.Count * 0.8);

                if (eightyPrecentMessageDelay < 500)
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
                    // recall all messages
                    for (var i = 0; i < LastMessageSeqs[userUin].Count; i++)
                    {
                        await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, LastMessageSeqs[userUin][i]);
                    }
                    // clear history
                    LastMessageTimes[userUin].Clear();
                    LastMessages[userUin].Clear();
                    LastMessageSeqs[userUin].Clear();
                    return;
                }
                
                if (LastMessages[userUin].Count > 3)
                {
                    var eightyPrecentMessageSimilarity = 0.0;
                    var messageSimilarities = new List<double>();
                    for (var i = 1; i < LastMessages[userUin].Count; i++)
                    {
                        messageSimilarities.Add(LevenshteinDistance.FindSimilarity(LastMessages[userUin][i], LastMessages[userUin][i - 1]));
                    }
                    messageSimilarities.Sort();
                    for (var i = 0; i < messageSimilarities.Count; i++)
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
                        // recall all messages
                        for (var i = 0; i < LastMessageSeqs[userUin].Count; i++)
                        {
                            await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, LastMessageSeqs[userUin][i]);
                        }
                        // clear history
                        LastMessageTimes[userUin].Clear();
                        LastMessages[userUin].Clear();
                        LastMessageSeqs[userUin].Clear();
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}