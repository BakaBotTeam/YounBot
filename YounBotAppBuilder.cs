using System;
using System.IO;
using Lagrange.Core.Common;
using Microsoft.Extensions.Configuration;
using YounBot.Config;
using JsonSerializer = System.Text.Json.JsonSerializer;
using static YounBot.Utils.FileUtils;
namespace YounBot;

public sealed class YounBotAppBuilder(IConfiguration configuration)
{
    private BotDeviceInfo _deviceInfo;
    private BotKeystore _keystore;
    private BotConfig _botConfig;
    private YounBotConfig _younBotConfig;
    
    public IConfiguration GetConfiguration() => configuration;
    public BotDeviceInfo GetDeviceInfo() => _deviceInfo;
    public BotKeystore GetKeystore() => _keystore;
    public BotConfig GetConfig() => _botConfig;
    public YounBotConfig GetYounBotConfig() => _younBotConfig;
    
    public void ConfigureBots()
    {
        var keystorePath = configuration["ConfigPath:Keystore"] ?? "keystore.json";
        var deviceInfoPath = configuration["ConfigPath:DeviceInfo"] ?? "device.json";
        var configPath = "younbot-config.json";
            

        bool isSuccess = Enum.TryParse<Protocols>(configuration["Account:Protocol"], out var protocol);
        
        _botConfig = new BotConfig
        {
            Protocol = isSuccess ? protocol : Protocols.Linux,
            AutoReconnect = bool.Parse(configuration["Account:AutoReconnect"] ?? "true"),
            UseIPv6Network = bool.Parse(configuration["Account:UseIPv6Network"] ?? "false"),
            GetOptimumServer = bool.Parse(configuration["Account:GetOptimumServer"] ?? "true"),
            AutoReLogin = bool.Parse(configuration["Account:AutoReLogin"] ?? "true"),
        };

        _keystore = new BotKeystore();
        SaveConfig(keystorePath, _keystore);
        _keystore = JsonSerializer.Deserialize<BotKeystore>(File.ReadAllText(keystorePath)) ?? new BotKeystore();

        _deviceInfo = BotDeviceInfo.GenerateInfo();
        SaveConfig(deviceInfoPath, _deviceInfo);
        _deviceInfo = JsonSerializer.Deserialize<BotDeviceInfo>(File.ReadAllText(deviceInfoPath)) ?? BotDeviceInfo.GenerateInfo();
        
        _younBotConfig = YounBotConfig.NewConfig();
        SaveConfig(configPath, _younBotConfig);
        _younBotConfig = JsonSerializer.Deserialize<YounBotConfig>(File.ReadAllText(configPath)) ?? YounBotConfig.NewConfig();
        
    }

    public YounBotApp Build() => new(this);
}
