using FreezeTune.Models;
using FreezeTune.Repositories;

namespace FreezeTune.Logic;



public class MaintenanceLogic:IMaintenanceLogic
{
    private const int NumberOfFrames = 40;
    private readonly IDatabaseRepository _dbRepository;
    private readonly IYoutubeRepository _ytRepository;
    private readonly IImageRepository _imageRepositor;

    public MaintenanceLogic(IDatabaseRepository dbRepository, IYoutubeRepository ytRepository, IImageRepository imageRepositor)
    {
        _dbRepository = dbRepository;
        _ytRepository = ytRepository;
        _imageRepositor = imageRepositor;
    }
    
    
    public Models.Video Init(string category)
    {
        var lastDay = _dbRepository.AvailableUntil(category) ??
                      new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);

        return new Models.Video
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
        return _imageRepositor.GetTempImages(category, video.Date, NumberOfFrames);
    }


    public void Add(string category, Video video)
    {
        var counter = 0;
        var imageCopy = video.ImageIds.ToDictionary(_ => counter++);
        _ytRepository.CopyImages(category, video.Date, imageCopy);
        _ytRepository.MoveVideoFile(video);
        _dbRepository.Upsert(new Daily
        {
            Category = category,
            Date = video.Date,
            Interpret = video.Interpret,
            Title = video.Title,
            Url = video.Url
        });
    }
}