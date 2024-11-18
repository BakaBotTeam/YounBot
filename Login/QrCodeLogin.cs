using Lagrange.Core.Common.Interface.Api;

namespace YounBot.Login;

public static class QrCodeLogin
{
    public static async Task Login(YounBotApp app)
    {
        if (!await app.Client.LoginByPassword())
        {
            var qrCode = await app.Client.FetchQrCode();

            if (qrCode != null)
            {
                await File.WriteAllBytesAsync("qrcode.png", qrCode.Value.QrCode);
                await app.Client.LoginByQrCode();
            }
        }
    }
}