using Microsoft.Playwright;

public class YouTubeFrameCapture
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly int _maxParallelCaptures = 10; // Nicht zu viele auf einmal

    public async Task Initialize()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });
    }

    public async Task<int> GetVideoDuration(string videoId)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            ViewportSize = new() { Width = 1280, Height = 720 }
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync($"https://www.youtube.com/watch?v={videoId}");
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

    public List<int> GenerateTimestamps(int videoDuration, int frameCount)
    {
        if (frameCount <= 0) return new List<int>();
        if (frameCount == 1) return new List<int> { videoDuration / 2 };

        var timestamps = new List<int>();
        var interval = (double)videoDuration / (frameCount + 1);

        for (int i = 1; i <= frameCount; i++)
        {
            timestamps.Add((int)Math.Floor(interval * i));
        }

        return timestamps;
    }

    // NEUE Methode: Mehrere Frames auf einmal
    public async Task<List<(int timestamp, byte[] data)>> CaptureFrames(string videoId, List<int> timestamps)
    {
        var semaphore = new SemaphoreSlim(_maxParallelCaptures);
        var tasks = new List<Task<(int, byte[])>>();

        foreach (var timestamp in timestamps)
        {
            tasks.Add(CaptureFrameWithSemaphore(videoId, timestamp, semaphore));
        }

        var results = await Task.WhenAll(tasks);
        return results.OrderBy(r => r.Item1).ToList();
    }

    private async Task<(int, byte[])> CaptureFrameWithSemaphore(string videoId, int seconds, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            var data = await CaptureFrame(videoId, seconds);
            Console.WriteLine($"received frame at {seconds}");
            return (seconds, data);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<byte[]> CaptureFrame(string videoId, int seconds)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            ViewportSize = new() { Width = 1280, Height = 720 }
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync($"https://www.youtube.com/watch?v={videoId}&t={seconds}s&cc_load_policy=0");

        await page.WaitForSelectorAsync("video", new PageWaitForSelectorOptions { Timeout = 10000 });

        try
        {
            var rejectButton = await page.WaitForSelectorAsync(
                "button:has-text('Alle ablehnen')",
                new PageWaitForSelectorOptions { Timeout = 2000 });

            if (rejectButton != null)
            {
                await rejectButton.ClickAsync();
                await Task.Delay(500);
            }
        }
        catch { }

        await page.WaitForFunctionAsync(@"
            () => {
                const video = document.querySelector('video');
                return video && video.readyState >= 2 && !video.seeking;
            }
        ", new PageWaitForFunctionOptions { Timeout = 10000 });

        await page.EvaluateAsync($@"
            () => {{
                const video = document.querySelector('video');
                if (video) {{
                    video.currentTime = {seconds};
                    video.pause();
                    
                    const tracks = video.textTracks;
                    for (let i = 0; i < tracks.length; i++) {{
                        tracks[i].mode = 'disabled';
                    }}
                }}
            }}
        ");

        await Task.Delay(1500);

        await page.EvaluateAsync(@"
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
        ");

        await Task.Delay(200);

        var video = await page.QuerySelectorAsync("video");
        var screenshot = await video!.ScreenshotAsync(new()
        {
            Type = ScreenshotType.Jpeg,
            Quality = 75
        });

        await context.CloseAsync();
        return screenshot;
    }

    public async Task Cleanup()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}



/* call:

using Microsoft.Playwright;

var capture = new YouTubeFrameCapture();
await capture.Initialize();

string videoId = "dQw4w9WgXcQ";
int frameCount = 40; // Oder 5, 10, 12 - beliebig!

// 1. Videolänge ermitteln
Console.WriteLine("Ermittle Videolänge...");
int duration = await capture.GetVideoDuration(videoId);
Console.WriteLine($"Video ist {duration} Sekunden lang");

// 2. Timestamps berechnen
var timestamps = capture.GenerateTimestamps(duration, frameCount);
Console.WriteLine($"Timestamps: {string.Join(", ", timestamps)}");

// 3. Frames capturem

var frames = await capture.CaptureFrames(videoId, timestamps);
int counter = 0;
foreach (var (timestamp, data) in frames)
{
    counter++;
    Console.WriteLine($"Capturing Frame {counter}/{frameCount} bei {timestamp}s...");
    //var frame = await capture.CaptureFrame(videoId, timestamps[i]);
    await File.WriteAllBytesAsync($"frame_{counter:00}_{timestamp}s.jpg", data);
}


await capture.Cleanup();
Console.WriteLine("Fertig!");
















*/
