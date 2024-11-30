using System.Text.Json;
using YounBot.WynnCraftAPI4CSharp.Http;
using YounBot.WynnCraftAPI4CSharp.Model.Player.Abilities;
using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Implements;
using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Model;
using YounBot.WynnCraftAPI4CSharp.Utils;

namespace YounBot.WynnCraftAPI4CSharp.Selection.Player;

public class PlayerAbilitiesSelection : PlayerChoices
{
    private readonly PlayerAbilities? abilities;

    public PlayerAbilitiesSelection(PlayerAbilities? abilities, Dictionary<Guid, PlayerChoice>? choices)
        : base(choices)
    {
        this.abilities = abilities;
    }

    public PlayerAbilities? GetAbilities()
    {
        return abilities;
    }

    public static PlayerAbilitiesSelection FromResponse(WynnCraftHttpResponse response)
    {
        if (StatusCode.MultipleChoices.Is(response.StatusCode))
        {
            return new PlayerAbilitiesSelection(
                null,
                JsonSerializer.Deserialize<Dictionary<Guid, PlayerChoice>>(response.Body, Utilities.Gson)
            );
        }
        else
        {
            return new PlayerAbilitiesSelection(
                JsonSerializer.Deserialize<PlayerAbilities>(response.Body, Utilities.Gson),
                null
            );
        }
    }

    public override string ToString()
    {
        return $"PlayerAbilitiesSelection{{abilities={this.abilities}}}";
    }
}