namespace YounBot.WynnCraftAPI4CSharp.Selection.Choice.Model;

public class GuildChoice
{
    public string Name { get; }
    public string Prefix { get; }
    public int Level { get; }
    public int Members { get; }
    public DateTime Created { get; }

    public string GetName()
    {
        return Name;
    }

    public string GetPrefix()
    {
        return Prefix;
    }

    public int GetLevel()
    {
        return Level;
    }

    public int GetMembers()
    {
        return Members;
    }

    public DateTime GetCreated()
    {
        return Created;
    }

    public override string ToString()
    {
        return $"GuildChoice{{Name='{Name}', Prefix='{Prefix}', Level={Level}, Members={Members}, Created={Created}}}";
    }
}