namespace YounBot.WynnCraftAPI4CSharp.Model.Player;

public enum PlayerRank
{
    UNKNOWN,
    PLAYER,
    VIP,
    VIPPLUS,
    HERO,
    CHAMPION,
    MEDIA,
    MODERATOR,
    ADMINISTRATOR,
    WEBDEV,
    GM,
    BUILD,
    CMD,
    HYBRID,
    ITEM,
    QA,
    ART
}

public class PlayerRankExtension
{
    public static PlayerRank FromString(string name)
    {
        foreach (PlayerRank rank in Enum.GetValues(typeof(PlayerRank)))
        {
            if (rank.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return rank;
            }
        }

        return PlayerRank.UNKNOWN;
    }
}