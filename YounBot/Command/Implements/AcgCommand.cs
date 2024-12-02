﻿using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
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
        JsonObject response = await HttpUtils.GetJsonObject("https://pixiv.yuki.sh/api/recommend?type=json");
        String title = response["data"]!["title"]!.ToString();
        String url = response["data"]!["urls"]!["original"]!.ToString();
        String tags = "";
        foreach (String tag in response["data"]!["tags"]!.AsArray())
            tags += tag + ", ";
        String author = response["data"]!["user"]!["name"]!.ToString();
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
}