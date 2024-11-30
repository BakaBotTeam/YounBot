using System.Text.Json;
using YounBot.WynnCraftAPI4CSharp.Json;

namespace YounBot.WynnCraftAPI4CSharp.Utils;

public class Utilities
{
    public static readonly JsonSerializerOptions Gson = new()
    {
        Converters =
        {
            new CoordinateSearchResultMapAdapter(),
            new PlayerSearchResultMapAdapter(),
            // new GuildSearchResultMapAdapter(),
            // new GuildMemberMapAdapter(),
            new CharacterClassAdapter(),
            new ColorTypeAdapter(),
            new GUidTypeAdapter()
            // new InstantTypeAdapter()
        }
    };
}