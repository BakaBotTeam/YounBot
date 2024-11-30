namespace YounBot.WynnCraftAPI4CSharp.Model.Character;

public class CharacterEntry
{
    public string Type { get; set; }
    public string Nickname { get; set; }
    public int Level { get; set; }
    public long Xp { get; set; }
    public int XpPercent { get; set; }
    public int TotalLevel { get; set; }
    public string[] Gamemode { get; set; }
    public Dictionary<string, object> Meta { get; set; }

    public override string ToString()
    {
        return $"CharacterEntry{{" +
               $"gamemode={string.Join(", ", Gamemode ?? Array.Empty<string>())}, " +
               $"type='{Type}', " +
               $"nickname='{Nickname}', " +
               $"level={Level}, " +
               $"xp={Xp}, " +
               $"xpPercent={XpPercent}, " +
               $"totalLevel={TotalLevel}, " +
               $"meta={string.Join(", ", Meta ?? new())}" +
               $"}}";
    }
}