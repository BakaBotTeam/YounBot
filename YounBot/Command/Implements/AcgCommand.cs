using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.Logging;
using YounBot.Permissions;
using YounBot.Utils;
using static YounBot.Utils.MessageUtils;

namespace YounBot.Command.Implements;

public class AcgCommand
{
    private readonly CooldownUtils _cooldown = new(10000L);
    private readonly CooldownUtils _sdCooldown = new(120000L);
    
    [Command("acg", "二次元图片")]
    public async Task AcgPicture(BotContext context, MessageChain chain)
    {
        uint user = chain.FriendUin;
        if (!_cooldown.IsTimePassed(user))
        {
            if (_cooldown.ShouldSendCooldownNotice(user))
                await SendMessage(context, chain, $"你可以在 {_cooldown.GetLeftTime(user) / 1000} 秒后继续使用该指令");
            return;
        }

        _cooldown.Flag(user);
        
        Task preMessage = SendMessage(context, chain, "Please Wait...");
        JsonObject response = await HttpUtils.GetJsonObject("https://pixiv.yuki.sh/api/recommend?type=json&nocache=" + new Random().Next(0, 1000000));
        string title = response["data"]!["title"]!.ToString();
        string url = response["data"]!["urls"]!["original"]!.ToString();
        string tags = "";
        foreach (string tag in response["data"]!["tags"]!.AsArray())
            tags += tag + ", ";
        string author = response["data"]!["user"]!["name"]!.ToString();
        byte[] image = await HttpUtils.GetBytes(url);
        MessageBuilder builder = MessageBuilder.Group(chain.GroupUin!.Value)
            .Image(image).Text("\nTitle: " + title + "\nTags: " + tags + "\nAuthor: " + author + "\nUrl: https://pixiv.net/artworks/" + response["data"]!["id"]!);
        preMessage.Wait();
        await context.SendMessage(builder.Build());
    }
    
    [Command("sd", "stable-diffusion 生图器")]
    public async Task StableDiffusion(BotContext context, MessageChain chain, string promet, string negativePromet = "lowres, bad anatomy, bad hands, text, error, missing fingers, extra digit, fewer digits, cropped, worst quality, low quality, normal quality, jpeg artifacts, signature, watermark, username, blurry, bad feet,")
    {
        uint user = chain.FriendUin;
        if (!_sdCooldown.IsTimePassed(user) && !Permission.IsBotOwner(chain))
        {
            if (_sdCooldown.ShouldSendCooldownNotice(user))
                await SendMessage(context, chain, $"你可以在 {_sdCooldown.GetLeftTime(user) / 1000} 秒后继续使用该指令");
            return;
        }

        if (promet.Contains("NSFW"))
        {
            await SendMessage(context, chain, "你生啥图呢？？");
            return;
        }

        _sdCooldown.Flag(user);
        
        Task preMessage = SendMessage(context, chain, "Please Wait...");
        JsonObject data = new()
        {
            ["height"] = 1024,
            ["width"] = 1024,
            ["prompt"] = promet,
            ["negative_prompt"] = "NSFW," + negativePromet
        };
        byte[] response = await CloudFlareApiInvoker.InvokeCustomAiTask("@cf/stabilityai/stable-diffusion-xl-base-1.0", data);
        MessageBuilder builder = MessageBuilder.Group(chain.GroupUin!.Value).MultiMsg(new []
        {
            MessageBuilder.Friend(10000).Image(response).Time(DateTime.MaxValue).Build()
        });
        preMessage.Wait();
        await context.SendMessage(builder.Build());
    }
    
