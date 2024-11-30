namespace YounBot.Command;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute(string primaryName, string description) : Attribute
{
    public readonly string PrimaryName = primaryName;
    public readonly string Description = description;
}