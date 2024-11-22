using System.Text.Json;
using System.Text.Json.Serialization;
using YounBot.WynnCraftAPI4CSharp.Model.Search;

namespace YounBot.WynnCraftAPI4CSharp.Json;

public class CoordinateSearchResultMapAdapter : JsonConverter<Dictionary<string, CoordinateSearchResult>>
{
    public override Dictionary<string, CoordinateSearchResult> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonObject = JsonDocument.ParseValue(ref reader).RootElement;
        var coordinateResultMap = new Dictionary<string, CoordinateSearchResult>();

        foreach (var property in jsonObject.EnumerateObject())
        {
            var coordinateResultObj = property.Value;
            var coordinateResult = JsonSerializer.Deserialize<CoordinateSearchResult>(coordinateResultObj.GetRawText(), options);
                
            if (coordinateResult != null)
            {
                coordinateResultMap[property.Name] = coordinateResult;
            }
        }

        return coordinateResultMap;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, CoordinateSearchResult> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Serialization is not implemented for this converter.");
    }
}