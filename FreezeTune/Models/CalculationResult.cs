namespace FreezeTune.Models;

public class CalculationResult
{
    public bool TitleMatch { get; set; }
    public bool InterpretMatch { get; set; }
    public Daily? Match { get; set; }
    public string? Interpret { get; set; }
}