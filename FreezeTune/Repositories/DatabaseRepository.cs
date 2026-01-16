using FreezeTune.Models;
using LiteDB;

namespace FreezeTune.Repositories;

public class DatabaseRepository : IDatabaseRepository
{
    private readonly Config _config;

    public DatabaseRepository(Config config)
    {
        _config = config;
    }
    
    private string GetDbName(string category)
    {
        return $"{_config.BasePath}/db/dailies_{category}.db";
    }

    public Daily? GetForDay(string category, DateOnly date)
    {
        using var db = new LiteDatabase(GetDbName(category));
        var dailies = db.GetCollection<Daily>();
        return dailies.FindOne(q => q.Date == date);
    }

    public void Upsert(Daily daily)
    {
        using var db = new LiteDatabase(GetDbName(daily.Category));
        var dailies = db.GetCollection<Daily>();
        dailies.Upsert(daily);
    }

    public DateOnly? AvailableUntil(string category)
    {
        using var db = new LiteDatabase(GetDbName(category));
        var dailies = db.GetCollection<Daily>();
        if (dailies.Count() == 0) return null;
        return DateOnly.FromDayNumber(dailies.Max(q => q.Date.DayNumber));
    }
}