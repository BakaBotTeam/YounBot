using Lagrange.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YounBot;

public class YounBotApp(YounBotAppBuilder appBuilder)
{
    public readonly IConfiguration Configuration = appBuilder.GetConfiguration();
    public BotContext? Client;
}