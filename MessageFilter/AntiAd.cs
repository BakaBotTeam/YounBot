using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using YounBot.Utils;

namespace YounBot.MessageFilter;

public static class AntiAd
{
    private static String[] regexes { get; set; }
    
    public class CheckResult
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
    
    public static string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256 instance
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // Compute the hash as a byte array
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // Convert the byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
    
    public static async Task OnGroupMessage(BotContext context, GroupMessageEvent @event) 
    {
        // check permission
        var selfPermission = context.FetchMembers((uint)@event.Chain.GroupUin).Result
            .FindLast((member => member.Uin == context.BotUin)).Permission;
        var targetPermission = @event.Chain.GroupMemberInfo.Permission;
        if (selfPermission <= targetPermission)
        {
            return;
        }
        // check message
        var text = MessageUtils.GetPlainTextForCheck(@event.Chain);
        var matcheds = false;
        foreach (var pattern in regexes)
        {
            var regex = new Regex(pattern);
            if (regex.IsMatch(text.Replace("\n", "").Replace(" ", "")))
            {
                matcheds = true;
            }
        }

        if (!matcheds)
        {
            return;
        };

        try
        {
            // create message sha256 as message id
            var id = ComputeSha256Hash(text);

            // find db for id
            var collection = YounBotApp.DB!.GetCollection<CheckResult>("check_result");
            var result = collection.FindOne(x => x.Id == id);

            // if not found in db
            if (result == null)
            {
                Console.WriteLine("Start new check");
                var invokeResult = await CloudFlareApiInvoker.InvokeAiTask(text);
                result = new CheckResult();
                result.Id = id;
                result.Result = invokeResult;
            }
            else
            {
                if (result.Result.StartsWith("true"))
                {
                    Console.WriteLine("Cache hit");
                }
            }

            Console.WriteLine(result.Result);
            var results = result.Result.Split("|");
            // format true|违规类型|判断理由 or false|无
            if (results[0] == "true")
            {
                var message = MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                    .Text("[违规检测] ").Mention(@event.Chain.FriendUin)
                    .Text($" 消息被检测到违规\n")
                    .Text($"违规类型: {results[1]}\n");
                if (!results[1].Contains("敏感"))
                {
                    message.Text($"判断理由: {results[2]}\n");
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
            Console.WriteLine(e);
        }
    }
}