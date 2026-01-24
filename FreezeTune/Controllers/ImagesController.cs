using FreezeTune.Logic;
using FreezeTune.Models;
using FreezeTune.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

[Route("Image")]
public class ImagesController : Controller
{
    private readonly IUserLogic _userLogic;
    private readonly IDatabaseRepository _databaseRepository;
    private readonly Config _config;

    public ImagesController(IUserLogic userLogic, IDatabaseRepository databaseRepository, Config config)
    {
        _userLogic = userLogic;
        _databaseRepository = databaseRepository;
        _config = config;
    }

    [HttpGet("Categories")]
    public List<string> GetCategories()
    {
        return _config.Categories;
    }

    [HttpPost]
    public Result Guess(string category, [FromBody] Guess guess)
    {
        var date = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
        var guessResult = _userLogic.TakeAGuess(category, guess);
        var result = new Result
        {
            Guesses = guess.GuessCount,
            InterpretCorrect = guessResult.InterpretMatch,
            TitleCorrect = guessResult.TitleMatch,
            Match = guessResult.Match,
        };

        if (guess.GuessCount >= 6 || result.InterpretCorrect)
        {
            // Hint interpret
            result.Interpret = guessResult.Interpret;
        }

        if (result.Match != null || guess.GuessCount >= 8)
        {
            // All Pictures again for the final page
            result.AllPictureContents = [];
            for (var i = 0; i < 8; i++)
            {
                result.AllPictureContents.Add(_userLogic.GetImage(category, date, i));
            }
        }

        if (result.Match != null) return result;

        if (guess.GuessCount >= 8)
        {
            result.Match = _databaseRepository.GetForDay(category, date);
            return result;
        }

        result.NextPictureContents = _userLogic.GetImage(category, date, guess.GuessCount);
        result.NextPicture = guess.GuessCount + 1;
        return result;
    }

    [HttpGet("Stream")]
    public IActionResult? GetVideoStream(string category, Guess guess)
    {
        var date = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
        var guessResult = _userLogic.TakeAGuess(category, guess);
        if (!guessResult.InterpretMatch || !guessResult.TitleMatch) return null; // nice try cheating
        var daily = _databaseRepository.GetForDay(category, date);
        if (!System.IO.File.Exists(daily.VideoFile))
        {
            return NotFound();
        }

        var fileInfo = new FileInfo(daily.VideoFile);
        var fileStream = new FileStream(daily.VideoFile, FileMode.Open, FileAccess.Read, FileShare.Read);

        var rangeHeader = Request.Headers.Range.ToString();

        if (!string.IsNullOrEmpty(rangeHeader))
        {
            var range = rangeHeader.Replace("bytes=", "").Split('-');
            var start = long.Parse(range[0]);
            var end = range.Length > 1 && !string.IsNullOrEmpty(range[1])
                ? long.Parse(range[1])
                : fileInfo.Length - 1;

            var length = end - start + 1;

            fileStream.Seek(start, SeekOrigin.Begin);

            Response.StatusCode = 206;
            Response.Headers.Append("Content-Range", $"bytes {start}-{end}/{fileInfo.Length}");
            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.ContentLength = length;

            return File(fileStream, "video/mp4", enableRangeProcessing: true);
        }

        Response.Headers.Append("Accept-Ranges", "bytes");
        return File(fileStream, "video/mp4", enableRangeProcessing: true);
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