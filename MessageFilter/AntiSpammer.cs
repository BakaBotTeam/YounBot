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
    private static readonly Dictionary<long, List<string>> LastMessages = new();
    private static readonly Dictionary<long, List<uint>> LastMessageSeqs = new();
    private static readonly Dictionary<long, long> LastMuteTime = new();
    
    public static async Task OnGroupMessage(BotContext context, GroupMessageEvent @event)
    {
        try
        {
            var selfPermission = context.FetchMembers((uint)@event.Chain.GroupUin).Result
                .FindLast((member => member.Uin == context.BotUin)).Permission;
            var targetPermission = @event.Chain.GroupMemberInfo.Permission;
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
            
            var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            LastMessageTimes[userUin].Add(currentTime);
            LastMessages[userUin].Add(MessageUtils.GetPlainTextForCheck(@event.Chain));
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
                            .Text(" Flagged Spamming(A)")
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
                        messageSimilarities.Add(LevenshteinDistance.Compute(LastMessages[userUin][i], LastMessages[userUin][i - 1]));
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
                    if (eightyPrecentMessageDelay > 0.82)
                    {
                        await context.MuteGroupMember(@event.Chain.GroupUin!.Value, userUin, 60);
                        if (LastMuteTime.ContainsKey(userUin) && currentTime - LastMuteTime[userUin] > 10000)
                        {
                            LastMuteTime[userUin] = currentTime;
                            await context.SendMessage(MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                                .Text("[消息过滤器] ").Mention(userUin)
                                .Text(" Flagged Spamming(B)")
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