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

    public async Task<Models.Video> Download(string category, Models.Video video)
    {
        return await _ytRepository.DownloadNFrames(video.Url, video.Date, category, NumberOfFrames);
    }

    public Dictionary<int, string> GetTmpImages(string category, Video video)
    {
        return _imageRepositor.GetTempImages(category, video.Date, NumberOfFrames);
    }


    public void Add(string category, Models.Video video)
    {
        var imageMove = new Dictionary<int, int>();
        var counter = 0;
        foreach (var videoImageId in video.ImageIds)
        {
            imageMove.Add(counter++, videoImageId);
        }
        _ytRepository.MoveImages(category, video.Date, imageMove);
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