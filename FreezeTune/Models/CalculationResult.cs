namespace FreezeTune.Models;

public class CalculationResult
{
    public int LevenshteinTitle { get; set; }
    public int LevenshteinInterpret { get; set; }
    public bool TitleMatch { get; set; }
    public bool InterpretMatch { get; set; }
    public Daily? Match { get; set; }
    public string? Interpret { get; set; }
}