using FreezeTune.Models;
using Xabe.FFmpeg;
using YoutubeExplode;

namespace FreezeTune.Repositories;


public class YoutubeRepository : IYoutubeRepository
{
    private readonly Config _config;

    public YoutubeRepository(Config config)
    {
        _config = config;
    }
    
    
    private string GetImagePathFor(DateOnly date, string category, string subdir, int number)
    {
        return  $"{_config.BasePath}/{subdir}/{category}-{date:yyyy-MM-dd}-{number}.png";
    }

    private string CleanForPath(string value)
    {
        return value;
    }

    private string GetVideoPathFor(Video video)
    {
        var directory = $"{_config.BasePath}/vid/{CleanForPath(video.Interpret)}";
        Directory.CreateDirectory(directory);
        return $"{directory}/{CleanForPath(video.Interpret)}-{CleanForPath(video.Title)}.mp4";
    }
    
    private string GetTempVideoPathFor(DateOnly date)
    {
        var directory = $"{_config.BasePath}/vid/tmp";
        Directory.CreateDirectory(directory);
        return $"{directory}/{date:yyyy-MM-dd}.mp4";
    }

    public void MoveVideoFile(Video video)
    {
        File.Move(GetTempVideoPathFor(video.Date), GetVideoPathFor(video));
    }
    
    public async Task<Video> DownloadNFrames(string youtubeUrl, DateOnly date, string category, int numberOfFrames)
    {
        var (author,title)=await DownloadVideo(youtubeUrl, date);
        var videoInfo=await FFmpeg.GetMediaInfo(GetTempVideoPathFor(date));
        var diff = videoInfo.Duration / numberOfFrames;
        var positions = new List<TimeSpan>();
        for (var i = 0; i < numberOfFrames; i++)
        {
            positions.Add(i*diff);
        }
        await ExtractSingleFrames(date, category, positions.ToArray());
        return new Video
        {
            Date = date,
            Interpret = author,
            Title = title,
            Url = youtubeUrl
        };
    }

    public void CopyImages(string category, DateOnly date, Dictionary<int, int> frames)
    {
        foreach (var frame in frames)
        {
            File.Copy(GetImagePathFor(date, category, "tmp", frame.Value), GetImagePathFor(date,category,"img", frame.Key), true);
        }
        // TODO: Delete old temp files
    }


    private async Task<(string,string)> DownloadVideo(string youtubeUrl, DateOnly date)
    {
        try
        {
            using var youtube = new YoutubeClient();
            var manifest = await youtube.Videos.Streams.GetManifestAsync(youtubeUrl);
            var streamInfo = manifest.GetVideoOnlyStreams().OrderBy(q => q.VideoResolution.Width).ThenBy(q => q.VideoResolution.Height)
                .First(q =>
                    q.VideoResolution.Width >= _config.Width && q.VideoResolution.Height >= _config.Height);
            await youtube.Videos.Streams.DownloadAsync(streamInfo, GetTempVideoPathFor(date));
            var videoContents = await youtube.Videos.GetAsync(youtubeUrl);
            return (videoContents.Author.ChannelTitle, videoContents.Title);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    private async Task ExtractSingleFrames( DateOnly date, string category, TimeSpan[] positions)
    {
        try
        {
            var counter = 0;
            foreach (var timeSpan in positions)
            {
              var res=  await FFmpeg.Conversions.FromSnippet.Snapshot(GetTempVideoPathFor
                  (date), GetImagePathFor(date, category, "tmp", counter++), timeSpan);
              await res.Start();
            }
            ///File.Delete(GetTempVideoPathFor(date));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public List<string> GetImagesFor(DateOnly date, string category)
    {
        var files = Directory.GetFiles($"{_config.BasePath}/img/ {category}-{date:yyyy-MM-dd}-*.png");
        return files.ToList();
    }

   
}