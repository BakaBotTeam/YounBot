using System.Diagnostics;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    
    public Task Init(BotConfig config, BotDeviceInfo deviceInfo, BotKeystore keystore, string version)
    {
        VERSION = version;
        Configuration = appBuilder.GetConfiguration();
        Client = BotFactory.Create(config, deviceInfo, keystore);
        Config = appBuilder.GetYounBotConfig();
        
        LoggingUtils.CreateLogger().LogInformation("Running on YounBot " + version);
        
        Client!.Invoker.OnBotLogEvent += (_, @event) =>
        {
            var logger = LoggingUtils.CreateLogger(@event.Tag);
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
            LoggingUtils.CreateLogger().LogInformation("Bot online");
        };

        Client!.Invoker.OnBotOfflineEvent += (_, @event) =>
        {
            LoggingUtils.CreateLogger().LogWarning($"Bot offline -> {@event.Message}");
        };

        CommandManager.Instance.InitializeCommands();
        AntiAd.Init();
        Db = new LiteDatabase("YounBot-MessageFilter.db");
        
        return Task.CompletedTask;
    }
    
    public Task Run()
    {
        Hookers.Init();
        
        Client!.Invoker.OnGroupMessageReceived += async (context, @event) =>
        {
            var stopwatch = Stopwatch.StartNew();
            MessageCounter.AddMessageReceived(DateTimeOffset.Now.ToUnixTimeSeconds());
            if (@event.Chain.FriendUin == context.BotUin) return;
            await AntiSpammer.OnGroupMessage(context, @event);
            if (!Config!.WorkersAiUrl!.Equals("http://0.0.0.0/")) 
                await AntiAd.OnGroupMessage(context, @event);
            
            var text = MessageUtils.GetPlainText(@event.Chain);
            var commandPrefix = Configuration["CommandPrefix"] ?? "/"; // put here for auto reload
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
                LoggingUtils.CreateLogger().LogWarning(e.ToString());
            }
        };
        
        UpTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        return Task.CompletedTask;
    }
}