using FreezeTune.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

[Route("search")]
public class SearchController : ControllerBase
{
    private readonly IMusicSearchRepository _musicSearchRepository;
    private readonly ITvSearchRepository _tvSearchRepository;

    public SearchController(IMusicSearchRepository musicSearchRepository, ITvSearchRepository tvSearchRepository)
    {
        _musicSearchRepository = musicSearchRepository;
        _tvSearchRepository = tvSearchRepository;
    }

    [HttpGet("artist")]
    public async Task<List<string>> SearchArtist(string? country, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        return await _musicSearchRepository.SearchArtistName(country, input);
    }

    [HttpGet("album")]
    public async Task<List<string>> SearchAlbum(string artist, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        return await _musicSearchRepository.SearchAlbumName(artist, input);
    }

    [HttpGet("tv")]
    public async Task<List<string>> SearchTvSeries(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        return await _tvSearchRepository.SearchTvSeries(input);
    }

    [HttpGet("movie")]
    public async Task<List<string>> SearchMovie(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        return await _tvSearchRepository.SearchMovie(input);
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