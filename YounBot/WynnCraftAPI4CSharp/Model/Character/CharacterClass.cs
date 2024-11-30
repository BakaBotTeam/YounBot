namespace YounBot.WynnCraftAPI4CSharp.Model.Character;

public class CharacterClass
{
    public CharacterType Type { get; set; }
    public CharacterType DonorType { get; set; }
    public int OverallDifficulty { get; set; }

    public override string ToString()
    {
        return $"CharacterClass{{" +
               $"type={Type}, " +
               $"donorType={DonorType}, " +
               $"overallDifficulty={OverallDifficulty}" +
               $"}}";
    }
}