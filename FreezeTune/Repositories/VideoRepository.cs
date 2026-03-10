using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using FreezeTune.Models;
using FreezeTune.Services;
using HtmlAgilityPack;
using Microsoft.Playwright;
using Xabe.FFmpeg;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace FreezeTune.Repositories;

public class VideoRepository : IVideoRepository
{
    private readonly Config _config;
    private readonly ProgressService _progressService;
    private readonly ILogger<VideoRepository> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const int MaxParallelCaptures = 4; // TODO: Config
    private DateTime? _lastYtFailure = null;

    public VideoRepository(Config config, ProgressService progressService, ILogger<VideoRepository> logger)
    {
        _config = config;
        _progressService = progressService;
        _logger = logger;
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
        var sourceFile = Directory.GetFiles(GetVideoCategoryPath(category)).FirstOrDefault();
        if (sourceFile==null) return null; // Sourcefile does not exist
        var targetFile = GetVideoPathFor(category, video);
        if (File.Exists(targetFile)) return targetFile; // Targetfile already exist
        
        File.Move(sourceFile, targetFile);
        return targetFile;
    }

    private async Task<Video> ExtractFrames(string category, string url, DateOnly date, string author, string title,
        int numberOfFrames, Action<int>? onProgress = null)
    {
        var videoFile = GetTempVideoPathFor(category, date, author, title);
        if (!File.Exists(videoFile))
            return new Video
            {
                Date = date,
                Interpret = author,
                Title = title,
                Url = url
            };
        var videoInfo = await FFmpeg.GetMediaInfo(videoFile);
        var diff = videoInfo.Duration / numberOfFrames;
        var positions = new List<TimeSpan>();
        for (var i = 0; i < numberOfFrames; i++)
        {
            positions.Add(i * diff);
        }

        await ExtractSingleFrames(date, category, positions.ToArray(), onProgress);

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
        var videoPath = GetVideoCategoryPath(category);
        if (!Path.Exists(videoPath)) return;
        foreach (var file in Directory.GetFiles(videoPath))
        {
            File.Delete(file);
        }

        var imgTmpPath = GetImagePath("tmp");
        if (!Path.Exists(imgTmpPath)) return;
        foreach (var file in Directory.GetFiles(imgTmpPath))
        {
            if (!file.StartsWith(Path.Combine(imgTmpPath,category))) continue;
            File.Delete(file);
        }
    }

