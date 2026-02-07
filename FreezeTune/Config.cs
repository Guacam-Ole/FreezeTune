using FreezeTune.Models;

namespace FreezeTune;

public class Config
{
    public string BasePath { get; set; } = "../../../..";
    public uint Width { get; set; } = 1920;
    public uint Height { get; set; } = 1080;
    public double MaxDistance { get; set; } = 3;

    public List<Category> Categories { get; set; } = [];
}