using System.Text.RegularExpressions;

namespace YounBot.WynnCraftAPI4CSharp.Model.Character;

public class CharacterArchetype
{
    private static readonly Regex StripColorPattern = new Regex("(?i)&[0-9A-FK-OR]");

    public string Name { get; set; }
    public int Difficulty { get; set; }
    public int Damage { get; set; }
    public int Defence { get; set; }
    public int Range { get; set; }
    public int Speed { get; set; }

    public string GetName()
    {
        return StripColorPattern.Replace(Name, "");
    }
    
    public override string ToString()
    {
        return $"CharacterArchetype{{" +
               $"name='{GetName()}', " +
               $"difficulty={Difficulty}, " +
               $"damage={Damage}, " +
               $"defence={Defence}, " +
               $"range={Range}, " +
               $"speed={Speed}" +
               $"}}";
    }
}