    public async Task<Video> DownloadNFrames(string url, DateOnly date, string category, int numberOfFrames,
        string? sessionId = null)
    {
        var author = "";
        var title = "";

        void ReportProgress(int percent, string stage)
        {
            if (sessionId != null) _progressService.Update(sessionId, percent, stage);
        }

        ReportProgress(0, "Starte...");
        CleanTemp(category);

        if (url.Contains("youtube"))
        {
            try
            {
                if (_lastYtFailure == null || _lastYtFailure < DateTime.Now.AddHours(-1))
                {
                    (author, title) = await DownloadVideoFromYoutube(category, url, date,
                        p => ReportProgress(p / 2, "YouTube Download"));
                    _lastYtFailure = null;
                }
            }
            catch (Exception)
            {
                _lastYtFailure = DateTime.Now;
            }

            if (_lastYtFailure != null)
            {
                var ytDlpSucceeded = false;
                try
                {
                    _logger.LogDebug("Trying yt-dlp as YouTube fallback");
                    (author, title) = await DownloadVideoWithYtDlp(category, url, date,
                        p => ReportProgress(p / 2, "yt-dlp Download"));
                    ytDlpSucceeded = true;
                }
                catch (Exception)
                {
                    _logger.LogDebug("yt-dlp also failed, falling back to screengrab");
                }

                if (!ytDlpSucceeded)
                {
                    _logger.LogDebug("Grabbing images because of yt error");
                    (author, title) =
                        await DownloadImagesFromVideo(url, date, category, numberOfFrames, p => ReportProgress(p / 2, "Frame Capture"));
                }
            }
        }
        else if (url.Contains("tidal"))
            (author, title) =
                await DownloadVideoFromTidal(category, url, date, p => ReportProgress(p / 2, "Tidal Download"));
        else if (url.Contains("dailymotion"))
        {
            try
            {
                (author, title) = await DownloadVideoWithYtDlp(category, url, date,
                    p => ReportProgress(p / 2, "Dailymotion Download"), referer: "https://www.dailymotion.com");
            }
            catch (Exception)
            {
                _logger.LogDebug("yt-dlp failed for Dailymotion, falling back to screengrab");
                (author, title) = await DownloadImagesFromVideo(url, date, category, numberOfFrames,
                    p => ReportProgress(p / 2, "Frame Capture"));
            }
        }
        else throw new Exception("wrong url");

        if (author == "auth") return new Video { Error = "Requires Tidal Token. Please auth in Docker" };

        ReportProgress(50, "Extrahiere Frames...");
        var result = await ExtractFrames(category, url, date, author, title, numberOfFrames,
            p => ReportProgress(50 + p / 2, "Extrahiere Frames"));
        ReportProgress(100, "Fertig");

        return result;
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

    private async Task<(string, string)> DownloadVideoFromYoutube(string category, string youtubeUrl, DateOnly date,
        Action<int>? onProgress = null)
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

            var lastReported = -1;
            var progress = new Progress<double>(p =>
            {
                var percent = (int)(p * 100);
                if (percent == lastReported) return;
                lastReported = percent;
                onProgress?.Invoke(percent);
            });
            await youtube.Videos.DownloadAsync(
                [audioStreamInfo, videoStreamInfo],
                new ConversionRequestBuilder(GetTempVideoPathFor(category, date, videoContents.Author.ChannelTitle,
                    videoContents.Title)).Build(),
                progress
            );
            _logger.LogInformation("Downloaded '{Artist}' - '{Title}' as Video from YouTube",videoContents.Author.ChannelTitle, videoContents.Title);
            return (videoContents.Author.ChannelTitle, videoContents.Title);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed downloading '{Url}' from Youtube",youtubeUrl);
            throw;
        }
    }

    private async Task<(string, string)> DownloadVideoFromTidal(string category, string tidalUrl, DateOnly date,
        Action<int>? onProgress = null)
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

            var progressCts = new CancellationTokenSource();
            var progressTask = Task.Run(async () =>
            {
                var startTime = DateTime.Now;
                const int totalSeconds = 30;
                while (!progressCts.Token.IsCancellationRequested)
                {
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    var percent = Math.Min(99, (int)(elapsed / totalSeconds * 100));
                    onProgress?.Invoke(percent);
                    await Task.Delay(300, progressCts.Token).ConfigureAwait(false);
                }
            }, progressCts.Token);

            var response = await Cli.Wrap(shellCommand)
                .WithArguments([
                    "dl", tidalUrl
                ])
                .ExecuteBufferedAsync();

            progressCts.Cancel();
            try
            {
                await progressTask;
            }
            catch (OperationCanceledException)
            {
            }

            onProgress?.Invoke(100);

            if (!response.IsSuccess) throw new Exception("Download failed");

            var downloadedFiles = Directory.GetFiles(GetVideoCategoryPath(category), $"{date:yyyy-MM-dd}*.mp4");
            var match = downloadedFiles.OrderByDescending(q => q).First();
            var rightPart = match[(match.IndexOf("|||", StringComparison.CurrentCulture) + 3)..];
            var parts = rightPart.Split("||");

            var artist = parts[0];
            var title = parts[1][..parts[1].LastIndexOf('.')];
            
            _logger.LogInformation("Downloaded '{Artist}' - '{Title}' from Tidal", artist,title);
            return (artist,title);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed downloading '{Url}' from Tidal",tidalUrl);
            throw;
        }
    }

    private async Task<(string, string)> DownloadVideoWithYtDlp(string category, string url, DateOnly date,
        Action<int>? onProgress = null, string? referer = null)
    {
        const string shellCommand = "yt-dlp";
        try
        {
            var outputTemplate = $"{GetVideoCategoryPath(category)}/{date:yyyy-MM-dd}|||%(uploader)s||%(title)s.%(ext)s";

            var progressCts = new CancellationTokenSource();
            var progressTask = Task.Run(async () =>
            {
                var startTime = DateTime.Now;
                const int totalSeconds = 30;
                while (!progressCts.Token.IsCancellationRequested)
                {
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    var percent = Math.Min(99, (int)(elapsed / totalSeconds * 100));
                    onProgress?.Invoke(percent);
                    await Task.Delay(300, progressCts.Token).ConfigureAwait(false);
                }
            }, progressCts.Token);

            List<string> args = ["--merge-output-format", "mp4", "-f", "bestvideo+bestaudio/best"];
            if (referer != null) args.AddRange(["--referer", referer]);
            args.AddRange(["-o", outputTemplate, url]);

            var response = await Cli.Wrap(shellCommand)
                .WithArguments(args)
                .ExecuteBufferedAsync();

            progressCts.Cancel();
            try { await progressTask; } catch (OperationCanceledException) { }
            onProgress?.Invoke(100);

            if (!response.IsSuccess) throw new Exception("Download failed");

            var downloadedFiles = Directory.GetFiles(GetVideoCategoryPath(category), $"{date:yyyy-MM-dd}*.mp4");
            var match = downloadedFiles.OrderByDescending(q => q).First();
            var rightPart = match[(match.IndexOf("|||", StringComparison.CurrentCulture) + 3)..];
            var parts = rightPart.Split("||");

            var artist = parts[0];
            var title = parts[1][..parts[1].LastIndexOf('.')];

            _logger.LogInformation("Downloaded '{Artist}' - '{Title}' via yt-dlp from '{Url}'", artist, title, url);
            return (artist, title);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed downloading '{Url}' via yt-dlp", url);
            throw;
        }
    }

    private async Task ExtractSingleFrames(DateOnly date, string category, TimeSpan[] positions,
        Action<int>? onProgress = null)
    {
        try
        {
            var counter = 0;
            var filename = Directory.GetFiles(GetVideoCategoryPath(category), "*.mp4").First();
            var total = positions.Length;

            foreach (var timeSpan in positions)
            {
                var res = await FFmpeg.Conversions.FromSnippet.Snapshot(filename,
                    GetImagePathFor(date, category, "tmp", counter++), timeSpan);
                res.SetOverwriteOutput(true);
                await res.Start();
                onProgress?.Invoke(counter * 100 / total);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Failed extracting frames for Category '{Category}'",category);
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

    private async Task<(int, string, string)> GetVideoMetadata(string url)
    {
        var context = await GetBrowserContext();
        var page = await context.NewPageAsync();

        await page.GotoAsync(url);
        await page.WaitForSelectorAsync("video", new PageWaitForSelectorOptions { Timeout = 10000 });

        var html = await page.ContentAsync();
        await context.CloseAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = doc.DocumentNode.SelectSingleNode("//meta[@itemprop='name']")?.GetAttributeValue("content", "")
                    ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", "")
                    ?? string.Empty;

        var channel = doc.DocumentNode.SelectSingleNode("//span[@itemprop='author']//link[@itemprop='name']")?.GetAttributeValue("content", "")
                      ?? string.Empty;

        var duration = 0;
        var isoDuration = doc.DocumentNode.SelectSingleNode("//meta[@itemprop='duration']")?.GetAttributeValue("content", "");
        if (!string.IsNullOrEmpty(isoDuration))
        {
            var match = Regex.Match(isoDuration, @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?");
            if (match.Success)
            {
                var hours = string.IsNullOrEmpty(match.Groups[1].Value) ? 0 : int.Parse(match.Groups[1].Value);
                var minutes = string.IsNullOrEmpty(match.Groups[2].Value) ? 0 : int.Parse(match.Groups[2].Value);
                var seconds = string.IsNullOrEmpty(match.Groups[3].Value) ? 0 : int.Parse(match.Groups[3].Value);
                duration = hours * 3600 + minutes * 60 + seconds;
            }
        }

        if (title.StartsWith(channel)) title = title[channel.Length..];
        return (duration, title, channel);
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

    private async Task<List<(int timestamp, byte[] data)>> CaptureFrames(string url, List<int> timestamps,
        Action<int>? onProgress = null)
    {
        var semaphore = new SemaphoreSlim(MaxParallelCaptures);
        var completedCount = 0;
        var total = timestamps.Count;

        var tasks = timestamps.Select(async timestamp =>
        {
            var result = await CaptureFrameWithSemaphore(url, timestamp, semaphore);
            var completed = Interlocked.Increment(ref completedCount);
            onProgress?.Invoke(completed * 100 / total);
            return result;
        }).ToList();

        var results = await Task.WhenAll(tasks);
        return results.OrderBy(r => r.Item1).ToList();
    }

    private async Task<(int, byte[])> CaptureFrameWithSemaphore(string url, int seconds, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            var data = await CaptureFrame(url, seconds);
            _logger.LogDebug("received frame at {seconds}", seconds);
            return (seconds, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed capturing frames from '{Url}'", url);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<IBrowserContext> GetBrowserContext()
    {
        return await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            ViewportSize = new ViewportSize { Width = 720, Height = 480 },
            Locale = "en-US"
        });   
    }
    
    
    private async Task<byte[]> CaptureFrame(string url, int seconds)
    {
        var context = await GetBrowserContext();
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
                _logger.LogWarning("no cookie warning received");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed capturing single Frame at '{Seconds}' seconds for '{Url}'",seconds, url);
        }

        await page.WaitForFunctionAsync(
            """
            () => {
                const video = document.querySelector('video');
                return video && video.readyState >= 2 && !video.seeking;
                  }
            """, new PageWaitForFunctionOptions { Timeout = 10000 });

        await page.EvaluateAsync(
            """
               (seconds) => {
                   const video = document.querySelector('video');
                   if (video) {
                       video.currentTime = seconds;
                       video.pause();

                       const tracks = video.textTracks;
                       for (let i = 0; i < tracks.length; i++) {
                           tracks[i].mode = 'disabled';
                       }
                   }
               }
            """, seconds);

        await Task.Delay(1500);

        await page.EvaluateAsync(
            """
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
            Type = ScreenshotType.Png,
        });

        await context.CloseAsync();
        return screenshot;
    }

    private async Task Cleanup()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    private async Task<(string, string)> DownloadImagesFromVideo(string url, DateOnly date, string category, int frameCount,
        Action<int>? onProgress = null)
    {
        if (url.Contains('&'))
        {
            url = url[..url.IndexOf('&')];
        }

        await Initialize();
        var (duration, title, channel) = await GetVideoMetadata(url);

        var timestamps = GenerateTimestamps(duration, frameCount);
        
        

        var frames = await CaptureFrames(url, timestamps, onProgress);
        var counter = 0;
        foreach (var (timestamp, data) in frames)
        {
            var filename = GetImagePathFor(date, category, "tmp", counter++);
            await File.WriteAllBytesAsync(filename, data);
        }

        await Cleanup();
        _logger.LogInformation("Retrieved Images from '{Url}' at the following timestamps: '{Timestamps}'", url, string.Join(",", timestamps));
        return (channel, title);
    }
}