using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using YounBot.WynnCraftAPI4CSharp.Model.Search;

namespace YounBot.WynnCraftAPI4CSharp.Json;

public class PlayerSearchResultMapAdapter : JsonConverter<Dictionary<Guid, PlayerSearchResult>>
{
    public override Dictionary<Guid, PlayerSearchResult> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonNode? jsonDocument = JsonObject.Parse(ref reader);
        JsonObject jsonObject = jsonDocument.AsObject();
        Dictionary<Guid, PlayerSearchResult> playerResultMap = new Dictionary<Guid, PlayerSearchResult>();

        foreach (KeyValuePair<string, JsonNode?> property in jsonObject)
        {
            JsonNode? playerResultObj = property.Value;
            playerResultObj["uuid"] = property.Key;

            PlayerSearchResult? playerSearchResult = JsonSerializer.Deserialize<PlayerSearchResult>(playerResultObj.ToJsonString(), options);
            if (playerSearchResult != null)
            {
                playerResultMap[Guid.Parse(property.Key)] = playerSearchResult;
            }
        }

        return playerResultMap;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<Guid, PlayerSearchResult> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Serialization is not implemented for this converter.");
    }
}