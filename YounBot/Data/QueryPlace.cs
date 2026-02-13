namespace YounBot.Data;

public record QueryPlace(int Id, string Name, string ShortNmae, ulong GroupId, List<int> Count, DateTime LastUpdated = default);