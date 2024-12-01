using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using YounBot.Utils;
using static YounBot.Utils.MessageUtils;

namespace YounBot.Command.Implements;

public class AcgCommand
{
    private readonly CooldownUtils _cooldown = new(10000L);
    
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
}