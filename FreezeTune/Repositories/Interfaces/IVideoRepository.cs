using FreezeTune.Models;

namespace FreezeTune.Repositories;

public interface IVideoRepository
{
    Task<Video> DownloadNFrames(string url, DateOnly date, string category, int numberOfFrames);
    void CopyImages(string category, DateOnly date, Dictionary<int, int> frames);
    string MoveVideoFile(string category, Video video);

    FileStream LoadVideoFromDisk(string filename);
}