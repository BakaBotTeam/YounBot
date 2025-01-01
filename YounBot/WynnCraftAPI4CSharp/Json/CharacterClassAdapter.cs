using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using YounBot.WynnCraftAPI4CSharp.Model.Character;

namespace YounBot.WynnCraftAPI4CSharp.Json;

public class CharacterClassAdapter : JsonConverter<CharacterClass>
{
    private static readonly Regex Pattern = new(@"(.+)\s\((.+)\)", RegexOptions.Compiled);

    public override CharacterClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
        JsonElement obj = jsonDocument.RootElement;
            
        string? name = obj.GetProperty("name").GetString();
        Match matcher = Pattern.Match(name);

        if (!matcher.Success)
            throw new JsonException($"Invalid format for CharacterClass: {name}");

        JsonObject jsonObject = new()
        {
            { "type", ToEnum(matcher.Groups[1].Value) },
            { "donorType", ToEnum(matcher.Groups[2].Value) }
        };

        string serialized = JsonSerializer.Serialize(jsonObject);
        return JsonSerializer.Deserialize<CharacterClass>(serialized, options)!;
    }

    public override void Write(Utf8JsonWriter writer, CharacterClass value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Serialization is not implemented for this converter.");
    }

    private static string ToEnum(string input)
    {
        return input.Replace(" ", "_");
    }
}