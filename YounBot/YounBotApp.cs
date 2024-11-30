using System.Diagnostics;
using HarmonyLib;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using YounBot.Command;
using YounBot.Config;
using YounBot.MessageFilter;
using YounBot.Utils;
using JsonSerializer = System.Text.Json.JsonSerializer;
using LogLevel = Lagrange.Core.Event.EventArg.LogLevel;

namespace YounBot;

public class YounBotApp(YounBotAppBuilder appBuilder)
{
    public static IConfiguration Configuration;
    public static BotContext? Client;
    public static YounBotConfig? Config;
    public static LiteDatabase? Db;
    public static string VERSION;
    public static long? UpTime;
    
    public Task Init(BotConfig config, BotDeviceInfo deviceInfo, BotKeystore? keystore, string version)
    {
        VERSION = version;
        Configuration = appBuilder.GetConfiguration();
        Client = keystore == null ? BotFactory.Create(config, uint.Parse(Configuration["Account:Uin"]??"0"), Configuration["Account:Password"]??"", out deviceInfo) : BotFactory.Create(config, deviceInfo, keystore);
        Config = appBuilder.GetYounBotConfig();
        
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
        
        Client!.Invoker.OnBotOnlineEvent += (_, _) =>
        {
            Client.UpdateKeystore();
            File.WriteAllText(Configuration["ConfigPath:Keystore"] ?? "keystore.json", JsonSerializer.Serialize(appBuilder.GetKeystore()));
            LoggingUtils.Logger.LogInformation("Bot online");
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
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(args.Url, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            using (AsciiQRCode asciiQrCode = new AsciiQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                await File.WriteAllBytesAsync("qrcode.png", qrCodeImage);
                // Open the QR code image
                try
                {
                    Process.Start("qrcode.png");
                }
                catch (Exception e)
                {
                    LoggingUtils.Logger.LogError("Failed to open QR code image, please open it manually: " + e.Message);
                }

                LoggingUtils.Logger.LogInformation("Please scan the QR code to login\n" + asciiQrCode.GetGraphic(1));
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
        Hookers.Init();
        
        Client!.Invoker.OnGroupMessageReceived += async (context, @event) =>
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            MessageCounter.AddMessageReceived(DateTimeOffset.Now.ToUnixTimeSeconds());
            if (@event.Chain.FriendUin == context.BotUin) return;
            await AntiSpammer.OnGroupMessage(context, @event);
            await AntiBannableMessage.OnGroupMessage(context, @event);
            if (!Config!.WorkersAiUrl!.Equals("http://0.0.0.0/")) 
                await AntiAd.OnGroupMessage(context, @event);
            
            // ILiteCollection<BsonValue>? collection = Db!.GetCollection<BsonValue>("blacklist");
            // if (collection.Exists(x => x == new BsonValue(@event.Chain.FriendUin.ToString()))) return;
            string text = MessageUtils.GetPlainText(@event.Chain);
            string commandPrefix = Configuration["CommandPrefix"] ?? "/"; // put here for auto reload
            if (text.StartsWith(commandPrefix))
            {
                await CommandManager.Instance.ExecuteCommand(context, @event.Chain, text.Substring(commandPrefix.Length));
            }
            stopwatch.Stop();
            InformationCollector.MessageInvokeCount[DateTimeOffset.Now.ToUnixTimeMilliseconds()] = stopwatch.ElapsedMilliseconds;
        };
        
        Client!.Invoker.OnFriendRequestEvent += async (context, @event) =>
        {
            try
            {
                await context.SetFriendRequest(@event);
            }
            catch (Exception e)
            {
                LoggingUtils.Logger.LogWarning(e.ToString());
            }
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