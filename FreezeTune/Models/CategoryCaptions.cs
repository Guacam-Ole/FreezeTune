namespace FreezeTune.Models;

public class CategoryCaptions
{
    public string? TitleCaption { get; set; }
    public string? ArtistCaption { get; set; }
    public bool HasArtist { get; set; } = true;
    public string? SubTitle { get; set; }
}
