using System.Text.Json;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YounBot.Command;

namespace YounBot;

public class YounBotApp(YounBotAppBuilder appBuilder)
{
    public readonly IConfiguration Configuration = appBuilder.GetConfiguration();
    public BotContext? Client;
    
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

        return Task.CompletedTask;
    }
    
    public Task Run()
    {
        Client!.Invoker.OnGroupMessageReceived += (context, @event) =>
        {
            var text = @event.Chain.ToPreviewText();
            if (text.StartsWith("/"))
            {
                CommandManager.Instance.ExecuteCommand(context, @event.Chain, text);
            }
        };
        
        Client!.Invoker.OnGroupMemberIncreaseEvent += (context, @event) =>
        {
            context.SendMessage(MessageBuilder.Group(@event.GroupUin).Text($"A new member '{@event.MemberUin}' joined in the group!").Build());
        };

        return Task.CompletedTask;
    }
}