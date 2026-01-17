using System.Globalization;
using System.Text.Json;
using FreezeTune;
using FreezeTune.Models;
using FreezeTune.Repositories;


public class Program
{
    private static IYoutubeRepository _ytRepo = new YoutubeRepository(new Config());
    private static IDatabaseRepository _dbRepo = new DatabaseRepository(new Config());
    private static ConsoleColor _defaultColor = Console.ForegroundColor;

    private static void ColWriteline(ConsoleColor color, string contents)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(contents);
        Console.ForegroundColor = _defaultColor;
    }

    private static void AutoColor(string contents)
    {
        var coloredContents = new List<KeyValuePair<ConsoleColor, string>>();

        if (contents.Contains('[') && contents.Contains(']'))
        {
            coloredContents.Add(new KeyValuePair<ConsoleColor, string>(_defaultColor,
                contents[..(contents.IndexOf('[') + 1)]));
            coloredContents.Add(new KeyValuePair<ConsoleColor, string>(ConsoleColor.Yellow,
                contents.Substring(contents.IndexOf('[') + 1, contents.IndexOf(']') - contents.IndexOf('[') - 1)));
            coloredContents.Add(new KeyValuePair<ConsoleColor, string>(_defaultColor,
                contents[contents.IndexOf(']')..]));
        }
        else
        {
            coloredContents = new List<KeyValuePair<ConsoleColor, string>>
            {
                new(_defaultColor, contents)
            };
        }


        foreach (var coloredContent in coloredContents)
        {
            ColWrite(coloredContent.Key, coloredContent.Value);
        }
    }

    private static void ColWrite(ConsoleColor color, string contents)
    {
        _defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(contents);
        Console.ForegroundColor = _defaultColor;
    }

    public static async Task Main()
    {
        // Console.WriteLine("---| FreeTune Updater |---");
        // AutoColor("Category [80s]:");
        // var category = Console.ReadLine();
        // if (string.IsNullOrWhiteSpace(category)) category = "80s";
        // var validUntil = _dbRepo.AvailableUntil(category);
        // var nextDate = validUntil == null
        //     ? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day)
        //     : validUntil.Value.AddDays(1);
        // ColWrite(_defaultColor, "Entries available until:");
        // ColWriteline(ConsoleColor.Cyan, $"{validUntil}");
        //
        // Console.Write("URL:");
        // var url = Console.ReadLine();
        // AutoColor($"Date (31.12.1900) [{nextDate.ToString("dd.MM.yyyy")}]:");
        // var dateStr = Console.ReadLine();
        // var date = string.IsNullOrWhiteSpace(dateStr)
        //     ? nextDate
        //     : DateOnly.Parse(dateStr, CultureInfo.GetCultureInfo("de-de"));
        //
        // AutoColor("Timestamps (0:33 1:23 0:3):");
        // var timestamps = Console.ReadLine();
        //
        // AutoColor("Interpret:");
        // var interpret = Console.ReadLine();
        // AutoColor("Title:");
        // var title = Console.ReadLine();
        // Console.WriteLine("----> Erstelle Bilder");
        //
        //
        // var timeSpans = new List<TimeSpan>();
        // foreach (var position in timestamps.Split(' ',
        //              StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        // {
        //     if (!position.Contains(':'))
        //     {
        //         timeSpans.Add(TimeSpan.FromSeconds(int.Parse(position)));
        //         continue;
        //     }
        //
        //     var parts = position.Split(':');
        //     timeSpans.Add(new TimeSpan(0, int.Parse(parts[0]), int.Parse(parts[1])));
        // }
        //
        // await _ytRepo.DownloadSingleFrames(url, date, category, timeSpans.ToArray());
        //
        // _dbRepo.Upsert(new Daily { Category = category, Date = date, Interpret = interpret, Title = title, Url = url });
        //
        // var dbContents = _dbRepo.GetForDay(category, date);
        // Console.WriteLine(JsonSerializer.Serialize(dbContents));
    }
}