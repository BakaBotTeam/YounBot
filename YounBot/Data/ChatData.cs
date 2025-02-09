using System.Text.Json.Nodes;

namespace YounBot.Data;

public class ChatData
{
    public DateTimeOffset Time { get; set; }
    public JsonArray Data { get; set; }
    
    public ChatData(DateTimeOffset time, JsonArray data)
    {
        Time = time;
        Data = data;
    }
}