namespace YounBot.Config;

[Serializable]
public class YounBotConfig
{
    public string GrokApiKey { get; set; } = "00000000-0000-0000-0000-00000000";
    public string HypixelApiKey { get; set; } = "00000000-0000-0000-0000-00000000";
    public string PrivateBinUrl { get; set; } = "null";
    public uint BotOwner { get; set; }
    public string CloudFlareAccountID { get; set; } = "";
    public string CloudFlareGatewayID { get; set; } = "";
    public string CloudFlareAuthToken { get; set; } = "";
    public int MaxMessageCache { get; set; } = 12;
    public int MaxGroupMessageCache { get; set; } = 25;
    public List<uint> BotAdmins { get; set; } = new();
    public List<uint> BlackLists { get; set; } = new();
    
    public static YounBotConfig NewConfig() => new()
    {
        GrokApiKey = "00000000-0000-0000-0000-00000000",
        HypixelApiKey = "00000000-0000-0000-0000-00000000",
        BotOwner = 0,
        MaxMessageCache = 12,
        MaxGroupMessageCache = 25,
        BotAdmins = new List<uint>(),
        BlackLists = new List<uint>(),
        PrivateBinUrl = "null"
    };
}