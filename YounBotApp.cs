using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YounBot.Command;
using YounBot.Config;
using YounBot.Utils;

namespace YounBot;

public class YounBotApp(YounBotAppBuilder appBuilder)
{
    public readonly IConfiguration Configuration = appBuilder.GetConfiguration();
    public BotContext? Client;
    public static YounBotConfig? Config;
    
    public Task Init()
    {
        Client!.Invoker.OnBotLogEvent += (_, @event) => {
            Console.WriteLine(@event.ToString());
        };
        
        Client!.Invoker.OnBotOnlineEvent += (_, @event) =>
        {
            Console.WriteLine("Logged in successfully!");
            Client.UpdateKeystore();
            File.WriteAllText(Configuration["ConfigPath:Keystore"] ?? "keystore.json", JsonSerializer.Serialize(appBuilder.GetKeystore()));
        };
        
        CommandManager.Instance.InitializeCommands();
        Config = appBuilder.GetYounBotConfig();

        return Task.CompletedTask;
    }
    
    public Task Run()
    {
        MessageFilter.MessageFilter.Init();
        
        // Client!.Invoker.OnGroupMessageReceived += async (context, @event) =>
        // {
        // };
        
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