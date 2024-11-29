using System.Text.Json;
using System.Text.Json.Serialization;

namespace YounBot.WynnCraftAPI4CSharp.Json;

public class GUidTypeAdapter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? uuidString = reader.GetString();
        if (uuidString == null)
            throw new JsonException("GUid value cannot be null.");
            
        return Guid.Parse(uuidString);
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}