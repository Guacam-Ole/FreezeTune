using System.ComponentModel;
using System.Runtime.InteropServices.JavaScript;
using Xabe.FFmpeg;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace FreezeTune.Repositories;


public class YoutubeRepository : IYoutubeRepository
{
    private readonly Config _config;

    public YoutubeRepository(Config config)
    {
        _config = config;
    }
    
    
    private string GetImagePathFor(DateOnly date, string category, int number)
    {
        return  $"{_config.BasePath}/img/{category}-{date:yyyy-MM-dd}-{number}.png";
    }

    private string GetVideoPathFor(DateOnly date)
    {
        return $"{_config.BasePath}/vid/{date:yyyy-MM-dd}.mp4";
    }

    public async Task DownloadSingleFrames(string youtubeUrl, DateOnly date, string category, params TimeSpan[] positions)
    {
        try
        {
            using var ms = new MemoryStream();
            using var youtube = new YoutubeClient();
            var manifest = await youtube.Videos.Streams.GetManifestAsync(youtubeUrl);
            var streamInfo = manifest.GetVideoOnlyStreams().OrderBy(q => q.VideoResolution.Width).ThenBy(q => q.VideoResolution.Height)
                .First(q =>
                    q.VideoResolution.Width >= _config.Width && q.VideoResolution.Height >= _config.Height);
            //var streamInfo = manifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
            await youtube.Videos.Streams.DownloadAsync(streamInfo, GetVideoPathFor(date));

            int counter = 0;
            foreach (var timeSpan in positions)
            {
              var res=  await FFmpeg.Conversions.FromSnippet.Snapshot(GetVideoPathFor
                  (date), GetImagePathFor(date, category, counter++), timeSpan);
              await res.Start();
            }
            File.Delete(GetVideoPathFor(date));
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