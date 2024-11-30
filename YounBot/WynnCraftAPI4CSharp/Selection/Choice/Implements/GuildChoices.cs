using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Model;

namespace YounBot.WynnCraftAPI4CSharp.Selection.Choice.Implements;

public class GuildChoices : Choice<Guid, GuildChoice>
{
    public static readonly Type GuildMapType = typeof(Dictionary<Guid, GuildChoice>);

    public GuildChoices(Dictionary<Guid, GuildChoice> choices) : base(choices)
    {
    }
}