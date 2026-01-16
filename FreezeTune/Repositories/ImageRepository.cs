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
        using var image = new MagickImage($"{_config.BasePath}/{category}-{date:yyyy-MM-dd}-{number}.png");
        image.Resize(_config.Width, _config.Height);
        var imgBase64 = image.ToBase64();
        return imgBase64;
    }
}