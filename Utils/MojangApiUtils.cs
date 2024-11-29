using System.Text.Json.Nodes;

namespace YounBot.Utils;

public static class MojangApiUtils
{
    public static async Task<string> GetUuidByName(string name)
    {
        JsonObject result = await HttpUtils.GetJsonObject($"https://api.mojang.com/users/profiles/minecraft/{name}");
        
        return result["id"]!.ToJsonString();
    } 
}