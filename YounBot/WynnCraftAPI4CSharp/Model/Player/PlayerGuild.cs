namespace YounBot.WynnCraftAPI4CSharp.Model.Player;

public class PlayerGuild
{
    public string Name { get; set; }
    public string Prefix { get; set; }
    public string Rank { get; set; }
    public string RankStars { get; set; }

    public string GetName()
    {
        return this.Name;
    }

    public string GetPrefix()
    {
        return this.Prefix;
    }

    public string GetRank()
    {
        return this.Rank;
    }
    
    public string GetRankStars()
    {
        return this.RankStars;
    }

    public override string ToString()
    {
        return $"PlayerGuild{{name='{this.Name}', prefix='{this.Prefix}', rank='{this.Rank}', rankStars='{this.RankStars}'}}";
    }
}