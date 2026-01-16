using FreezeTune.Models;

namespace FreezeTune.Repositories;

public interface IDatabaseRepository
{
    Daily GetForDay(string category, DateOnly date);
    void Upsert(Daily daily);
    DateOnly? AvailableUntil(string category);
}