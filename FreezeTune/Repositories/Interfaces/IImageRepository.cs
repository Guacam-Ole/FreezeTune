namespace FreezeTune.Repositories;

public interface IImageRepository
{
    string GetBase64Image(string category, DateOnly date, int number);
}