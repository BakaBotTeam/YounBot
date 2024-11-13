using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Console = System.Console;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace YounBot.Login;

public static class QrCodeLogin
{
    public static async Task Login(YounBotApp app, BotConfig config, BotDeviceInfo deviceInfo, BotKeystore keystore)
    {
        app.Client = BotFactory.Create(config, deviceInfo, keystore);
        var client = app.Client;
        
        client.Invoker.OnBotLogEvent += (_, @event) => {
            Console.WriteLine(@event.ToString());
        };
        
        client.Invoker.OnBotOnlineEvent += (_, @event) =>
        {
            Console.WriteLine(@event.ToString());
            client.UpdateKeystore();
            File.WriteAllText(app.Configuration["ConfigPath:Keystore"] ?? "keystore.json", JsonSerializer.Serialize(keystore));
        };

        if (!await app.Client.LoginByPassword())
        {
            var qrCode = await client.FetchQrCode();

            if (qrCode != null)
            {
                await File.WriteAllBytesAsync("qrcode.png", qrCode.Value.QrCode);
                await app.Client.LoginByQrCode();
            }
        }
    }
}