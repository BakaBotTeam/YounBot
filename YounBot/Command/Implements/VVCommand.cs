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
        Task<MessageResult> preMessage = context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text("让我找找...").Build());
        string url = $"https://api.zvv.quest/search?q={UrlEncoder.Default.Encode(keyword)}&n=1";
        JsonObject response = await HttpUtils.GetJsonObject(url);
        if (response["code"].GetValue<int>() != 200)
        {
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Text("找不到...").Build());
            return;
        }
        string vvUrl = response["data"][0].GetValue<string>();
        byte[] image = await HttpUtils.GetBytes(vvUrl);
        preMessage.Wait();
        await context.RecallGroupMessage(chain.GroupUin!.Value, preMessage.Result);
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin.Value).Image(image).Build());
    }
}