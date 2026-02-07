namespace FreezeTune.Models;

public class Category
{
    public string Name { get; set; } = "";
    public string? Password { get; set; }
    public string? ArtistCaption { get; set; }
    public string? TitleCaption { get; set; }
    public bool ShowHints { get; set; } = true;
}