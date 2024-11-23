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
    public BotContext? Client;
    public static YounBotConfig? Config;
    public static LiteDatabase? Db;
    
    public Task Init(BotConfig config, BotDeviceInfo deviceInfo, BotKeystore keystore)
    {
        Configuration = appBuilder.GetConfiguration();
        Client = BotFactory.Create(config, deviceInfo, keystore);
        Config = appBuilder.GetYounBotConfig();
        
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
        };

        Client!.Invoker.OnBotOfflineEvent += (_, @event) =>
        {
            LoggingUtils.CreateLogger().LogWarning($"机器人已下线 -> {@event.Message}");
        };

        CommandManager.Instance.InitializeCommands();
        AntiAd.Init();
        Db = new LiteDatabase("YounBot-MessageFilter.db");
        
        return Task.CompletedTask;
    }
    
    public Task Run()
    {
        Client!.Invoker.OnGroupMessageReceived += async (context, @event) =>
        {
            if (@event.Chain.FriendUin == context.BotUin) return;
            await AntiSpammer.OnGroupMessage(context, @event);
            if (!Config!.WorkersAiUrl!.Equals("http://0.0.0.0/")) 
                await AntiAd.OnGroupMessage(context, @event);
        };
        
        Client!.Invoker.OnGroupMessageReceived += async (context, @event) =>
        {
            var text = MessageUtils.GetPlainText(@event.Chain);
            var commandPrefix = Configuration["CommandPrefix"] ?? "/"; // put here for auto reload
            if (text.StartsWith(commandPrefix))
            {
                await CommandManager.Instance.ExecuteCommand(context, @event.Chain, text.Substring(commandPrefix.Length));
            }
        };

        return Task.CompletedTask;
    }
}