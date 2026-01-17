using ImageMagick;

namespace FreezeTune.Repositories;

public class ImageRepository : IImageRepository
{
    private readonly Config _config;

    public ImageRepository(Config config)
    {
        _config = config;
    }

    public string GetBase64Image(string category, DateOnly date, int number)
    {
        return GetBase64Image(category, date, number, "img");
    }

    private string GetBase64Image(string category, DateOnly date, int number, string subDirectory)
    {
        using var image = new MagickImage($"{_config.BasePath}/{subDirectory}/{category}-{date:yyyy-MM-dd}-{number}.png");
        image.Resize(_config.Width, _config.Height);
        var imgBase64 = image.ToBase64();
        return imgBase64;
    }

    public Dictionary<int, string> GetTempImages(string category, DateOnly date, int maxImages)
    {
        var allImages = new Dictionary<int, string>();
        for (var i = 0; i < maxImages; i++)
        {
            allImages.Add(i, GetBase64Image(category,date,i,"tmp"));
        }

        return allImages;
    }
}