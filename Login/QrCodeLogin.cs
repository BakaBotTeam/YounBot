using System.IO;
using System.Threading.Tasks;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;

namespace YounBot.Login;

public static class QrCodeLogin
{
    public static async Task Login(YounBotApp app, BotConfig config, BotDeviceInfo deviceInfo, BotKeystore keystore)
    {
        app.Client = BotFactory.Create(config, deviceInfo, keystore);
        var client = app.Client;
        
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