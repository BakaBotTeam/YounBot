namespace YounBot.Data;

public record QueryPlace(int Id, string Name, string ShortNmae, ulong GroupId, List<short> Count, DateTime LastUpdated = default);