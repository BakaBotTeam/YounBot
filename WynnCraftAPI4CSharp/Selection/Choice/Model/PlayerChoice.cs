using YounBot.WynnCraftAPI4CSharp.Model.Player;

namespace YounBot.WynnCraftAPI4CSharp.Selection.Choice.Model;

public class PlayerChoice
{
    public string StoredName { get; }
    public string Rank { get; }
    public bool Veteran { get; }
    public string RankBadge { get; }
    public string SupportRank { get; }
    public string ShortenedRank { get; }
    public PlayerLegacyRankColour LegacyRankColour { get; }

    public string GetStoredName()
    {
        return StoredName;
    }

    public string GetRank()
    {
        return Rank;
    }

    public bool IsVeteran()
    {
        return Veteran;
    }

    public string GetRankBadge()
    {
        return RankBadge;
    }

    public string GetSupportRank()
    {
        return SupportRank;
    }

    public string GetShortenedRank()
    {
        return ShortenedRank;
    }

    public PlayerLegacyRankColour GetLegacyRankColour()
    {
        return LegacyRankColour;
    }

    public PlayerRank GetPlayerRank()
    {
        return PlayerRankExtension.FromString(
            Rank.Equals("Player", StringComparison.OrdinalIgnoreCase) && SupportRank != null ? SupportRank : Rank);
    }

    public override string ToString()
    {
        return
            $"PlayerChoice{{StoredName='{StoredName}', Rank='{Rank}', Veteran={Veteran}, RankBadge='{RankBadge}', SupportRank='{SupportRank}', ShortenedRank='{ShortenedRank}', LegacyRankColour={LegacyRankColour}}}";
    }
}