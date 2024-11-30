namespace YounBot.WynnCraftAPI4CSharp.Model.Player.Abilities;

public class PlayerAbilities
{
    public int Pages { get; set; }
    public PlayerAbility[] Map { get; set; }

    public override string ToString()
    {
        return $"PlayerAbilities{{" +
               $"pages={Pages}, " +
               $"map={Map.Select(ability => ability.ToString()).Aggregate((current, next) => current + ", " + next)}" +
               $"}}";
    }
}