using FreezeTune.Models;

namespace FreezeTune.Repositories;

public interface IYoutubeRepository
{
    Task<Video> DownloadNFrames(string youtubeUrl, DateOnly date, string category, int numberOfFrames);
    void MoveImages(string category, DateOnly date, Dictionary<int, int> frames);
}