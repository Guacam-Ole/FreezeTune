using FreezeTune.Logic;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

[Route("Maintenance")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceLogic _maintenanceLogic;

    public MaintenanceController(IMaintenanceLogic maintenanceLogic)
    {
        _maintenanceLogic = maintenanceLogic;
    }

    private bool IsValidKey(string key)
    {
        var requiredKey = Environment.GetEnvironmentVariable("FREEZEAPIKEY");
        return key == requiredKey;
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
            if (!IsValidKey(apiKey)) throw new Exception("Wrong key");

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
        if (!IsValidKey(apiKey)) throw new Exception("Wrong key");

        return _maintenanceLogic.GetTmpImages(category, video);
    }
    
    [HttpPost("Store")]
    public bool Store(string apiKey, string category, [FromBody] Models.Video video)
    {
        if (!IsValidKey(apiKey)) throw new Exception("Wrong key");
        _maintenanceLogic.Add(category, video) ;
        return true;
    }
}