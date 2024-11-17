using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using LiteDB;
using Microsoft.Extensions.Configuration;
using YounBot.Command;
using YounBot.Config;
using YounBot.Utils;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace YounBot;

public class YounBotApp(YounBotAppBuilder appBuilder)
{
    private readonly IConfiguration _configuration = appBuilder.GetConfiguration();
    public BotContext? Client;
    public static YounBotConfig? Config;
    public static LiteDatabase? Db;
    
    public Task Init()
    {
        Client!.Invoker.OnBotLogEvent += (_, @event) => {
            Console.WriteLine(@event.ToString());
        };
        
        Client!.Invoker.OnBotOnlineEvent += (_, _) =>
        {
            Console.WriteLine("Logged in successfully!");
            Client.UpdateKeystore();
            File.WriteAllText(_configuration["ConfigPath:Keystore"] ?? "keystore.json", JsonSerializer.Serialize(appBuilder.GetKeystore()));
        };
        
        CommandManager.Instance.InitializeCommands();
        Config = appBuilder.GetYounBotConfig();
        MessageFilter.AntiAd.Init();
        Db = new LiteDatabase("YounBot-MessageFilter.db");
        
        return Task.CompletedTask;
    }
    
    public Task Run()
    {
        Client!.Invoker.OnGroupMessageReceived += async (context, @event) =>
        {
            if (@event.Chain.FriendUin == context.BotUin) return;
            await MessageFilter.AntiSpammer.OnGroupMessage(context, @event);
            if (!Config!.WorkersAiUrl!.Equals("http://0.0.0.0/")) 
                await MessageFilter.AntiAd.OnGroupMessage(context, @event);
        };
        
        Client!.Invoker.OnGroupMessageReceived += async (context, @event) =>
        {
            var text = MessageUtils.GetPlainText(@event.Chain);
            var commandPrefix = _configuration["CommandPrefix"] ?? "/"; // put here for auto reload
            if (text.StartsWith(commandPrefix))
            {
                await CommandManager.Instance.ExecuteCommand(context, @event.Chain, text.Substring(commandPrefix.Length));
            }
        };

        return Task.CompletedTask;
    }
}