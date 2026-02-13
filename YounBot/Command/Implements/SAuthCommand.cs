using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.Logging;
using PrivateBinSharp;
using YounBot.Utils;

namespace YounBot.Command.Implements;

public class SAuthCommand
{
    CooldownUtils Cooldown = new(30000);

    [Command("sauth", "转SAuth")]
    public async Task SAuth(BotContext context, MessageChain chain, string account, string password)
    {
        if (!Cooldown.IsTimePassed(chain.FriendUin))
        {
            if (Cooldown.ShouldSendCooldownNotice(chain.FriendUin))
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Forward(chain).Text($"你可以在 {Cooldown.GetLeftTime(chain.FriendUin) / 1000} 秒后继续使用该指令").Build());
            return;
        }

        try
        {
            Cooldown.Flag(chain.FriendUin);
            string url = YounBotApp.Config!.SauthApi;
            if (url == "null")
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Forward(chain).Text("接口未配置，请联系管理员").Build());
                return;
            }
            JsonObject postData = new()
            {
                ["username"] = account,
                ["password"] = password
            };
            Dictionary<string, string> headers = new()
            {
                ["User-Agent"] = "YounBot/1.0"
            };
            JsonObject response = await HttpUtils.PostJsonObject(url, headers: headers, data: postData);
            if (response["success"]?.GetValue<bool>() != true)
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Forward(chain).Text($"登录失败，服务器返回: {response["message"]?.GetValue<string>() ?? "未知"}").Build());
                return;
            }
            if (response["sauth_json"] == null)
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Forward(chain).Text("登录失败，未获取到sauth").Build());
                return;
            }
            string sauthJson = response["sauth_json"]!.GetValue<string>();
            if (YounBotApp.PrivateBinClient == null)
            {
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Forward(chain).Text("PrivateBin客户端未初始化，无法上传sauth").Build());
                return;
            }
            // unwarp sauthJson
            Paste paste = await YounBotApp.PrivateBinClient.CreatePaste(sauthJson, "", burnAfterReading: true);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Forward(chain).Text($"登录成功, 点击链接查看: {paste.ViewURL}\n此 Url 5 分钟内有效, 且阅后即焚").Build());
        }
        catch (Exception ex)
        {
            LoggingUtils.CreateLogger().LogError("SAuth命令执行时出现错误: {0}", ex);
            await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Forward(chain).Text("请求时出现错误").Build());
            Cooldown.AddLeftTime(chain.FriendUin, -600000);
        }
    }
}