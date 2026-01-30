using System.Text.RegularExpressions;
using Fastenshtein;
using FreezeTune.Models;
using FreezeTune.Repositories;

namespace FreezeTune.Logic;

public class UserLogic : IUserLogic
{
    private readonly uint _maxDistance;
    private readonly IDatabaseRepository _databaseRepository;
    private readonly IImageRepository _imageRepositor;
    

    public UserLogic(IDatabaseRepository databaseRepository, IImageRepository imageRepositor,
        IVideoRepository videoRepository, Config config)
    {
        _databaseRepository = databaseRepository;
        _imageRepositor = imageRepositor;
      
        _maxDistance = config.MaxDistance;
    }

    public string GetImage(string category, DateOnly date, int currentNumber)
    {
        return _imageRepositor.GetBase64Image(category, date, currentNumber);
    }

    private int GetLevenshtein(string original, string guess)
    {
        var cleanedOriginal = Regex.Replace(original.ToLower(), @"[^a-zA-Z0-9\s]", "");
        var cleanedGuess = Regex.Replace(guess.ToLower(), @"[^a-zA-Z0-9\s]", "");

        var lev = new Levenshtein(cleanedOriginal);
        return lev.DistanceFrom(cleanedGuess);
    }

    public bool ValuesAreCorrect(string category, string interpret, string title)
    {
        var todaysRiddle = _databaseRepository.GetForToday(category);
        if (todaysRiddle == null) throw new Exception("Data is missing");

        var levInterpretValue = GetLevenshtein(todaysRiddle.Interpret, interpret);
        var levTitleValue = GetLevenshtein(todaysRiddle.Title, title);
        return levInterpretValue <= _maxDistance && levTitleValue <= _maxDistance;
    }
    
    public CalculationResult TakeAGuess(string category, Guess guess)
    {
        var todaysRiddle = _databaseRepository.GetForToday(category);
        if (todaysRiddle == null) throw new Exception("Data is missing");

        var levInterpretValue = GetLevenshtein(todaysRiddle.Interpret, guess.Interpret);
        var levTitleValue = GetLevenshtein(todaysRiddle.Title, guess.Title);

        var result = new CalculationResult
        {
            InterpretMatch = levInterpretValue <= _maxDistance,
            TitleMatch = levTitleValue <= _maxDistance,
            LevenshteinInterpret = levInterpretValue,
            LevenshteinTitle = levTitleValue,
        };
        if (result.InterpretMatch && result.TitleMatch)
        {
            result.Match = todaysRiddle;
            _databaseRepository.AddStats(category, guess.GuessCount, true);
        }
        else if (guess.GuessCount == 8)
        {
            _databaseRepository.AddStats(category, guess.GuessCount, false);
        }

        result.Interpret = todaysRiddle.Interpret;
        return result;
    }
}