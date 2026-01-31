using FreezeTune.Models;

namespace FreezeTune;

public class Config
{
    public string BasePath { get; set; } = "../../../..";
    public uint Width { get; set; } = 1920;
    public uint Height { get; set; } = 1080;
    public uint MaxDistance { get; set; } = 3;

    public List<string> Categories { get; set; } = ["80s", "90s", "Hamburg"];
    public Dictionary<string, string>? CategoryKeys { get; set; } = null;
}