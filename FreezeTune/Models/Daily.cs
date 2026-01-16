namespace FreezeTune.Models;

public class Daily
{
    public string Category { get; set; } // "80s" etc.
    public DateOnly Date { get; set; }
    public string Url { get; set; }
    public string Interpret { get; set; }
    public string Title { get; set; }
}