using CliWrap;
using CliWrap.Buffered;
using FreezeTune.Models;
using Microsoft.Playwright;
using Xabe.FFmpeg;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace FreezeTune.Repositories;

public class VideoRepository : IVideoRepository
{
    private readonly Config _config;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const int FrameCount=40; // TODO: Config
    private const int MaxParallelCaptures = 10; // TODO: Config
    private DateTime? LastYtFailure = null;

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

    private static string CleanForPath(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        var sanitized = string.Join("_", value.Split(invalidChars,
            StringSplitOptions.RemoveEmptyEntries));

        return sanitized.TrimEnd('.', ' ');
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


    public string? MoveVideoFile(string category, Video video)
    {
        if (File.Exists(GetVideoPathFor(category, video))) return null;
        var sourceFile = Directory.GetFiles(GetVideoCategoryPath(category)).First();
        var targetFile = GetVideoPathFor(category, video);
        File.Move(sourceFile, targetFile);
        return targetFile;
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
        var path = GetVideoCategoryPath(category);
        if (!Path.Exists(path)) return;
        foreach (var file in Directory.GetFiles(GetVideoCategoryPath(category)))
        {
            File.Delete(file);
        }
    }

    public async Task<Video> DownloadNFrames(string url, DateOnly date, string category, int numberOfFrames)
    {
        var author = "";
        var title = "";

        CleanTemp(category);
        if (url.Contains("youtube"))
        {
            try
            {
                if (LastYtFailure == null || LastYtFailure < DateTime.Now.AddHours(-1))
                {
                    (author, title) = await DownloadVideoFromYoutube(category, url, date);
                    LastYtFailure = null;
                }
            }
            catch (Exception e)
            {
                LastYtFailure = DateTime.Now;
            }

            if (LastYtFailure != null)
            {
                Console.WriteLine("Grabbing images because of yt error");
                await DownloadImagesFromVideo(url); // TODO: Try to get tile + artist
                
            }

            

        }
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


    /*
     // Get stream manifest
       var videoUrl = "https://youtube.com/watch?v=u_yIGGhubZs";
       var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);

       // Select best audio stream (highest bitrate)
       var audioStreamInfo = streamManifest
           .GetAudioStreams()
           .Where(s => s.Container == Container.Mp4)
           .GetWithHighestBitrate();

       // Select best video stream (1080p60 in this example)
       var videoStreamInfo = streamManifest
           .GetVideoStreams()
           .Where(s => s.Container == Container.Mp4)
           .First(s => s.VideoQuality.Label == "1080p60");

       // Download and mux streams into a single file
       await youtube.Videos.DownloadAsync(
           [audioStreamInfo, videoStreamInfo],
           new ConversionRequestBuilder("video.mp4").Build()
       );

     */

    private async Task<(string, string)> DownloadVideoFromYoutube(string category, string youtubeUrl, DateOnly date)
    {
        try
        {
            using var youtube = new YoutubeClient();
            var manifest = await youtube.Videos.Streams.GetManifestAsync(youtubeUrl);

            var audioStreamInfo = manifest
                .GetAudioStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestBitrate();

            var videoStreams = manifest.GetVideoStreams().Where(s => s.Container == Container.Mp4);
            var videoStreamInfo =
                videoStreams
                    .OrderBy(q => q.VideoResolution.Width)
                    .ThenBy(q => q.VideoResolution.Height)
                    .FirstOrDefault(q =>
                        q.VideoResolution.Width >= _config.Width &&
                        q.VideoResolution.Height >= _config.Height)
                ?? videoStreams
                    .OrderByDescending(q => q.VideoResolution.Width)
                    .ThenByDescending(q => q.VideoResolution.Height)
                    .First();

            var videoContents = await youtube.Videos.GetAsync(youtubeUrl);
            if (videoContents.Duration.HasValue && videoContents.Duration.Value.TotalMinutes > 30)
                throw new Exception("Too long");

            await youtube.Videos.DownloadAsync(
                [audioStreamInfo, videoStreamInfo],
                new ConversionRequestBuilder(GetTempVideoPathFor(category, date, videoContents.Author.ChannelTitle,
                    videoContents.Title)).Build()
            );
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


            var response = await Cli.Wrap(shellCommand)
                .WithArguments([
                    "dl", tidalUrl
                ])
                .ExecuteBufferedAsync();

            if (!response.IsSuccess) throw new Exception("Download failed");

            var downloadedFiles = Directory.GetFiles(GetVideoCategoryPath(category), $"{date:yyyy-MM-dd}*.mp4");
            var match = downloadedFiles.OrderByDescending(q => q).First();
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
    


    private async Task Initialize()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = ["--disable-blink-features=AutomationControlled"]
        });
    }

    private async Task<int> GetVideoDuration(string url)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            ViewportSize = new() { Width = 1280, Height = 720 },
            Locale = "en-US"
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(url);
        await page.WaitForSelectorAsync("video", new PageWaitForSelectorOptions { Timeout = 10000 });

        var duration = await page.EvaluateAsync<double>(@"
            () => {
                const video = document.querySelector('video');
                return video ? video.duration : 0;
            }
        ");

        await context.CloseAsync();
        return (int)Math.Floor(duration);
    }

    private static List<int> GenerateTimestamps(int videoDuration, int frameCount)
    {
        var timestamps = new List<int>();
        var interval = (double)videoDuration / (frameCount + 1);

        for (var i = 1; i <= frameCount; i++)
        {
            timestamps.Add((int)Math.Floor(interval * i));
        }

        return timestamps;
    }

    private async Task<List<(int timestamp, byte[] data)>> CaptureFrames(string url, List<int> timestamps)
    {
        var semaphore = new SemaphoreSlim(MaxParallelCaptures);
        var tasks = timestamps.Select(timestamp => CaptureFrameWithSemaphore(url, timestamp, semaphore)).ToList();

        var results = await Task.WhenAll(tasks);
        return results.OrderBy(r => r.Item1).ToList();
    }

    private async Task<(int, byte[])> CaptureFrameWithSemaphore(string url, int seconds, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            var data = await CaptureFrame(url, seconds);
            Console.WriteLine($"received frame at {seconds}");
            return (seconds, data);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<byte[]> CaptureFrame(string url, int seconds)
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            ViewportSize = new ViewportSize { Width = 720, Height = 480 },
            Locale = "en-US"
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync($"{url}&t={seconds}s&cc_load_policy=0");

        await page.WaitForSelectorAsync("video", new PageWaitForSelectorOptions { Timeout = 10000 });

        try
        {
            var rejectButton = await page.WaitForSelectorAsync(
                "button:has-text('Reject all')",
                new PageWaitForSelectorOptions { Timeout = 4000 });

            if (rejectButton != null)
            {
                await rejectButton.ClickAsync();
                await Task.Delay(500);
            }
            else
            {
                Console.WriteLine("no cookie warning received");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        await page.WaitForFunctionAsync("""

                                                    () => {
                                                        const video = document.querySelector('video');
                                                        return video && video.readyState >= 2 && !video.seeking;
                                                    }
                                                
                                        """, new PageWaitForFunctionOptions { Timeout = 10000 });

        await page.EvaluateAsync($$"""

                                               () => {
                                                   const video = document.querySelector('video');
                                                   if (video) {
                                                       video.currentTime = {{seconds}};
                                                       video.pause();
                                                       
                                                       const tracks = video.textTracks;
                                                       for (let i = 0; i < tracks.length; i++) {
                                                           tracks[i].mode = 'disabled';
                                                       }
                                                   }
                                               }
                                           
                                   """);

        await Task.Delay(1500);

        await page.EvaluateAsync("""

                                             () => {
                                                 const overlays = document.querySelectorAll(
                                                     '.ytp-pause-overlay, ' +
                                                     '.ytp-chrome-top, ' +
                                                     '.ytp-chrome-bottom, ' +
                                                     '.ytp-gradient-top, ' +
                                                     '.ytp-gradient-bottom, ' +
                                                     '.ytp-caption-window-container, ' +
                                                     '.caption-window'
                                                 );
                                                 overlays.forEach(el => el.style.display = 'none');
                                             }
                                         
                                 """);

        await Task.Delay(200);

        var video = await page.QuerySelectorAsync("video");
        var screenshot = await video!.ScreenshotAsync(new ElementHandleScreenshotOptions
        {
            Type = ScreenshotType.Jpeg,
            Quality = 75
        });

        await context.CloseAsync();
        return screenshot;
    }

    private async Task Cleanup()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    private async Task DownloadImagesFromVideo(string url)
    {
        if (url.Contains('&'))
        {
            url = url[..url.IndexOf('&')];
        }
        await Initialize();
        var duration = await GetVideoDuration(url);

        var timestamps = GenerateTimestamps(duration, FrameCount);
        Console.WriteLine($"Timestamps: {string.Join(", ", timestamps)}");

        var frames = await CaptureFrames(url, timestamps);
        var counter = 0;
        foreach (var (timestamp, data) in frames)
        {
            counter++;
            Console.WriteLine($"Capturing Frame {counter}/{FrameCount} bei {timestamp}s...");
            await File.WriteAllBytesAsync($"frame_{counter:00}_{timestamp}s.jpg", data);
        }


        await Cleanup();
    }
}