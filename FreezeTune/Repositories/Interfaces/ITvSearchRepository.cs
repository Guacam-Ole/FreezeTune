using TidalUSDK.Responses;

namespace FreezeTune.Repositories;

public interface ITvSearchRepository
{
    Task<List<string>> SearchTvSeries(string prefix);
    Task<List<string>> SearchMovie(string prefix);
}