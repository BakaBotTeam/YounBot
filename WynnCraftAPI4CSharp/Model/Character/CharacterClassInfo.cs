namespace YounBot.WynnCraftAPI4CSharp.Model.Character;

public class CharacterClassInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Lore { get; set; }
    public int OverallDifficulty { get; set; }
    public Dictionary<string, CharacterArchetype> Archetypes { get; set; }

    public override string ToString()
    {
        return $"CharacterClassInfo{{" +
               $"id='{Id}', " +
               $"name='{Name}', " +
               $"lore='{Lore}', " +
               $"overallDifficulty={OverallDifficulty}, " +
               $"archetypes={string.Join(", ", Archetypes ?? new())}" +
               $"}}";
    }
}