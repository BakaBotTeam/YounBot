using System.Security.Cryptography;
using System.Text;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using YounBot.Utils;
using static YounBot.Utils.MessageUtils;

namespace YounBot.Command.Implements;

public class AltCommand
{
    private readonly CooldownUtils _cooldown = new(60000L);
    
    [Command("4399", "获取4399小号")]
    public async Task Get4399(BotContext context, MessageChain chain)
    {
        var user = chain.FriendUin;
        
        try
        {
            var friend = (await context.FetchFriends()).FindLast(f => f.Uin == user);
            
            if (friend == null)
            {
                await SendMessage(context, chain, "请先添加机器人为好友, 再在群聊使用指令");
                return;
            }
            
            if (!_cooldown.IsTimePassed(user))
            {
                if (_cooldown.ShouldSendCooldownNotice(user))
                    await SendMessage(context, chain, $"你可以在 {_cooldown.GetLeftTime(user) / 1000} 秒后继续使用该指令");
                return;
            }
        
            _cooldown.Flag(user);
        
            if (YounBotApp.Configuration["4399AltApiKey"] == null || YounBotApp.Configuration["4399AltApiKey"] == "")
            {
                await SendMessage(context, chain, "未配置API密钥");
                return;
            }
        
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signature = GenerateSignature(ts.ToString(), YounBotApp.Configuration["4399AltApiKey"]!);
            var url = $"https://4399.cuteguimc.win/fetchapi.php?ts={ts}&signature={signature}";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                await context.SendMessage(MessageBuilder.Friend(friend.Uin).Text("小号: ").Text(responseBody).Build());
                await SendMessage(context, chain, "请检查私聊", true);
            }
        }
        catch (Exception e)
        {
            _cooldown.AddLeftTime(user, -60000L);
            throw new Exception("无法获取小号", e);
        }
    }
    
    static string GenerateSignature(string data, string key)
    {
        using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}