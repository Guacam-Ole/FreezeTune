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
        var existing = dailies.FindOne(q => q.Date == daily.Date);
        if (existing != null) dailies.Delete(existing.Id);
        dailies.Upsert(daily);
    }

    public DateOnly? AvailableUntil(string category)
    {
        using var db = new LiteDatabase(GetDbName(category));
        var dailies = db.GetCollection<Daily>();
        if (dailies.Count() == 0) return null;
        return DateOnly.FromDayNumber(dailies.Max(q => q.Date.DayNumber));
    }

    public void AddStats(string category,  int numberOfGuesses, bool success)
    {
        var today = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        using var db = new LiteDatabase(GetDbName(category));
        var allStats = db.GetCollection<Stats>();
        var todaysStats = allStats.FindOne(q => q.Date == today);
        if (todaysStats == null)
        {
            todaysStats = new Stats
            {
                Date = today
            };
        }

        if (success)
        {
            todaysStats.GuessToSuccess.TryAdd(numberOfGuesses, 0);

            todaysStats.GuessToSuccess[numberOfGuesses]++;
            todaysStats.Successes++;
        }
        else
        {
            todaysStats.Failures++;
        }

        allStats.Upsert(todaysStats);
    }

    public List<Stats> GetMonthlyStats(string category)
    {
        var today = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        using var db = new LiteDatabase(GetDbName(category));
        var allStats = db.GetCollection<Stats>();

        return allStats.Find(q => q.Date.Year == today.Year && q.Date.Month == today.Month).OrderBy(q => q.Date.Day)
            .ToList();
    }
}