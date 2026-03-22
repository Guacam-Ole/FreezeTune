using Flurl.Util;

namespace FreezeTune.Repositories;

public class MusicSearchRepository : IMusicSearchRepository
{
    public async Task<List<string>> SearchArtist(string? countryFilter, string prefix)
    {
        var query = new MetaBrainz.MusicBrainz.Query();
        var matches = await query.FindArtistsAsync(prefix, simple: true, limit:100);
        return matches.Results
            .Where(q => countryFilter == null || q.Item.Country != null &&
                q.Item.Country.Equals(countryFilter, StringComparison.CurrentCultureIgnoreCase))
            .Select(q => q.Item.Name).Distinct().Take(10).ToList();
    }

    public async Task<List<string>> SearchTitleForArtist(string? artist, string prefix)
    {
        var query = new MetaBrainz.MusicBrainz.Query();
        var matches = await query.FindRecordingsAsync(prefix, simple: true, limit:100);
        if (artist == null) return  matches.Results.Select(q => q.Item.Title).Distinct().Take(10).ToList();
        
        var artistMatches = matches.Results.Where(q => q.Item.ArtistCredit.Any(at =>
            at.Name != null && at.Name.Equals(artist, StringComparison.CurrentCultureIgnoreCase)));
        return artistMatches.Select(q => q.Item.Title).Distinct().Take(10). ToList();
    }

    public async Task<List<string>> SearchAlbum(string artist, string prefix)
    {
        var query = new MetaBrainz.MusicBrainz.Query();
        var matches = await query.FindReleasesAsync(prefix, simple: true, limit: 100);
        return matches.Results.Where(q =>
                q.Item.ArtistCredit.Any(q =>
                    q.Name != null && q.Name.Equals(artist, StringComparison.CurrentCultureIgnoreCase)))
            .Select(q => q.Item.Title).Distinct().Take(10). ToList();
    }

    public async Task<List<string>> SearchTitleForAlbum(string artist, string? album, string prefix)
    {
        var query = new MetaBrainz.MusicBrainz.Query();
        var matches = await query.FindRecordingsAsync(prefix, simple: true, limit:100);
        var artitsMatches = matches.Results.Where(q =>
            q.Item.ArtistCredit.Any(art =>
                art.Name != null && art.Name.Equals(artist, StringComparison.CurrentCultureIgnoreCase)));
        if (album == null) return artitsMatches.Select(q => q.Item.Title).ToList();

        return artitsMatches
            .Where(q => q.Item.Releases.Any(q => q.Title.Equals(album, StringComparison.CurrentCultureIgnoreCase)))
            .Select(q => q.Item.Title).Distinct().Take(10).ToList();
    }
}