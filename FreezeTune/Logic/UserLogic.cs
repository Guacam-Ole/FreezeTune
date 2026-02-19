using System.Text.RegularExpressions;
using FreezeTune.Models;
using F23.StringSimilarity;
using FreezeTune.Repositories;
using FreezeTune.Services;

namespace FreezeTune.Logic;

public class UserLogic : IUserLogic
{
    private readonly double _maxDistance;
    private readonly IDatabaseRepository _databaseRepository;
    private readonly IImageRepository _imageRepositor;
    private readonly MetricsService _metrics;
    private readonly Config _config;

    public UserLogic(IDatabaseRepository databaseRepository, IImageRepository imageRepositor,
        IVideoRepository videoRepository, Config config, MetricsService metrics)
    {
        _databaseRepository = databaseRepository;
        _imageRepositor = imageRepositor;
        _metrics = metrics;
        _maxDistance = config.MaxDistance;
        _config = config;
    }

    public string GetImage(string category, DateOnly date, int currentNumber)
    {
        return _imageRepositor.GetBase64Image(category, date, currentNumber);
    }

    private double GetDistance(string original, string guess)
    {
        var cleanedOriginal = Regex.Replace(original.ToLower(), @"[^a-zA-Z0-9\s]", "");
        var cleanedGuess = Regex.Replace(guess.ToLower(), @"[^a-zA-Z0-9\s]", "");

        var jaro = new JaroWinkler();
        var distance=jaro.Distance(cleanedOriginal, cleanedGuess);
        return distance;
    }

    public bool ValuesAreCorrect(string category, string interpret, string title)
    {
        var todaysRiddle = _databaseRepository.GetForToday(category);
        if (todaysRiddle == null) throw new Exception("Data is missing");

        Console.WriteLine($"Maxdistance: {_maxDistance}");
        var noArtist = _config.Categories.FirstOrDefault(q => q.Name == category)?.NoArtist ?? false;
        var artistOk = noArtist || GetDistance(todaysRiddle.Interpret, interpret) <= _maxDistance;
        var titleOk  = GetDistance(todaysRiddle.Title, title) <= _maxDistance;
        return artistOk && titleOk;
    }

    public CalculationResult TakeAGuess(string category, Guess guess)
    {
        var todaysRiddle = _databaseRepository.GetForToday(category);
        if (todaysRiddle == null) throw new Exception("Data is missing");

        var noArtist = _config.Categories.FirstOrDefault(q => q.Name == category)?.NoArtist ?? false;
        var artistDistance = noArtist ? 0 : GetDistance(todaysRiddle.Interpret, guess.Interpret);
        var titleDistance = GetDistance(todaysRiddle.Title, guess.Title);

        var result = new CalculationResult
        {
            InterpretMatch = noArtist || artistDistance <= _maxDistance,
            TitleMatch = titleDistance<= _maxDistance
        };

        _metrics.RecordGuess(category);

        if (result.InterpretMatch && result.TitleMatch)
        {
            result.Match = todaysRiddle;
            _databaseRepository.AddStats(category, guess.GuessCount, true);
            _metrics.RecordGameCompleted(category, guess.GuessCount, true);
        }
        else if (guess.GuessCount == 8)
        {
            _databaseRepository.AddStats(category, guess.GuessCount, false);
            _metrics.RecordGameCompleted(category, guess.GuessCount, false);
        }

        result.Interpret = todaysRiddle.Interpret;
        return result;
    }
}