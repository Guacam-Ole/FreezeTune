namespace FreezeTune.Models;

public class Song
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; }
    public string Artist { get; set; }
    public int? Year { get; set; }
    public string? Decade { get; set; }
}