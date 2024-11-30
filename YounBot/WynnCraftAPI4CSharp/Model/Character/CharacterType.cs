namespace YounBot.WynnCraftAPI4CSharp.Model.Character;

public enum CharacterType
{
    UNKNOWN,
    ARCHER,
    HUNTER,
    WARRIOR,
    KNIGHT,
    ASSASSIN,
    NINJA,
    MAGE,
    DARKWIZARD,
    SHAMAN,
    SKYSEER
}

public static class CharacterTypeExtensions
{
    public static string GetName(this CharacterType characterType)
    {
        return characterType switch
        {
            CharacterType.UNKNOWN => "Unknown",
            CharacterType.ARCHER => "Archer",
            CharacterType.HUNTER => "Hunter",
            CharacterType.WARRIOR => "Warrior",
            CharacterType.KNIGHT => "Knight",
            CharacterType.ASSASSIN => "Assassin",
            CharacterType.NINJA => "Ninja",
            CharacterType.MAGE => "Mage",
            CharacterType.DARKWIZARD => "Dark Wizard",
            CharacterType.SHAMAN => "Shaman",
            CharacterType.SKYSEER => "Skyseer",
            _ => throw new ArgumentOutOfRangeException(nameof(characterType), characterType, null)
        };
    }
    
    public static CharacterType GetNonDonorType(this CharacterType characterType) {
        if (characterType == CharacterType.UNKNOWN) {
            throw new ArgumentException("Cannot convert unknown character type to a non-donor type");
        }

        switch (characterType)
        {
            case CharacterType.HUNTER: return CharacterType.ARCHER;
            case CharacterType.KNIGHT: return CharacterType.WARRIOR;
            case CharacterType.NINJA: return CharacterType.ASSASSIN;
            case CharacterType.DARKWIZARD: return CharacterType.MAGE;
            case CharacterType.SKYSEER: return CharacterType.SHAMAN;
            default: return characterType;
        }
    }
    
    public static CharacterType FromName(String name) {
        foreach (CharacterType characterType in Enum.GetValues(typeof(CharacterType)))
        {
            if (characterType.GetName().Equals(name))
            {
                return characterType;
            }
        }

        return CharacterType.UNKNOWN;
    }
}