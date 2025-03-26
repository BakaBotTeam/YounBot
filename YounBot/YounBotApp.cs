using System.Diagnostics;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PrivateBinSharp;
using QRCoder;
using YounBot.Command;
using YounBot.Config;
using YounBot.Listener;
using YounBot.Listener.MessageFilter;
using YounBot.Signer;
using YounBot.Utils;
using LogLevel = Lagrange.Core.Event.EventArg.LogLevel;
using static YounBot.Utils.FileUtils;

namespace YounBot;

public class YounBotApp(YounBotAppBuilder appBuilder)
{
    public static IConfiguration Configuration;
    public static BotContext? Client;
    public static YounBotConfig? Config;
    public static LiteDatabase? Db;
    public static BotKeystore? Keystore;
    public static string VERSION;
    public static long? UpTime;
    public static PrivateBinClient? PrivateBinClient;
    
    public Task Init(BotConfig config, BotDeviceInfo deviceInfo, BotKeystore? keystore, string version)
    {
        VERSION = version;
        Configuration = appBuilder.GetConfiguration();
        Keystore = keystore;
        OneBotSigner signer = new(Configuration, LoggingUtils.Logger);
        config.CustomSignProvider = signer;
        Client = Keystore == null ? BotFactory.Create(config, uint.Parse(Configuration["Account:Uin"]??"0"), Configuration["Account:Password"]??"", signer.GetAppInfo(), out deviceInfo) : BotFactory.Create(config, deviceInfo, Keystore, signer.GetAppInfo());
        Config = appBuilder.GetYounBotConfig();
        
        if (Config.PrivateBinUrl != "null")
        {
            PrivateBinClient = new PrivateBinClient(Config.PrivateBinUrl);
        }
        
        LoggingUtils.Logger.LogInformation("Running on YounBot " + version);
        
        Client!.Invoker.OnBotLogEvent += (_, @event) =>
        {
            ILogger logger = LoggingUtils.CreateLogger(@event.Tag);
            switch (@event.Level)
            {
                case LogLevel.Debug:
                    logger.LogDebug(@event.EventMessage);
                    break;
                case LogLevel.Verbose:
                case LogLevel.Information:
                    logger.LogInformation(@event.EventMessage);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(@event.EventMessage);
                    break;
                case LogLevel.Exception:
                    logger.LogError(@event.EventMessage);
                    break;
                case LogLevel.Fatal:
                    logger.LogCritical(@event.EventMessage);
                    break;
            }
        };
        
        Client!.Invoker.OnBotOnlineEvent += (_, @event) =>
        {
            Keystore = Client.UpdateKeystore();
            SaveConfig(Configuration["ConfigPath:Keystore"] ?? "keystore.json", Keystore, true);
            LoggingUtils.Logger.LogInformation($"Bot online -> {@event.EventMessage}");
        };

        Client!.Invoker.OnBotOfflineEvent += (_, @event) =>
        {
            LoggingUtils.Logger.LogWarning($"Bot offline -> {@event.Message}");
        };
        
        Client.Invoker.OnBotCaptchaEvent += async (_, args) =>
        {
            LoggingUtils.Logger.LogWarning($"Captcha: {args.Url}");

            await Task.Run(() =>
            {
                LoggingUtils.Logger.LogWarning("Please input ticket:");
                string? ticket = Console.ReadLine();
                LoggingUtils.Logger.LogWarning("Please input randomString:");
                string? randomString = Console.ReadLine();

                if (ticket != null && randomString != null) Client.SubmitCaptcha(ticket, randomString);
            });
        };

        Client.Invoker!.OnBotNewDeviceVerify += async (_, args) =>
        {
            using (QRCodeGenerator qrGenerator = new())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(args.Url, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new(qrCodeData))
            using (AsciiQRCode asciiQrCode = new(qrCodeData))
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
        };

        CommandManager.Instance.InitializeCommands();
        AntiAd.Init();
        AntiBannableMessage.Init();

        Db = new LiteDatabase("YounBot.db");
        
        return Task.CompletedTask;
    }
    
    public Task Run()
    {
        // Hookers.Init(); R.I.P My hookers

        Client!.Invoker.OnGroupMemberIncreaseEvent += async (_, args) =>
        {
            if (args.MemberUin == Client.BotUin) return;
            await NewMemberWelcome.OnGroupMemberIncrease(args);
        };
        
        Client!.Invoker.OnGroupMessageReceived += async (context, @event) =>
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            MessageCounter.AddMessageReceived(DateTimeOffset.Now.ToUnixTimeSeconds());
            if (@event.Chain.FriendUin == context.BotUin) return;
            await AntiSpammer.OnGroupMessage(context, @event);
            await AntiBannableMessage.OnGroupMessage(context, @event);
            if (!Config!.CloudFlareAuthToken!.Equals("")) 
                await AntiAd.OnGroupMessage(context, @event);
            
            if (Config!.BlackLists!.Contains(@event.Chain.FriendUin)) return;
            string text = MessageUtils.GetPlainText(@event.Chain);
            string commandPrefix = Configuration["CommandPrefix"] ?? "/"; // put here for auto reload
            if (text.StartsWith(commandPrefix))
            {
                await CommandManager.Instance.ExecuteCommand(context, @event.Chain, text.Substring(commandPrefix.Length));
            }
            stopwatch.Stop();
            InformationCollector.MessageInvokeCount[DateTimeOffset.Now.ToUnixTimeMilliseconds()] = stopwatch.ElapsedMilliseconds;
        };

        Client!.Invoker.OnGroupInvitationReceived += async (context, @event) =>
        {
            if (@event.InvitorUin != Config!.BotOwner) return;
            try
            {
                LoggingUtils.Logger.LogInformation($"Received group invitation: {@event}");
                BotGroupRequest invitation = (await context.FetchGroupRequests())!.FindLast(x => x.GroupUin == @event.GroupUin && x.InvitorMemberUin == @event.InvitorUin)!;
                await context.SetGroupRequest(invitation, true);
            } 
            catch (Exception e)
            {
                LoggingUtils.Logger.LogWarning(e.ToString());
            }
        };
        
        UpTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        return Task.CompletedTask;
    }
}