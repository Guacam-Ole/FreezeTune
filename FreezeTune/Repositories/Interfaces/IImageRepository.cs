namespace FreezeTune.Repositories;

public interface IImageRepository
{
    string GetBase64Image(string category, DateOnly date, int number);
    Dictionary<int, string> GetTempImages(string category, DateOnly date, int maxImages);
}