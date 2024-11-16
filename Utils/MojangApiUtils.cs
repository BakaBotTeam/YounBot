namespace YounBot.Utils;

public static class MojangApiUtils
{
    public static string GetUuidByName(string name)
    {
        return HttpUtils.GetJsonObject($"https://api.mojang.com/users/profiles/minecraft/{name}").Result["id"]!.ToJsonString();
    } 
}