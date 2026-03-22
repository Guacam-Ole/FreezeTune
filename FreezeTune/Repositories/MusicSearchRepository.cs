using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;

namespace FreezeTune.Repositories;

public class MusicSearchRepository : IMusicSearchRepository
{
    public async Task<List<ISearchResult<IArtist>>> SearchArtist(string? countryFilter, string artist)
    {
        var query = new MetaBrainz.MusicBrainz.Query();

        var matches = await query.FindArtistsAsync(artist, simple: true, limit: 100);
        return matches.Results
            .Where(q => countryFilter == null || q.Item.Country != null &&
                q.Item.Country.Equals(countryFilter, StringComparison.CurrentCultureIgnoreCase))
            .Where(q => q.Item.Name.Contains(artist, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
    }

    public async Task<List<ISearchResult<IRelease>>> SearchAlbum(string artist, string album)
    {
        var query = new MetaBrainz.MusicBrainz.Query();
        var matches = await query.FindReleasesAsync(album + " AND artist:" + artist, simple: false, limit: 100);
        return matches.Results.Where(q =>
            q.Item.Title.Contains(album, StringComparison.CurrentCultureIgnoreCase) &&
            q.Item.ArtistCredit.Any(ac =>
                ac.Name != null && ac.Name.Equals(artist, StringComparison.CurrentCultureIgnoreCase))).ToList();
    }


    public async Task<List<string>> SearchArtistName(string? countryFilter, string prefix)
    {
        var artists = await SearchArtist(countryFilter, prefix);
        return artists.Select(q => q.Item.Name).Distinct().Take(25).ToList();
    }

    public async Task<List<string>> SearchTitleForArtist(string? artist, string prefix)
    {
        var query = new MetaBrainz.MusicBrainz.Query();

        var matches = artist == null
            ? await query.FindRecordingsAsync(prefix, simple: true, limit: 100)
            : await query.FindRecordingsAsync(prefix + " AND artist:" + artist, simple: false, limit: 100);


        return matches.Results.Select(q => q.Item.Title)
            .Where(q => q.Contains(prefix, StringComparison.CurrentCultureIgnoreCase)).Distinct().Take(25).ToList();
    }

    public async Task<List<string>> SearchAlbumName(string artist, string prefix)
    {
        var albums = await SearchAlbum(artist, prefix);
        return albums.Select(q => q.Item.Title).Distinct().Take(25).ToList();
    }

    public async Task<List<string>> SearchTitleForAlbum(string artist, string? album, string prefix)
    {
        var query = new MetaBrainz.MusicBrainz.Query();

        var matches = album == null
            ? await query.FindRecordingsAsync(prefix + " AND artist:" + artist, simple: false, limit: 100)
            : await query.FindRecordingsAsync(prefix + " AND artist:" + artist + " AND release:" + album, simple: false,
                limit: 100);


        return matches.Results.Select(q => q.Item.Title)
            .Where(q => q.Contains(prefix, StringComparison.CurrentCultureIgnoreCase)).Distinct().Take(25).ToList();
    }
}