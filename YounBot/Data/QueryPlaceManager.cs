using LiteDB;

namespace YounBot.Data;

public class QueryPlaceManager
{
    private readonly ILiteCollection<QueryPlace> _collection;

    public QueryPlaceManager(LiteDatabase db)
    {
        _collection = db.GetCollection<QueryPlace>("query_places");
        _collection.EnsureIndex(x => x.Id, true);
    }

    public List<short>? GetCountByName(string name, ulong groupId)
    {
        QueryPlace? place = _collection.FindOne(p => p.Name == name && p.GroupId == groupId);
        if (place == null) return null;
        if (place.LastUpdated.Date == DateTime.Now.Date) return place?.Count;
        place = place with { Count = [], LastUpdated = DateTime.Now };
        _collection.Update(place);
        return place?.Count;
    }

    public List<short>? GetCountByShortName(string shortName, ulong groupId)
    {
        QueryPlace? place = _collection.FindOne(p => p.ShortNmae == shortName && p.GroupId == groupId);
        if (place == null) return null;
        if (place.LastUpdated.Date == DateTime.Now.Date) return place?.Count;
        place = place with { Count = [], LastUpdated = DateTime.Now };
        _collection.Update(place);
        return place?.Count;
    }

    public bool AddQueryPlace(string name, string shortName, ulong groupId)
    {
        if (_collection.Exists(p => (p.Name == name || p.ShortNmae == shortName) && p.GroupId == groupId))
            return false;
        int newId = _collection.Count() == 0 ? 1 : _collection.FindAll().Max(p => p.Id) + 1;
        QueryPlace newPlace = new(newId, name, shortName, groupId, []);
        _collection.Insert(newPlace);
        return true;
    }

    public bool RemoveQueryPlace(string nameOrShortName, ulong groupId)
    {
        QueryPlace? place = _collection.FindOne(p => (p.Name == nameOrShortName || p.ShortNmae == nameOrShortName) && p.GroupId == groupId);
        if (place == null) return false;
        _collection.Delete(place.Id);
        return true;
    }

    public bool UpdateCount(string nameOrShortName, ulong groupId, List<short> newCount)
    {
        QueryPlace? place = _collection.FindOne(p => (p.Name == nameOrShortName || p.ShortNmae == nameOrShortName) && p.GroupId == groupId);
        if (place == null) return false;
        place = place with { Count = newCount };
        _collection.Update(place);
        return true;
    }

    public QueryPlace? GetPlace(string nameOrShortName, ulong groupId)
    {
        QueryPlace? place = _collection.FindOne(p => (p.Name == nameOrShortName || p.ShortNmae == nameOrShortName) && p.GroupId == groupId);
        if (place == null) return null;
        if (place.LastUpdated.Date == DateTime.Now.Date) return place;
        place = place with { Count = [], LastUpdated = DateTime.Now };
        _collection.Update(place);
        return place;
    }

    public List<QueryPlace> GetAllPlaces(ulong groupId)
    {
        List<QueryPlace> places = _collection.Find(p => p.GroupId == groupId).ToList();
        foreach (QueryPlace updatedPlace in from place in places where place.LastUpdated.Date != DateTime.Now.Date select place with { Count = [], LastUpdated = DateTime.Now })
        {
            _collection.Update(updatedPlace);
        }
        return _collection.Find(p => p.GroupId == groupId).ToList();
    }
}
