using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using LiteDB;
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
        Assembly assem = Assembly.GetExecutingAssembly();
        Stream resourceStream = assem.GetManifestResourceStream("YounBot.Resources.checker.txt")!;
        StreamReader reader = new StreamReader(resourceStream);
        string[] lines = reader.ReadToEnd().Split("\n");
        regexes = new String[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            regexes[i] = lines[i].Replace("\n", "").Replace("\r", "");
        }
    }
    
    private static string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256 instance
        using SHA256 sha256Hash = SHA256.Create();
        
        // Compute the hash as a byte array
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

        // Convert the byte array to a string
        StringBuilder builder = new StringBuilder();
        foreach (byte b in bytes)
        { 
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
    
    public static async Task OnGroupMessage(BotContext context, GroupMessageEvent @event) 
    {
        // check permission
        List<BotGroupMember> members = await context.FetchMembers(@event.Chain.GroupUin!.Value);
        GroupMemberPermission selfPermission = members.FindLast(member => member.Uin == context.BotUin)!.Permission;
        GroupMemberPermission targetPermission = @event.Chain.GroupMemberInfo!.Permission;
        if (selfPermission <= targetPermission)
        {
            return;
        }
        // check message
        string text = MessageUtils.GetPlainTextForCheck(@event.Chain);
        bool matched = false;
        foreach (string pattern in regexes)
        {
            Regex regex = new Regex(pattern);
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
            string id = ComputeSha256Hash(text);

            // find db for id
            ILiteCollection<CheckResult>? collection = YounBotApp.Db!.GetCollection<CheckResult>("check_result");
            CheckResult? result = collection.FindOne(x => x.Id == id);

            // if not found in db
            // if (result == null)
            // {
            Console.WriteLine("Start new check");
            string invokeResult = await CloudFlareApiInvoker.InvokeAiTask(text);
            result = new CheckResult();
            result.Id = id;
            result.Result = invokeResult;
            // }
            // else
            // {
            //     if (result.Result.StartsWith("true"))
            //     {
            //         Console.WriteLine("Cache hit");
            //     }
            // }

            LoggingUtils.Logger.LogInformation(result.Result);
            string[] results = result.Result.Split("|");
            // format true|违规类型|判断理由 or false|无
            if (results[0] == "true")
            {
                MessageBuilder message = MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                    .Text("[消息过滤器] ").Mention(@event.Chain.FriendUin)
                    .Text($" Flagged FalseMessage({results[1]})");
                if (!results[1].Contains("敏感"))
                {
                    message.Text($" due to {results[2]}");
                }

                MessageResult messageResult = await context.SendMessage(message.Build());
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
            LoggingUtils.Logger.LogWarning(e.ToString());
        }
    }
}