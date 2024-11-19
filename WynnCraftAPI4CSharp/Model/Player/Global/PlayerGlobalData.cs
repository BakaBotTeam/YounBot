namespace YounBot.WynnCraftAPI4CSharp.Model.Player.Global;

public class PlayerGlobalData
{
    public int Wars { get; private set; }
    public int TotalLevel { get; private set; }
    public int KilledMobs { get; private set; }
    public int ChestsFound { get; private set; }
    public PlayerDungeons Dungeons { get; private set; }
    public PlayerRaids Raids { get; private set; }
    public int CompletedQuests { get; private set; }
    public PlayerPvP Pvp { get; private set; }

    public override string ToString()
    {
        return $"PlayerGlobalData{{wars={Wars}, totalLevel={TotalLevel}, killedMobs={KilledMobs}, chestsFound={ChestsFound}, dungeons={Dungeons}, raids={Raids}, completedQuests={CompletedQuests}, pvp={Pvp}}}";
    }
}