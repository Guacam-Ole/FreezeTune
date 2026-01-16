using Fastenshtein;
using FreezeTune.Models;
using FreezeTune.Repositories;

namespace FreezeTune.Logic;

public class UserLogic:IUserLogic
{
    private const int _maxDistance = 4;
    private readonly IDatabaseRepository _databaseRepository;
    private readonly IImageRepository _imageRepositor;

    public UserLogic(IDatabaseRepository databaseRepository, IImageRepository imageRepositor)
    {
        _databaseRepository = databaseRepository;
        _imageRepositor = imageRepositor;
    }
    
    public string GetImage(string category, DateOnly date, int currentNumber)
    {
        return _imageRepositor.GetBase64Image(category, date, currentNumber);
    }
    
    public CalculationResult TakeAGuess(string category, Guess guess)
    {
        var todaysRiddle = _databaseRepository.GetForDay(category,
            new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));
        if (todaysRiddle == null) throw new Exception("Data is missing");

        var levInterpret = new Levenshtein(todaysRiddle.Interpret);
        var levTitle = new Levenshtein(todaysRiddle.Title);

        var levInterpretValue = levInterpret.DistanceFrom(guess.Interpret);
        var levTitleValue = levTitle.DistanceFrom(guess.Title);

        var result = new CalculationResult
        {
            InterpretMatch = levInterpretValue <= _maxDistance,
            TitleMatch = levTitleValue <= _maxDistance,
            LevenshteinInterpret = levInterpretValue,
            LevenshteinTitle = levTitleValue,
        };
        if (result.InterpretMatch && result.TitleMatch) result.Match = todaysRiddle;
        return result;
    }
}