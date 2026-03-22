
namespace FreezeTune.Repositories;

public interface IMusicSearchRepository
{
    Task<List<string>> SearchArtistName(string? country,  string prefix);
    Task<List<string>> SearchTitleForArtist(string? artist, string prefix);
    Task<List<string>> SearchAlbumName(string artist, string prefix);
    Task<List<string>> SearchTitleForAlbum(string artist, string? album, string prefix);
}