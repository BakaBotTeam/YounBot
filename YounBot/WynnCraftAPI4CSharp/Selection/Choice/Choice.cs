namespace YounBot.WynnCraftAPI4CSharp.Selection.Choice;

public class Choice<K, V>
{
    private readonly Dictionary<K, V> choices;

    public Choice(Dictionary<K, V> choices)
    {
        this.choices = choices;
    }

    public Dictionary<K, V> GetChoices()
    {
        return this.choices;
    }

    public bool HasChoice()
    {
        return this.choices != null && this.choices.Count > 0;
    }

    public override string ToString()
    {
        return $"Choice{{choices={this.choices}}}";
    }
}