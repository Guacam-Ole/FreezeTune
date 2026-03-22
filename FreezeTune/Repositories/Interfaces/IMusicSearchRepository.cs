namespace FreezeTune.Repositories;

public interface IMusicSearchRepository
{
    Task<List<string>> SearchArtist(string? country,  string prefix);
    Task<List<string>> SearchTitleForArtist(string? artist, string prefix);
    Task<List<string>> SearchAlbum(string artist, string prefix);
    Task<List<string>> SearchTitleForAlbum(string artist, string? album, string prefix);
}