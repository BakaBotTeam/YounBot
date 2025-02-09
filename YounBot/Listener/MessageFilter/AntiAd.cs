using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using LiteDB;
using Microsoft.Extensions.Logging;
using PrivateBinSharp;
using YounBot.Utils;

namespace YounBot.Listener.MessageFilter;

public static class AntiAd
{
    private static String[] regexes { get; set; }
    private static Regex resultRegex = new("^([tf][ra][ul]s?e?)\\|([^|\\n]+)$");
    
    
    private class CheckResult
    {
        public string Id { get; set; }
        public string Result { get; set; }
        public DateTimeOffset Time { get; set; }
        public string ResultUrl { get; set; }
    }
    
    public static void Init()
    {
        Assembly assem = Assembly.GetExecutingAssembly();
        Stream resourceStream = assem.GetManifestResourceStream("YounBot.Resources.checker.txt")!;
        StreamReader reader = new(resourceStream);
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
        StringBuilder builder = new();
        foreach (byte b in bytes)
        { 
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
    
    public static async Task OnGroupMessage(BotContext context, GroupMessageEvent @event) 
    {
        if (!(await BotUtils.HasEnoughPermission(@event.Chain))) return;
        // check message
        string text = MessageUtils.GetPlainTextForCheck(@event.Chain);
        bool matched = false;
        foreach (string pattern in regexes)
        {
            Regex regex = new(pattern);
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
            if (result == null || result.Time.AddDays(5) < DateTimeOffset.Now)
            {
                Console.WriteLine("Start new check");
                string invokeResult = await CloudFlareApiInvoker.InvokeAiTask(text);
                result = new CheckResult();
                result.Id = id;
                result.Result = invokeResult;
                result.Time = DateTimeOffset.Now;
                result.ResultUrl = "上传至PrivateBin失败";
            }
            else
            {
                Console.WriteLine("Cache hit");
            }
            
            string url = result.ResultUrl;
            if (url == "上传至PrivateBin失败") {
                try
                {
                    string uploadContent = $"MessageContext: {text}\n\nModel Response: {result.Result}";
                    Paste paste = await YounBotApp.PrivateBinClient?.CreatePaste(uploadContent, "", "1year")!;
                    if (paste.IsSuccess)
                    {
                        url = paste.ViewURL;
                        result.ResultUrl = url;
                    }
                }
                catch (Exception e)
                {
                    LoggingUtils.Logger.LogWarning(e.ToString());
                }
            }

            string resultText = result.Result;
            if (resultText.Contains("</think>")) resultText = resultText.Substring(resultText.IndexOf("</think>") + 8).Replace("\n", "");
            string[] results = resultText.Split("|");
            if (results[0] == "true")
            {
                try
                {
                    await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, @event.Chain.Sequence);
                }
                catch (Exception)
                {
                }

                await context.MuteGroupMember(@event.Chain.GroupUin!.Value, @event.Chain.FriendUin, 600);
                
                MessageBuilder message = MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                    .Text("[消息过滤器] ").Mention(@event.Chain.FriendUin)
                    .Text($" Flagged FalseMessage({results[1]})\nDetails: {url}");

                MessageResult messageResult = await context.SendMessage(message.Build());
                
                // recall message after 5s
                await Task.Delay(5000);
                await context.RecallGroupMessage(@event.Chain.GroupUin!.Value, messageResult);
            } 
            else if (results[0] != "false")
            {
                MessageBuilder message = MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                    .Text("[消息过滤器] ").Mention(@event.Chain.FriendUin)
                    .Text($" 模型返回结果异常 请联系bot管理员 | Details: {url}");

                MessageResult messageResult = await context.SendMessage(message.Build());
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