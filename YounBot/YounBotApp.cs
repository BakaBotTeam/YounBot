﻿using System.Diagnostics;
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
    
    public Task Init(BotConfig config, BotDeviceInfo deviceInfo, BotKeystore? keystore, string version)
    {
        VERSION = version;
        Configuration = appBuilder.GetConfiguration();
        Keystore = keystore;
        Client = Keystore == null ? BotFactory.Create(config, uint.Parse(Configuration["Account:Uin"]??"0"), Configuration["Account:Password"]??"", out deviceInfo) : BotFactory.Create(config, deviceInfo, Keystore);
        Client.Config.CustomSignProvider = new OneBotSigner(Configuration, LoggingUtils.Logger, Client);
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