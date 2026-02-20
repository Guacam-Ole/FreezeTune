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

    // public Daily? GetForDay(string category, DateOnly date)
    // {
    //     using var db = new LiteDatabase(GetDbName(category));
    //     var dailies = db.GetCollection<Daily>();
    //     return dailies.FindOne(q => q.Date == date);
    // }

    public Daily GetForToday(string category)
    {
        var today = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);

        using var db = new LiteDatabase(GetDbName(category));
        var dailies = db.GetCollection<Daily>();
        var todaysQuiz = dailies.FindOne(q => q.Date == today);
        if (todaysQuiz != null) return todaysQuiz;
        var randomizer = new Random(today.DayNumber);
        var nextQuizId = randomizer.Next(dailies.Count());
        var oldQuiz = dailies.FindAll().ElementAt(nextQuizId);
        return oldQuiz;
    }

    public int CountForCategory(string category)
    {
        using var db = new LiteDatabase(GetDbName(category));
        var dailies = db.GetCollection<Daily>();
        return dailies.Count();
    }

    public DateOnly? LastTimeWeHad(string category, string interpret, string title)
    {
        using var db = new LiteDatabase(GetDbName(category));
        var dailies = db.GetCollection<Daily>();
        var match = dailies.FindAll().FirstOrDefault(q =>
        {
            var titleMatch = q.Title != null &&
                             q.Title.Equals(title, StringComparison.OrdinalIgnoreCase);
            if (!titleMatch) return false;

            if (string.IsNullOrEmpty(interpret)) return true;

            return q.Interpret != null &&
                   q.Interpret.Equals(interpret, StringComparison.OrdinalIgnoreCase);
        });
        return match?.Date;
    }

    public DateOnly? LastTimeWeHadUrl(string category, string url)
    {
        using var db = new LiteDatabase(GetDbName(category));
        var dailies = db.GetCollection<Daily>();
        var urlLower = NormalizeYouTubeUrl(url);
        var match = dailies.FindAll().FirstOrDefault(q =>
            q.Url != null &&
            NormalizeYouTubeUrl(q.Url) == urlLower);
        return match?.Date;
    }

    private static string NormalizeYouTubeUrl(string url)
    {
        url = url.Trim().ToLowerInvariant();
        var ampIndex = url.IndexOf('&');
        if (ampIndex > 0) url = url[..ampIndex];
        return url;
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

    public void AddStats(string category, int numberOfGuesses, bool success)
    {
        var today = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        using var db = new LiteDatabase(GetDbName(category));
        var allStats = db.GetCollection<Stats>();
        var todaysStats = allStats.FindOne(q => q.Date == today) ?? new Stats
        {
            Date = today
        };

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

    public List<Stats> GetQuarterStats(string category)
    {
        var today = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        using var db = new LiteDatabase(GetDbName(category));
        var allStats = db.GetCollection<Stats>();

        var thisQuarter = DateOnly.FromDateTime(DateTime.Today.AddMonths(-3));
        
        return allStats.Find(q => q.Date.DayNumber>=thisQuarter.DayNumber)
            .ToList();
    }
}