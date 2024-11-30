namespace YounBot.Config;

[Serializable]
public class YounBotConfig
{
    public string? HypixelApiKey { get; set; }
    public uint BotOwner { get; set; }
    public string? WorkersAiUrl { get; set; }
    public string? WorkersAiBasicAuth { get; set; }
    public int MaxMessageCache { get; set; }
    public int MaxGroupMessageCache { get; set; }
    public List<uint>? BotAdmins { get; set; }
    
    public static YounBotConfig NewConfig() => new()
    {
        HypixelApiKey = "00000000-0000-0000-0000-00000000",
        BotOwner = 0,
        WorkersAiUrl = "http://0.0.0.0/",
        WorkersAiBasicAuth = "username:password",
        MaxMessageCache = 12,
        MaxGroupMessageCache = 25,
        BotAdmins = new List<uint>()
    };
}