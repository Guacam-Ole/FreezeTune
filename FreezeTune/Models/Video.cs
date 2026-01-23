namespace FreezeTune.Models;

public class Video
{
    public string Url { get; set; }
    public List<int> ImageIds { get; set; }
    public string Interpret { get; set; }
    public string Title { get; set; }
    public DateOnly Date { get; set; }
    public string? Error { get; set; }
}