using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace YounBot;

public sealed class YounBotAppBuilder
{
    private IServiceCollection Services;

    private IConfiguration Configuration;

    public YounBotAppBuilder(IServiceCollection services, IConfiguration configuration)
    {
        Services = services;
        Configuration = configuration;
    }
    
    public IServiceCollection GetServices() => Services;
    public IConfiguration GetConfiguration() => Configuration;

    public YounBotAppBuilder ConfigureBots()
    {
        string keystorePath = Configuration["ConfigPath:Keystore"] ?? "keystore.json";
        string deviceInfoPath = Configuration["ConfigPath:DeviceInfo"] ?? "device.json";

        bool isSuccess = Enum.TryParse<Protocols>(Configuration["Account:Protocol"], out var protocol);
        var config = new BotConfig
        {
            Protocol = isSuccess ? protocol : Protocols.Linux,
            AutoReconnect = bool.Parse(Configuration["Account:AutoReconnect"] ?? "true"),
            UseIPv6Network = bool.Parse(Configuration["Account:UseIPv6Network"] ?? "false"),
            GetOptimumServer = bool.Parse(Configuration["Account:GetOptimumServer"] ?? "true"),
            AutoReLogin = bool.Parse(Configuration["Account:AutoReLogin"] ?? "true"),
        };

        BotKeystore keystore;
        if (!File.Exists(keystorePath))
        {
            keystore = new BotKeystore();
            string? directoryPath = Path.GetDirectoryName(keystorePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        else
        {
            keystore = JsonSerializer.Deserialize<BotKeystore>(File.ReadAllText(keystorePath)) ?? new BotKeystore();
        }

        BotDeviceInfo deviceInfo;
        if (!File.Exists(deviceInfoPath))
        {
            deviceInfo = BotDeviceInfo.GenerateInfo();
            string json = JsonSerializer.Serialize(deviceInfo);
            string? directoryPath = Path.GetDirectoryName(deviceInfoPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(deviceInfoPath, json);
        }
        else
        {
            deviceInfo = JsonSerializer.Deserialize<BotDeviceInfo>(File.ReadAllText(deviceInfoPath)) ?? BotDeviceInfo.GenerateInfo();
        }

        Services.AddSingleton(BotFactory.Create(config, deviceInfo, keystore));

        return this;
    }

    public YounBotAppBuilder ConfigureLogging(Action<ILoggingBuilder> configureLogging)
    {
        Services.AddLogging(configureLogging);
        return this;
    }

    public YounBotApp Build() => new(this);
}
