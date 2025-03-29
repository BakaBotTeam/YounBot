using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using YounBot.Utils;

namespace YounBot.Command.Implements;

public class RAnimalCommand
{
    [Command("rshiba", "随机柴犬")]
    public async Task Rshiba(BotContext context, MessageChain chain)
    {
        string url = "https://dog.ceo/api/breed/shiba/images/random?" + DateTimeOffset.Now.ToUnixTimeMilliseconds();
        JsonObject response = await HttpUtils.GetJsonObject(url);
        if (response["status"].GetValue<string>() != "success")
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("API好像出了点问题...").Build());
            return;
        }
        string imageUrl = response["message"].GetValue<string>();
        byte[] image = await HttpUtils.GetBytes(imageUrl);
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Image(image).Build());
    }
    
    [Command("rdog", "随机狗狗")]
    public async Task Rdog(BotContext context, MessageChain chain)
    {
        string url = "https://dog.ceo/api/breeds/image/random?" + DateTimeOffset.Now.ToUnixTimeMilliseconds();
        JsonObject response = await HttpUtils.GetJsonObject(url);
        if (response["status"].GetValue<string>() != "success")
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("API好像出了点问题...").Build());
            return;
        }
        string imageUrl = response["message"].GetValue<string>();
        byte[] image = await HttpUtils.GetBytes(imageUrl);
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Image(image).Build());
    }
    
    [Command("rcat", "随机猫猫")]
    public async Task Rcat(BotContext context, MessageChain chain)
    {
        string url = "https://cataas.com/cat?json=true&nocache=" + DateTimeOffset.Now.ToUnixTimeMilliseconds();
        JsonObject response = await HttpUtils.GetJsonObject(url);
        string imageUrl = response["url"].GetValue<string>();
        byte[] image = await HttpUtils.GetBytes(imageUrl);
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Image(image).Build());
    }
    
    [Command("rcatgif", "随机猫猫GIF")]
    public async Task RcatGif(BotContext context, MessageChain chain)
    {
        string url = "https://cataas.com/cat/gif?json=true&nocache=" + DateTimeOffset.Now.ToUnixTimeMilliseconds();
        JsonObject response = await HttpUtils.GetJsonObject(url);
        string imageUrl = response["url"].GetValue<string>();
        byte[] image = await HttpUtils.GetBytes(imageUrl);
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Image(image).Build());
    }
}