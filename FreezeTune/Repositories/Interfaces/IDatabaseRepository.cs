using FreezeTune.Models;

namespace FreezeTune.Repositories;

public interface IDatabaseRepository
{
    //Daily GetForDay(string category, DateOnly date);
    Daily GetForToday(string category);
    DateOnly? LastTimeWeHad(string category, string interpret, string title);
    void Upsert(Daily daily);
    DateOnly? AvailableUntil(string category);
    void AddStats(string category, int numberOfGuesses, bool success);
    List<Stats> GetMonthlyStats(string category);

}