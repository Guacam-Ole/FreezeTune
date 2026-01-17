namespace FreezeTune.Models;

public class Stats
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public Dictionary<int, int> GuessToSuccess { get; set; } = new();
    public int Failures { get; set; }
    public int Successes { get; set; }
}