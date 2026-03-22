using FreezeTune.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

[Route("search")]
public class SearchController : ControllerBase
{
    private readonly IMusicSearchRepository _musicSearchRepository;

    public SearchController(IMusicSearchRepository musicSearchRepository)
    {
        _musicSearchRepository = musicSearchRepository;
    }

    [HttpGet("artist")]
    public async Task<List<string>> SearchArtist(string? country, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        return await _musicSearchRepository.SearchArtist(country, input);
    }

    [HttpGet("album")]
    public async Task<List<string>> SearchAlbum(string artist, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        return await _musicSearchRepository.SearchAlbum(artist, input);
    }


    [HttpGet("albumtitle")]
    public async Task<List<string>> SearchAlbumTitle(string artist, string? album, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        return await _musicSearchRepository.SearchTitleForAlbum(artist, album, input);
    }
    
    [HttpGet("artisttitle")]
    public async Task<List<string>> SearchArtistTitle(string artist, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        return await _musicSearchRepository.SearchTitleForArtist(artist, input);
    }
}