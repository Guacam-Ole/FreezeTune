using Flurl.Util;
using TvDbSharper;

namespace FreezeTune.Repositories;

public class TvSearchRepository:ITvSearchRepository
{
    private readonly Secrets _secrets;

    public TvSearchRepository(Secrets secrets)
    {
        _secrets = secrets;
    }


    private async Task<List<string>> Search(string searchtype, string prefix)
    {
        var client = new TvDbClient();
        await client.Login(_secrets.TvApiKey, string.Empty);

        var searchResult = await client.Search(new SearchOptionalParams { Query = prefix });
        if (!searchResult.Status.Equals("success")) return new List<string>();
        List<string> matches = (from match in searchResult.Data.Where(q => q.Type == searchtype) from translation in match.Translations where translation.Value.Contains(prefix, StringComparison.CurrentCultureIgnoreCase) select translation.Value).ToList();

        return matches.Distinct().ToList();
    }

    public async Task<List<string>> SearchTvSeries(string prefix)
    {
        return await Search("series", prefix);
    }

    public async Task<List<string>> SearchMovie(string prefix)
    {
        return await Search("movie", prefix);
    }
}