namespace YounBot.WynnCraftAPI4CSharp.Model.Player.Abilities;

public class PlayerAbility
{
    public string Type { get; set; }
    public Coordinates Location { get; set; }
    public Dictionary<string, object> Meta { get; set; }
    public string[] Family { get; set; }

    public override string ToString()
    {
        return $"PlayerAbility{{" +
               $"type='{Type}', " +
               $"coordinates={Location}, " +
               $"meta={string.Join(", ", Meta ?? new Dictionary<string, object>())}, " +
               $"family={string.Join(", ", Family ?? Array.Empty<string>())}" +
               $"}}";
    }

    public class Coordinates
    {
        public int X { get; set; }
        public int Y { get; set; }

        public override string ToString()
        {
            return $"Coordinates{{x={X}, y={Y}}}";
        }
    }
}