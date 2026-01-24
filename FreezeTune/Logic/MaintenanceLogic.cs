using FreezeTune.Models;
using FreezeTune.Repositories;

namespace FreezeTune.Logic;



public class MaintenanceLogic:IMaintenanceLogic
{
    private const int NumberOfFrames = 80;
    private readonly IDatabaseRepository _dbRepository;
    private readonly IVideoRepository _ytRepository;
    private readonly IImageRepository _imageRepositor;

    public MaintenanceLogic(IDatabaseRepository dbRepository, IVideoRepository ytRepository, IImageRepository imageRepositor)
    {
        _dbRepository = dbRepository;
        _ytRepository = ytRepository;
        _imageRepositor = imageRepositor;
    }
    
    
    public Video Init(string category)
    {
        var lastDay = _dbRepository.AvailableUntil(category) ?? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day).AddDays(-1);

        return new Video
        {
            Date = lastDay.AddDays(1)
        };
    }

    public async Task<Video> Download(string category, Models.Video video)
    {
        return await _ytRepository.DownloadNFrames(video.Url, video.Date, category, NumberOfFrames);
    }

    public Dictionary<int, string> GetTmpImages(string category, Video video)
    {
        var lastTimeWeHad = _dbRepository.LastTimeWeHad(category, video.Interpret, video.Title);
        return lastTimeWeHad.HasValue ? throw new Exception("We already had this") : _imageRepositor.GetTempImages(category, video.Date, NumberOfFrames);
    }


    public void Add(string category, Video video)
    {
        var counter = 0;
        var imageCopy = video.ImageIds.ToDictionary(_ => counter++);
        _ytRepository.CopyImages(category, video.Date, imageCopy);
        var videoFile=_ytRepository.MoveVideoFile(category, video);
        _dbRepository.Upsert(new Daily
        {
            Category = category,
            Date = video.Date,
            Interpret = video.Interpret,
            Title = video.Title,
            Url = video.Url,
            VideoFile=videoFile
        });
    }
}
