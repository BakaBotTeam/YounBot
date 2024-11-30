using YounBot.WynnCraftAPI4CSharp.Selection.Choice.Model;

namespace YounBot.WynnCraftAPI4CSharp.Selection.Choice.Implements;
using System;
using System.Collections.Generic;
public class PlayerChoices : Choice<Guid, PlayerChoice>
{
    public static readonly Type PlayerMapType = typeof(Dictionary<Guid, PlayerChoice>);

    public PlayerChoices(Dictionary<Guid, PlayerChoice> choices) : base(choices)
    {
    }
}