using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace YounBot.Utils.Hypixel;

public static class HypixelApiUtils
{
    private static readonly HttpClient HttpClient = new();

    public static async Task<JsonObject> RequestAsync(string url)
    {
        var fullUrl = $"https://api.hypixel.net{url}";
        var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

        var apiKey = YounBotApp.Config!.HypixelApiKey!;
        request.Headers.Add("API-Key", apiKey);

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync();
        var jsonObject = JsonNode.Parse(raw)!.AsObject();

        if (!jsonObject["success"]!.GetValue<bool>())
        {
            var cause = jsonObject["cause"]?.ToString() ?? "Unknown error";
            throw new ApiFailedException($"Request failed with message \"{cause}\"");
        }

        return jsonObject;
    }

    public static string? ResolveRank(string rank)
    {
        return rank switch
        {
            "ADMIN" => "Admin",
            "MODERATOR" => "Moderator",
            "HELPER" => "Helper",
            "SUPERSTAR" => "MVP++",
            "MVP_PLUS" => "MVP+",
            "MVP" => "MVP",
            "VIP_PLUS" => "VIP+",
            "VIP" => "VIP",
            _ => null
        };
    }

    public static string ResolveGameType(string gameType)
    {
        return gameType switch
        {
            "QUAKECRAFT" => "Quake",
            "WALLS" => "Walls",
            "PAINTBALL" => "Paintball",
            "SURVIVAL_GAMES" => "Blitz Survival Games",
            "TNTGAMES" => "TNT Games",
            "VAMPIREZ" => "VampireZ",
            "WALLS3" => "Mega Walls",
            "ARCADE" => "Arcade",
            "ARENA" => "Arena",
            "UHC" => "UHC",
            "MCGO" => "Cops and Crims",
            "BATTLEGROUND" => "Warlords",
            "SUPER_SMASH" => "Smash Heroes",
            "GINGERBREAD" => "Turbo Kart Racers",
            "HOUSING" => "Housing",
            "SKYWARS" => "SkyWars",
            "TRUE_COMBAT" => "Crazy Walls",
            "SPEED_UHC" => "Speed UHC",
            "SKYCLASH" => "SkyClash",
            "LEGACY" => "Classic Games",
            "PROTOTYPE" => "Prototype",
            "BEDWARS" => "Bed Wars",
            "MURDER_MYSTERY" => "Murder Mystery",
            "BUILD_BATTLE" => "Build Battle",
            "DUELS" => "Duels",
            "SKYBLOCK" => "SkyBlock",
            "PIT" => "Pit",
            "REPLAY" => "Replay",
            "SMP" => "SMP",
            "WOOL_GAMES" => "Wool Wars",
            _ => gameType
        };
    }
}