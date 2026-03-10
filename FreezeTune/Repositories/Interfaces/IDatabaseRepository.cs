using FreezeTune.Models;

namespace FreezeTune.Repositories;

public interface IDatabaseRepository
{
    //Daily GetForDay(string category, DateOnly date);
    Daily GetForToday(string category);
    DateOnly? LastTimeWeHad(string category, string interpret, string title);
    DateOnly? LastTimeWeHadUrl(string category, string url);
    void Upsert(Daily daily);
    DateOnly? AvailableUntil(string category);
    void AddStats(string category, int numberOfGuesses, bool success);
    List<Stats> GetQuarterStats(string category);
    int CountForCategory(string category);

    List<Daily> GetALlForCategory(string category);
}