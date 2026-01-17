using FreezeTune.Models;

namespace FreezeTune.Repositories;

public interface IDatabaseRepository
{
    Daily GetForDay(string category, DateOnly date);
    void Upsert(Daily daily);
    DateOnly? AvailableUntil(string category);
    void AddStats(string category, int numberOfGuesses, bool success);
    List<Stats> GetMonthlyStats(string category);

}