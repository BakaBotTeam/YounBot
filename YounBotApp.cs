using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YounBot;

public class YounBotApp
{
    public IServiceCollection Services;
    public IConfiguration Configuration;
    
    public YounBotApp(YounBotAppBuilder builder)
    {
        Services = builder.GetServices();
        Configuration = builder.GetConfiguration();
    }
}