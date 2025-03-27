using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using YounBot.Utils;

namespace YounBot.Command.Implements;

public class VvCommand
{
    [Command("vv", "你能跟我看一辈子的vv吗")]
    public async Task Vv(BotContext context, MessageChain chain, string keyword)
    {
        string url = $"https://vvsearch.top/api/mygo/img?keyword={UrlEncoder.Default.Encode(keyword)}";
        JsonObject response = await HttpUtils.GetJsonObject(url);
        string vvUrl = response["urls"][0]["url"].GetValue<string>();
        byte[] image = await HttpUtils.GetBytes(vvUrl);
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Image(image).Build());
    }
}