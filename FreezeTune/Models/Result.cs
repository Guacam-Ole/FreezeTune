namespace FreezeTune.Models;

public class Result
{
    public bool InterpretCorrect { get; set; }
    public bool TitleCorrect { get; set; }
    public int? NextPicture { get; set; }
    public int Guesses { get; set; }
    public Daily? Match { get; set; }
    public string NextPictureContents { get; set; } = string.Empty;
    public string? Interpret { get; set; }
    public List<string>? AllPictureContents { get; set; }
}