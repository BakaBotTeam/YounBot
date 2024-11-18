﻿using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Microsoft.Extensions.Logging;
using YounBot.Utils;

namespace YounBot.MessageFilter;

public static class AntiAd
{
    private static String[] regexes { get; set; }
    
    private class CheckResult
    {
        public string Id { get; set; }
        public string Result { get; set; }
    }
    
    public static void Init()
    {
        var assem = Assembly.GetExecutingAssembly();
        using var resourceStream = assem.GetManifestResourceStream("YounBot.Resources.checker.txt")!;
        var reader = new StreamReader(resourceStream);
        var lines = reader.ReadToEnd().Split("\n");
        regexes = new String[lines.Length];
        for (var i = 0; i < lines.Length; i++)
        {
            regexes[i] = lines[i].Replace("\n", "").Replace("\r", "");
        }
    }
    
    private static string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256 instance
        using var sha256Hash = SHA256.Create();
        
        // Compute the hash as a byte array
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

        // Convert the byte array to a string
        var builder = new StringBuilder();
        foreach (byte b in bytes)
        { 
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
    
    public static async Task OnGroupMessage(BotContext context, GroupMessageEvent @event) 
    {
        // check permission
        var members = await context.FetchMembers(@event.Chain.GroupUin!.Value);
        var selfPermission = members.FindLast(member => member.Uin == context.BotUin)!.Permission;
        var targetPermission = @event.Chain.GroupMemberInfo!.Permission;
        if (selfPermission <= targetPermission)
        {
            return;
        }
        // check message
        var text = MessageUtils.GetPlainTextForCheck(@event.Chain);
        var matched = false;
        foreach (var pattern in regexes)
        {
            var regex = new Regex(pattern);
            if (regex.IsMatch(text.Replace("\n", "").Replace(" ", "")))
            {
                matched = true;
            }
        }

        if (!matched)
        {
            return;
        };

        try
        {
            // create message sha256 as message id
            var id = ComputeSha256Hash(text);

            // find db for id
            var collection = YounBotApp.Db!.GetCollection<CheckResult>("check_result");
            var result = collection.FindOne(x => x.Id == id);

            // if not found in db
            // if (result == null)
            // {
            LoggingUtils.CreateLogger().LogInformation("Start new check");
            var msg = MessageCache.GetCheckMessage(@event.Chain.GroupUin ?? 0, @event.Chain.FriendUin);
            LoggingUtils.CreateLogger().LogInformation($"Check message: {msg}");
            var invokeResult = await CloudFlareApiInvoker.InvokeAiTask(msg);
            result = new CheckResult();
            result.Id = id;
            result.Result = invokeResult;
            // }
            // else
            // {
            //     if (result.Result.StartsWith("true"))
            //     {
            //         LoggingUtils.CreateLogger().LogInformation("Cache hit");
            //     }
            // }

            LoggingUtils.CreateLogger().LogInformation(result.Result);
            var results = result.Result.Split("|");
            // format true|违规类型|判断理由 or false|无
            if (results[0] == "true")
            {
                var message = MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                    .Text("[消息过滤器] ").Mention(@event.Chain.FriendUin)
                    .Text($" Flagged FalseMessage({results[1]})");
                if (!results[1].Contains("敏感"))
                {
                    message.Text($" due to {results[2]}");
                }

                var messageResult = await context.SendMessage(message.Build());
                try
                {
                    await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, @event.Chain.Sequence);
                }
                catch (Exception)
                {
                }

                await context.MuteGroupMember(@event.Chain.GroupUin!.Value, @event.Chain.FriendUin, 600);
                // recall message after 5s
                await Task.Delay(5000);
                await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, messageResult);
            }

            // store result at db
            collection.Upsert(result);
        }
        catch (Exception e)
        {
            LoggingUtils.CreateLogger().LogWarning(e.ToString());
        }
    }
}