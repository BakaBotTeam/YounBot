namespace YounBot.WynnCraftAPI4CSharp.Model.Search;

public class CoordinateSearchResult
{
    private string name;
    private int[] start;
    private int[] end;

    public string GetName()
    {
        return this.name;
    }

    public int GetStartX()
    {
        return this.start[0];
    }

    public int GetStartZ()
    {
        return this.start[1];
    }

    public int GetEndX()
    {
        return this.end[0];
    }

    public int GetEndZ()
    {
        return this.end[1];
    }

    public override string ToString()
    {
        return $"CoordinateSearchResult{{name='{this.name}', start=[{string.Join(", ", this.start)}], end=[{string.Join(", ", this.end)}]}}";
    }
}