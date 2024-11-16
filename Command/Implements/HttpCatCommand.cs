using Lagrange.Core;
using Lagrange.Core.Message;
using YounBot.Utils;
using static YounBot.Utils.MessageUtils;
namespace YounBot.Command.Implements;

public class HttpCatCommand {
    private readonly CooldownUtils _cooldown = new(10000L);
    [Command("httpcat", "Send cute http cat pictures")]
    public async void AcgPicture(BotContext context, MessageChain chain, int code)
    {
        var user = chain.FriendUin;
        if (!_cooldown.IsTimePassed(user))
        {
            if (_cooldown.ShouldSendCooldownNotice(user))
                await SendMessage(context, chain, $"你可以在 {_cooldown.GetLeftTime(user) / 1000} 秒后继续使用该指令");
            return;
        }

        _cooldown.Flag(user);
        await SendImage(context, chain, await ImageUtils.UrlToImageMessageAsync($"https://http.cat/{code}"));
    }
}