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
        DateTime startTime = DateTime.Now;
        JsonObject response = await HttpUtils.GetJsonObject("https://pixiv.yuki.sh/api/recommend?type=json&nocache=" + new Random().Next(0, 1000000));
        DateTime endTime = DateTime.Now;
        double timeTotal = (endTime - startTime).TotalMilliseconds;
        startTime = DateTime.Now;
        using ImageInfo info = await GetImageInfo(response["data"]!["id"]!.ToString());
        endTime = DateTime.Now;
        double imgDownloadTime = (endTime - startTime).TotalMilliseconds;
        MessageBuilder builder = MessageBuilder.Group(chain.GroupUin!.Value)
            .Image(info.Image).Text("\nTitle: " + info.Title + "\nTags: " + info.Tags + "\nAuthor: " + info.Author + "\nUrl: " + info.Url + "\n(req: " + Math.Round(timeTotal, 2) + "ms, img: " + Math.Round(imgDownloadTime, 2) + "ms, other: 0ms, offical: " + info.IsOfficialApi + ")");
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
        double timeTotal = (endTime - startTime).TotalMilliseconds;
        // get page count, get last <li class="number">20</li>
        startTime = DateTime.Now;
        MatchCollection pageCountMatches = Regex.Matches(response, @"<li class=""number"">(\d+)</li>");
        endTime = DateTime.Now;
        double regexTime = (endTime - startTime).TotalMilliseconds;
        if (pageCountMatches.Count != 0)
        {
            int pageCount = int.Parse(pageCountMatches[pageCountMatches.Count - 1].Groups[1].Value);
            LoggingUtils.Logger.LogInformation("Found " + pageCount + " pages");
            // get random page
            int page = new Random().Next(1, Math.Min(pageCount + 1, 30));
            startTime = DateTime.Now;
            response = await HttpUtils.GetString($"https://www.vilipix.com/tags/{tag}/illusts?p={page}", headers: headers);
            endTime = DateTime.Now;
            timeTotal += (endTime - startTime).TotalMilliseconds;
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
        regexTime += (endTime - startTime).TotalMilliseconds;
        // get random image, retry 3 times
        double imgDownloadTime = 0;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                startTime = DateTime.Now;
                using ImageInfo info = await GetImageInfo(ids[new Random().Next(0, ids.Length)]);
                endTime = DateTime.Now;
                imgDownloadTime += (endTime - startTime).TotalMilliseconds;
                MessageBuilder builder = MessageBuilder.Group(chain.GroupUin!.Value)
                    .Image(info.Image).Text("\nTitle: " + info.Title + "\nTags: " + info.Tags + "\nAuthor: " + info.Author + "\nUrl: " + info.Url + "\n(req: " + Math.Round(timeTotal, 2) + "ms, img: " + Math.Round(imgDownloadTime, 2) + "ms, other: " + Math.Round(regexTime, 2) + "ms, offical: " + info.IsOfficialApi + ")");
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

    private async Task<ImageInfo> GetImageInfoFromOfficalApi(string id)
    {
        string url = $"https://www.pixiv.net/ajax/illust/{id}?lang=zh";
        JsonObject response = await HttpUtils.GetJsonObject(url);
        if (response["error"]!.GetValue<bool>())
        {
            throw new Exception("获取图片信息失败");
        }
        ImageInfo imageInfo = new();
        imageInfo.Title = response["body"]!["title"]!.ToString();
        imageInfo.Url = "https://www.pixiv.net/artworks/" + id;
        imageInfo.Tags = "";
        foreach (JsonObject tag in response["body"]!["tags"]!["tags"]!.AsArray())
            imageInfo.Tags += tag["tag"].GetValue<string>() + ", ";
        imageInfo.Author = response["body"]!["userName"]!.ToString();
        imageInfo.Image = await HttpUtils.GetBytes(response["body"]!["urls"]!["regular"]!.ToString().Replace("i.pximg.net", "i.pixiv.cat"));
        imageInfo.IsOfficialApi = true;
        return imageInfo;
    }

    private async Task<ImageInfo> GetImageInfoFromUnofficalApi(string id)
    {
        string url = $"https://pixiv.yuki.sh/api/illust?id={id}";
        JsonObject response = await HttpUtils.GetJsonObject(url);
        ImageInfo imageInfo = new();
        imageInfo.Title = response["data"]!["title"]!.ToString();
        imageInfo.Url = "https://www.pixiv.net/artworks/" + id;
        imageInfo.Tags = "";
        foreach (string tag in response["data"]!["tags"]!.AsArray())
            imageInfo.Tags += tag + ", ";
        imageInfo.Author = response["data"]!["user"]!["name"]!.ToString();
        imageInfo.Image = await HttpUtils.GetBytes(response["data"]!["urls"]!["regular"]!.ToString());
        imageInfo.IsOfficialApi = false;
        return imageInfo;
    }

    private async Task<ImageInfo> GetImageInfo(string id)
    {
        Task<ImageInfo>[] tasks = [GetImageInfoFromOfficalApi(id), GetImageInfoFromUnofficalApi(id)];
        Task<ImageInfo> completedTask = await Task.WhenAny(tasks);
        if (completedTask.IsFaulted)
        {
            LoggingUtils.Logger.LogError(completedTask.Exception!.ToString());
            completedTask = tasks.First(t => t != completedTask);
            if (completedTask.IsFaulted)
                throw new Exception("获取图片信息失败", completedTask.Exception);
        }

        foreach (Task<ImageInfo> task in tasks)
        {
            if (task != completedTask)
            {
                task.ContinueWith(t =>
                {
                    if (!t.IsFaulted)
                    {
                        t.Result.Dispose();
                    }
                });
            }
        }

        return await completedTask;
    }
    
    private class ImageInfo : IDisposable
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Tags { get; set; }
        public string Author { get; set; }
        public byte[] Image { get; set; }
        public bool IsOfficialApi { get; set; }

        public void Dispose()
        {
            Image = null;
        }
    }
}