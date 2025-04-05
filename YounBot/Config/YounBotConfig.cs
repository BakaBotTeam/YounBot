namespace YounBot.Config;

[Serializable]
public class YounBotConfig
{
    public string GrokApiKey { get; set; } = "00000000-0000-0000-0000-00000000";
    public string HypixelApiKey { get; set; } = "00000000-0000-0000-0000-00000000";
    public string PrivateBinUrl { get; set; } = "null";
    public string EasyImageApiUrl { get; set; } = "https://example.com/api/index.php";
    public string EasyImageApiKey { get; set; } = "00000000-0000-0000-0000-00000000";
    public uint BotOwner { get; set; }
    public string CloudFlareAccountID { get; set; } = "";
    public string CloudFlareGatewayID { get; set; } = "";
    public string CloudFlareAuthToken { get; set; } = "";
    public int MaxMessageCache { get; set; } = 12;
    public int MaxGroupMessageCache { get; set; } = 25;
    public List<uint> BotAdmins { get; set; } = new();
    public List<uint> BlackLists { get; set; } = new();
    public string DeepSeekApiKey { get; set; } = "sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

    public static YounBotConfig NewConfig() => new()
    {
        GrokApiKey = "00000000-0000-0000-0000-00000000",
        HypixelApiKey = "00000000-0000-0000-0000-00000000",
        DeepSeekApiKey = "sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
        BotOwner = 0,
        MaxMessageCache = 12,
        MaxGroupMessageCache = 25,
        BotAdmins = new List<uint>(),
        BlackLists = new List<uint>(),
        PrivateBinUrl = "null",
        EasyImageApiUrl = "https://example.com/api/index.php",
        EasyImageApiKey = "00000000-0000-0000-0000-00000000",
    };
}