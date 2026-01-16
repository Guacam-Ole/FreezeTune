using FreezeTune.Logic;
using FreezeTune.Models;
using FreezeTune.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

public class ImagesController : Controller
{
    private readonly IUserLogic _userLogic;

    public ImagesController(IUserLogic userLogic)
    {
        _userLogic = userLogic;
    }

    [HttpPost]
    public Result Guess(string category, Guess guess)
    {
        var date = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
        var guessResult = _userLogic.TakeAGuess(category, guess);
        var result = new Result
        {
            Guesses = guess.GuessCount, InterpretCorrect = guessResult.InterpretMatch,
            TitleCorrect = guessResult.TitleMatch,
            Match = guessResult.Match,
        };
        if (result.Match != null) return result;
        result.NextPictureContents = _userLogic.GetImage(category, date, guess.GuessCount);
        result.NextPicture = guess.GuessCount + 1;
        return result;
    }

    [HttpGet]
    public Result GetTodaysRiddle(string category)
    {
        var date = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
        return new Result
        {
            Guesses = 0,
            InterpretCorrect = false,
            TitleCorrect = false,
            NextPicture = 1,
            NextPictureContents = _userLogic.GetImage(category, date, 0)
        };
    } 
}