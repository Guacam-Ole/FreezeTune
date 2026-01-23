using System.Runtime.InteropServices.ComTypes;
using CliWrap;
using CliWrap.Buffered;
using Flurl.Http;
using FreezeTune.Models;
using Xabe.FFmpeg;
using YoutubeExplode;

namespace FreezeTune.Repositories;

public class VideoRepository : IVideoRepository
{
    private readonly Config _config;

    public VideoRepository(Config config)
    {
        _config = config;
    }

    private string GetImagePath(string subDir)
    {
        return $"{_config.BasePath}/{subDir}/";
    }

    private string GetImagePathFor(DateOnly date, string category, string subDir, int number)
    {
        return $"{GetImagePath(subDir)}{category}-{date:yyyy-MM-dd}-{number}.png";
    }

    private string CleanForPath(string value)
    {
        // TODO: Clean Names
        return value;
    }

    private string GetVideoPathFor(string category, Video video)
    {
        var directory = $"{_config.BasePath}/vid/{category}/{CleanForPath(video.Interpret)}";
        Directory.CreateDirectory(directory);
        return $"{directory}/{CleanForPath(video.Interpret)}-{CleanForPath(video.Title)}.mp4";
    }

    private string GetVideoCategoryPath(string category)
    {
        return $"{_config.BasePath}/vid/tmp/{category}";
    }

    private string GetTempVideoPathFor(string category, DateOnly date, string? interpret, string? title)
    {
        var directory = GetVideoCategoryPath(category);
        Directory.CreateDirectory(directory);
        var path = $"{directory}/{date:yyyy-MM-dd}";
        if (interpret != null) path += $"|||{CleanForPath(interpret)}||{CleanForPath(title)}.mp4";
        return path;
    }


    public void MoveVideoFile(string category, Video video)
    {
        if (File.Exists(GetVideoPathFor(category, video))) return;
        var sourceFile = Directory.GetFiles(GetVideoCategoryPath(category)).First();
        
        File.Move(sourceFile, GetVideoPathFor(category, video));

       
    }

  

    private async Task<Video> ExtractFrames(string category, string url, DateOnly date, string author, string title,
        int numberOfFrames)
    {
        var videoInfo = await FFmpeg.GetMediaInfo(GetTempVideoPathFor(category, date, author, title));
        var diff = videoInfo.Duration / numberOfFrames;
        var positions = new List<TimeSpan>();
        for (var i = 0; i < numberOfFrames; i++)
        {
            positions.Add(i * diff);
        }

        await ExtractSingleFrames(date, category, positions.ToArray());
        
        return new Video
        {
            Date = date,
            Interpret = author,
            Title = title,
            Url = url
        };
    }

    private void CleanTemp(string category)
    {
        foreach (var file in Directory.GetFiles(GetVideoCategoryPath(category)))
        {
            File.Delete(file);
        }
    }
    
    public async Task<Video> DownloadNFrames(string url, DateOnly date, string category, int numberOfFrames)
    {
        string author;
        string title;

         CleanTemp(category);
        if (url.Contains("youtube"))
            (author, title) = await DownloadVideoFromYoutube(category, url, date);
        else if (url.Contains("tidal"))
            (author, title) = await DownloadVideoFromTidal(category, url, date);
        else throw new Exception("wrong url");

        if (author == "auth") return new Video { Error = "Requires Tidal Token. Please auth in Docker" };
        return await ExtractFrames(category, url, date, author, title, numberOfFrames);
    }



    public void CopyImages(string category, DateOnly date, Dictionary<int, int> frames)
    {
        foreach (var frame in frames)
        {
            File.Copy(GetImagePathFor(date, category, "tmp", frame.Value),
                GetImagePathFor(date, category, "img", frame.Key), true);
        }

        CleanUpTempDir(category, date);
    }


    private void CleanUpTempDir(string category, DateOnly date)
    {
        var tmpPath = GetImagePath("tmp");
        var files = Directory.GetFiles(tmpPath);
        foreach (var img in files)
        {
            File.Delete(img);
        }
    }


    private async Task<(string, string)> DownloadVideoFromYoutube(string category, string youtubeUrl, DateOnly date)
    {
        try
        {
            using var youtube = new YoutubeClient();
            var manifest = await youtube.Videos.Streams.GetManifestAsync(youtubeUrl);
            var streamInfo = manifest.GetVideoOnlyStreams().OrderBy(q => q.VideoResolution.Width)
                .ThenBy(q => q.VideoResolution.Height)
                .First(q =>
                    q.VideoResolution.Width >= _config.Width && q.VideoResolution.Height >= _config.Height);
            var videoContents = await youtube.Videos.GetAsync(youtubeUrl);
            await youtube.Videos.Streams.DownloadAsync(streamInfo,
                GetTempVideoPathFor(category, date, videoContents.Author.ChannelTitle, videoContents.Title));

            return (videoContents.Author.ChannelTitle, videoContents.Title);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<(string, string)> DownloadVideoFromTidal(string category, string tidalUrl, DateOnly date)
    {
        const string shellCommand = "tidal-dl-ng";
        const string shellConfig = "cfg";
            
        try
        {
             await Cli.Wrap(shellCommand)
                .WithArguments([
                    shellConfig, "download_base_path",
                    "" + GetVideoCategoryPath(category) + ""
                ])
                .ExecuteBufferedAsync();
            
            await Cli.Wrap(shellCommand)
                .WithArguments([
                    shellConfig, "format_video",
                    $"{date:yyyy-MM-dd}|||{{artist_name}}||{{track_title}}"
                ])
                .ExecuteBufferedAsync();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                 await Cli.Wrap("tidal-dl-ng")
                    .WithArguments(["login"])
                    .ExecuteBufferedAsync(cts.Token);
            }
            catch (OperationCanceledException e)
            {
                return ("auth", "auth! das Spiel ist auth!");
            }
            
          
            var response=await Cli.Wrap(shellCommand)
                .WithArguments([
                    "dl", tidalUrl
                ])
                .ExecuteBufferedAsync();

            if (!response.IsSuccess) throw new Exception("Download failed");
            
            var downloadedFiles = Directory.GetFiles(GetVideoCategoryPath(category), $"{date:yyyy-MM-dd}*.mp4");
            var match = downloadedFiles.OrderByDescending(q=>q) .First();
            var rightpart = match.Substring(match.IndexOf("|||", StringComparison.CurrentCulture) + 3);
            var parts = rightpart.Split("||");

            return (parts[0], parts[1][..parts[1].LastIndexOf('.')]);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task ExtractSingleFrames(DateOnly date, string category, TimeSpan[] positions)
    {
        try
        {
            var counter = 0;
            var filename = Directory.GetFiles(GetVideoCategoryPath(category), "*.mp4").First();

            foreach (var timeSpan in positions)
            {
                var res = await FFmpeg.Conversions.FromSnippet.Snapshot(filename,
                    GetImagePathFor(date, category, "tmp", counter++), timeSpan);
                await res.Start();
            }
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