    [Command("ptag", "按照tag随机获取图片")]
    public async Task PictureByTag(BotContext context, MessageChain chain, string tag)
    {
        uint user = chain.FriendUin;
        if (!_cooldown.IsTimePassed(user))
        {
            if (_cooldown.ShouldSendCooldownNotice(user))
                await SendMessage(context, chain, $"你可以在 {_cooldown.GetLeftTime(user) / 1000} 秒后继续使用该指令");
            return;
        }

        _cooldown.Flag(user);
        
        Task preMessage = SendMessage(context, chain, "Please Wait...");
        Dictionary<string, string> headers = new()
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
            ["Referer"] = "https://www.vilipix.com/",
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8",
            ["Accept-Language"] = "zh-CN,zh;q=0.9,en;q=0.8",
            ["Cache-Control"] = "max-age=0",
            ["Connection"] = "keep-alive",
            ["Upgrade-Insecure-Requests"] = "1"
        };
        DateTime startTime = DateTime.Now;
        string response = await HttpUtils.GetString($"https://www.vilipix.com/tags/{tag}/illusts", headers: headers);
        DateTime endTime = DateTime.Now;
        int timeTotal = (int)(endTime - startTime).TotalMilliseconds;
        // get page count, get last <li class="number">20</li>
        startTime = DateTime.Now;
        MatchCollection pageCountMatches = Regex.Matches(response, @"<li class=""number"">(\d+)</li>");
        endTime = DateTime.Now;
        int regexTime = (int)(endTime - startTime).TotalMilliseconds;
        if (pageCountMatches.Count != 0)
        {
            int pageCount = int.Parse(pageCountMatches[pageCountMatches.Count - 1].Groups[1].Value);
            LoggingUtils.Logger.LogInformation("Found " + pageCount + " pages");
            // get random page
            int page = new Random().Next(1, Math.Min(pageCount + 1, 30));
            startTime = DateTime.Now;
            response = await HttpUtils.GetString($"https://www.vilipix.com/tags/{tag}/illusts?p={page}", headers: headers);
            endTime = DateTime.Now;
            timeTotal += (int)(endTime - startTime).TotalMilliseconds;
        }
        // get all image id, <a href="/illust/123496259"
        startTime = DateTime.Now;
        MatchCollection matches = Regex.Matches(response, @"<a href=""/illust/(\d+)""");
        if (matches.Count == 0)
        {
            await SendMessage(context, chain, "No image found");
            return;
        }
        string[] ids = new string[matches.Count];
        for (int i = 0; i < matches.Count; i++)
            ids[i] = matches[i].Groups[1].Value;
        LoggingUtils.Logger.LogInformation("Found " + ids.Length + " images");
        endTime = DateTime.Now;
        regexTime += (int)(endTime - startTime).TotalMilliseconds;
        // get random image, retry 3 times
        int imgDownloadTime = 0;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                // api: https://pixiv.yuki.sh/api/illust?id=
                startTime = DateTime.Now;
                JsonObject _response = await HttpUtils.GetJsonObject($"https://pixiv.yuki.sh/api/illust?id={ids[new Random().Next(0, ids.Length)]}");
                endTime = DateTime.Now;
                imgDownloadTime += (int)(endTime - startTime).TotalMilliseconds;
                string title = _response["data"]!["title"]!.ToString();
                string url = _response["data"]!["urls"]!["original"]!.ToString();
                string tags = "";
                foreach (string _tag in _response["data"]!["tags"]!.AsArray())
                    tags += _tag + ", ";
                string author = _response["data"]!["user"]!["name"]!.ToString();
                startTime = DateTime.Now;
                byte[] image = await HttpUtils.GetBytes(url);
                endTime = DateTime.Now;
                imgDownloadTime += (int)(endTime - startTime).TotalMilliseconds;
                MessageBuilder builder = MessageBuilder.Group(chain.GroupUin!.Value)
                    .Image(image).Text("\nTitle: " + title + "\nTags: " + tags + "\nAuthor: " + author + "\nUrl: https://pixiv.net/artworks/" + _response["data"]!["id"]! + "\n(req: " + timeTotal + "ms, img: " + imgDownloadTime + "ms, other: " + regexTime + "ms)");
                preMessage.Wait();
                await context.SendMessage(builder.Build());
                break;
            }
            catch
            {
                if (i == 2)
                {
                    throw;
                }
            }
        }
    }
}