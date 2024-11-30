using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YounBot.WynnCraftAPI4CSharp.Json;

public class ColorTypeAdapter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? colorCode = reader.GetString();
        if (colorCode == null)
            throw new JsonException("Color code cannot be null.");
            
        return ColorTranslator.FromHtml(colorCode);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        string colorCode = ColorTranslator.ToHtml(value);
        writer.WriteStringValue(colorCode);
    }
}