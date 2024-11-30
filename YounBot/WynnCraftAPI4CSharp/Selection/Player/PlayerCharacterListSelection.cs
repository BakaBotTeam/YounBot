using System.Text.Json;
using YounBot.WynnCraftAPI4CSharp.Http;
using YounBot.WynnCraftAPI4CSharp.Model.Character;
using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Implements;
using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Model;
using YounBot.WynnCraftAPI4CSharp.Utils;

namespace YounBot.WynnCraftAPI4CSharp.Selection.Player;

public class PlayerCharacterListSelection : PlayerChoices
{
    private readonly Dictionary<Guid, CharacterEntry>? characters;

    public PlayerCharacterListSelection(Dictionary<Guid, CharacterEntry>? characters, Dictionary<Guid, PlayerChoice>? choices)
        : base(choices)
    {
        this.characters = characters;
    }

    public Dictionary<Guid, CharacterEntry>? GetCharacters()
    {
        return characters;
    }

    public static PlayerCharacterListSelection FromResponse(WynnCraftHttpResponse response)
    {
        if (StatusCode.MultipleChoices.Is(response.StatusCode))
        {
            return new PlayerCharacterListSelection(
                null,
                JsonSerializer.Deserialize<Dictionary<Guid, PlayerChoice>>(response.Body, Utilities.Gson)
            );
        }
        else
        {
            return new PlayerCharacterListSelection( 
                JsonSerializer.Deserialize<Dictionary<Guid, CharacterEntry>>(response.Body, Utilities.Gson),
                null
            );
        }
    }

    public override string ToString()
    {
        return $"PlayerCharacterListSelection{{characters={this.characters}}} {base.ToString()}";
    }
}