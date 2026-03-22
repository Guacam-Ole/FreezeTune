namespace FreezeTune.Models;

public class Category
{
    public string Name { get; set; } = "";
    public string? Password { get; set; }
    public string? ArtistCaption { get; set; }
    public string? TitleCaption { get; set; }
    public bool ShowHints { get; set; } = true;
    public bool HasArtist { get; set; } = true;
    public string? Header { get; set; }
    public string? SubTitle { get; set; }
    public SearchMode SearchMode { get; set; } = SearchMode.None;
    public string? CountryFilter { get; set; }
    public string? Artist { get; set; }
}