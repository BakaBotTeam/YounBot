namespace YounBot.Config;

[Serializable]
public class YounBotConfig
{
    public string? HypixelApiKey { get; set; }
    public uint BotOwner { get; set; }
    
    public static YounBotConfig NewConfig() => new()
    {
        HypixelApiKey = "00000000-0000-0000-0000-00000000",
        BotOwner = 0
    };
}