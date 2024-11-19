namespace YounBot.WynnCraftAPI4CSharp.Model.Search;

public class PlayerSearchResult
{
    private Guid uuid;
    private string username;

    public Guid GetUuid()
    {
        return this.uuid;
    }

    public string GetUsername()
    {
        return this.username;
    }

    public override string ToString()
    {
        return $"PlayerSearchResult{{uuid={this.uuid}, username='{this.username}'}}";
    }
}