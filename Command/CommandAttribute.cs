namespace YounBot.Command;

using System;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string PrimaryName;
    public string Description;

    public CommandAttribute(string primaryName, string description)
    {
        PrimaryName = primaryName;
        Description = description;
    }
}