using FreezeTune.Models;

namespace FreezeTune.Logic;

public interface IUserLogic
{
    string GetImage(string category, DateOnly date, int currentNumber);
    CalculationResult TakeAGuess(string category, Guess guess);
    bool ValuesAreCorrect(string category, string interpret, string title);

}