using System.Text.Json;
using YounBot.WynnCraftAPI4CSharp.Http;
using YounBot.WynnCraftAPI4CSharp.Model.Player.Character;
using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Implements;
using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Model;
using YounBot.WynnCraftAPI4CSharp.Utils;

namespace YounBot.WynnCraftAPI4CSharp.Selection.Player;

public class PlayerCharacterSelection : PlayerChoices
{
    private readonly PlayerCharacter? character;

    public PlayerCharacterSelection(PlayerCharacter? character, Dictionary<Guid, PlayerChoice>? choices)
        : base(choices)
    {
        this.character = character;
    }

    public PlayerCharacter GetCharacter()
    {
        return character;
    }

    public static PlayerCharacterSelection FromResponse(WynnCraftHttpResponse response)
    {
        if (StatusCode.MultipleChoices.Is(response.StatusCode))
        {
            return new PlayerCharacterSelection(
                null,
                JsonSerializer.Deserialize<Dictionary<Guid, PlayerChoice>>(response.Body, Utilities.Gson)
            );
        }
        else
        {
            return new PlayerCharacterSelection(
                JsonSerializer.Deserialize<PlayerCharacter>(response.Body, Utilities.Gson),
                null
            );
        }
    }

    public override string ToString()
    {
        return $"PlayerCharacterSelection{{character={this.character}}} " + base.ToString();
    }
}