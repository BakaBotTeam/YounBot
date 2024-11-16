using System.Threading.Tasks;

namespace YounBot.Utils;

public static class MojangApiUtils
{
    public static async Task<string> GetUuidByName(string name)
    {
        var result = await HttpUtils.GetJsonObject($"https://api.mojang.com/users/profiles/minecraft/{name}");
        
        return result["id"]!.ToJsonString();
    } 
}