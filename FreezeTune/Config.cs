namespace FreezeTune;

public class Config
{
    public string BasePath { get; set; } = "../../../..";
    public uint Width { get; set; } = 640;
    public uint Height { get; set; } = 480;
    public uint MaxDistance { get; set; } = 3;
    
    public List<string> Categories { get; set; }= ["80s"];
    public Dictionary<string, string>? CategoryKeys { get; set; } = null;
}