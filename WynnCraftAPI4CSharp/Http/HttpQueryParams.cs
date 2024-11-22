using System.Text;

namespace YounBot.WynnCraftAPI4CSharp.Http;

public class HttpQueryParams
{
    private readonly Dictionary<string, object?> _params = new();

    private HttpQueryParams()
    {
    }

    public HttpQueryParams Add(string key)
    {
        return Add(key, null);
    }

    public HttpQueryParams Add(string key, object? value)
    {
        _params[key] = value;
        return this;
    }

    public string GetAsQueryString(string @base)
    {
        var url = new StringBuilder(@base);
        var startedQuery = false;

        foreach (var entry in _params)
        {
            if (!startedQuery)
            {
                startedQuery = true;
                url.Append("?");
            }
            else
            {
                url.Append("&");
            }

            url.Append(entry.Key);

            if (entry.Value != null) url.Append("=").Append(entry.Value);
        }

        return url.ToString();
    }

    public static HttpQueryParams Create()
    {
        return new HttpQueryParams();
    }
}