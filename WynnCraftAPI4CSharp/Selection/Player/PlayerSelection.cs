using System.Text.Json;
using System.Text.Json.Serialization;
using YounBot.WynnCraftAPI4CSharp.Http;
using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Implements;
using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Model;
using YounBot.WynnCraftAPI4CSharp.Model.Player;
using YounBot.WynnCraftAPI4CSharp.Utils;

namespace YounBot.WynnCraftAPI4CSharp.Selection.Player;

public class PlayerSelection : PlayerChoices
{
    public readonly Model.Player.Player? player;

    public PlayerSelection(Model.Player.Player? player, Dictionary<Guid, PlayerChoice>? choices) : base(choices)
    {
        this.player = player;
    }

    public Model.Player.Player? GetPlayer()
    {
        return player;
    }

    public static PlayerSelection FromResponse(WynnCraftHttpResponse response)
    {
        
        if (StatusCode.MultipleChoices.Is(response.StatusCode))
        {
            return new PlayerSelection(
                null,
                JsonSerializer.Deserialize<Dictionary<Guid, PlayerChoice>>(response.Body)
            );
        }
        else
        {
            return new PlayerSelection(
                JsonSerializer.Deserialize<Model.Player.Player>(response.Body, new JsonSerializerOptions { Converters = {  new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true) } }),
                null
            );
        }
    }

    public override string ToString()
    {
        return $"PlayerSelection{{player={this.player}}} {base.ToString()}";
    }
}