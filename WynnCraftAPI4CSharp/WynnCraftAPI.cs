using YounBot.WynnCraftAPI4CSharp.API;
using YounBot.WynnCraftAPI4CSharp.Http;
using YounBot.WynnCraftAPI4CSharp.Http.Implements;

namespace YounBot.WynnCraftAPI4CSharp;

public class WynnCraftApi
{
    public static string BaseUrl = "https://api.wynncraft.com/v3/";
    private readonly IWynnHttpClient client;
    private readonly PlayerApi player;
    
    public PlayerApi Player => player;
    public WynnCraftApi(IWynnHttpClient client)
    {
        this.client = client;
        this.player = new PlayerApi(client);
    }
    
    public WynnCraftApi() : this(new DefaultHttpClient()) { }
}