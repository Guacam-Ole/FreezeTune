namespace FreezeTune.Logic;

public interface IMaintenanceLogic
{
    Models.Video Init(string category);
    void Add(string category, Models.Video video);
    Task<Models.Video> Download(string category, Models.Video video, string? sessionId = null);
    Dictionary<int, string> GetTmpImages(string category, Models.Video video);
}