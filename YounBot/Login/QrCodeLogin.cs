using System.Diagnostics;
using Lagrange.Core.Common.Interface.Api;
using Microsoft.Extensions.Logging;
using QRCoder;
using YounBot.Utils;

namespace YounBot.Login;

public static class QrCodeLogin
{
    public static async Task Login(YounBotApp app)
    {
        if ((YounBotApp.Configuration!["Account:Password"] != null &&
             YounBotApp.Configuration!["Account:Password"] != "") || YounBotApp.Keystore != null)
        {
            if (!(await YounBotApp.Client!.LoginByPassword()))
            {
                Environment.Exit(1);
            }
            return;
        }
        
        (string Url, byte[] QrCode)? qrCodeInfo = await YounBotApp.Client!.FetchQrCode();

        if (qrCodeInfo != null)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCodeInfo.Value.Url, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            using (AsciiQRCode asciiQrCode = new AsciiQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                await File.WriteAllBytesAsync("qrcode.png", qrCodeImage);
                // Open the QR code image
                try {
                    ProcessStartInfo startInfo = new("qrcode.png")
                    {
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                catch (Exception e)
                {
                    LoggingUtils.Logger.LogError("Failed to open QR code image, please open it manually: " + e.Message);
                }

                LoggingUtils.Logger.LogInformation("Please scan the QR code to login\n" + asciiQrCode.GetGraphicSmall());
            }
            await YounBotApp.Client!.LoginByQrCode();
        }
    }
}