namespace FreezeTune.Repositories;

public interface IYoutubeRepository
{
    Task DownloadSingleFrames(string youtubeUrl, DateOnly date, string category, params TimeSpan[] positions);
    List<string> GetImagesFor(DateOnly date, string category);
}