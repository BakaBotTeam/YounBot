using System;
using System.IO;
using System.Text.Json.Serialization;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YounBot.Config;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
        
        if (!File.Exists(keystorePath))
        {
            _keystore = new BotKeystore();
            string? directoryPath = Path.GetDirectoryName(keystorePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        else
        {
            _keystore = JsonSerializer.Deserialize<BotKeystore>(File.ReadAllText(keystorePath)) ?? new BotKeystore();
        }

        if (!File.Exists(deviceInfoPath))
        {
            _deviceInfo = BotDeviceInfo.GenerateInfo();
            string json = JsonSerializer.Serialize(_deviceInfo);
            string? directoryPath = Path.GetDirectoryName(deviceInfoPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(deviceInfoPath, json);
        }
        else
        {
            _deviceInfo = JsonSerializer.Deserialize<BotDeviceInfo>(File.ReadAllText(deviceInfoPath)) ?? BotDeviceInfo.GenerateInfo();
        }
        
        if (!File.Exists("younbot-config.json"))
        {
            _younBotConfig = YounBotConfig.NewConfig();
            var directoryPath = Path.GetDirectoryName("younbot-config.json");
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            var json = JsonSerializer.Serialize(_younBotConfig);
            
            File.WriteAllText("younbot-config.json", json);
        }
        else
        {
            _younBotConfig = JsonSerializer.Deserialize<YounBotConfig>(File.ReadAllText(configPath)) ?? YounBotConfig.NewConfig();
        }
    }

    public YounBotApp Build() => new(this);
}
