using FreezeTune.Logic;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

[Route("Maintenance")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceLogic _maintenanceLogic;
    private readonly Config _config;

    public MaintenanceController(IMaintenanceLogic maintenanceLogic, Config config)
    {
        _maintenanceLogic = maintenanceLogic;
        _config = config;
    }

    private void ValidateKey(string category, string key)
    {
        if (!_config.Categories.Contains(category)) throw new Exception("Wrong Catgory");
        var masterKey = Environment.GetEnvironmentVariable("FREEZEAPIKEY");
        if (key == masterKey) return;
        if (_config.CategoryKeys==null || !_config.CategoryKeys.ContainsKey(category)) return;
        if (_config.CategoryKeys[category] != key) throw new Exception("Wrong Key");
    }

    [HttpGet("Date")]
    public Models.Video GetDate(string category)
    {
        return _maintenanceLogic.Init(category);
    }

    [HttpPost("Download")]
    public async Task<Models.Video> Download(string apiKey, string category, [FromBody] Models.Video video)
    {
        try
        {
            ValidateKey(category, apiKey);

            return await _maintenanceLogic.Download(category, video);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpPost("Temp")]
    public Dictionary<int,string> GetTempImages(string apiKey, string category, [FromBody] Models.Video video)
    {
        ValidateKey(category, apiKey);

        return _maintenanceLogic.GetTmpImages(category, video);
    }
    
    [HttpPost("Store")]
    public bool Store(string apiKey, string category, [FromBody] Models.Video video)
    {
        ValidateKey(category, apiKey);
        _maintenanceLogic.Add(category, video) ;
        return true;
    }
}