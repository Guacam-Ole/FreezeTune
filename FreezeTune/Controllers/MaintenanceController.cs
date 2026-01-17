using FreezeTune.Logic;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

 [Microsoft.AspNetCore.Components.Route("Maintenance")]
public class MaintenanceController
{
    private readonly IMaintenanceLogic _maintenanceLogic;

    public MaintenanceController(IMaintenanceLogic maintenanceLogic)
    {
        _maintenanceLogic = maintenanceLogic;
    }

    private bool IsValidKey(string key)
    {
        return key == Environment.GetEnvironmentVariable("FREEZEAPIKEY");
    }
    
    [HttpGet("Date")]
    public Models.Video GetDate(string category)
    {
        return _maintenanceLogic.Init(category);
    }

    [HttpGet("Download")]
    public async Task<Models.Video> Download(string apiKey, string category,Models.Video video)
    {
        if (!IsValidKey(apiKey)) throw new Exception("Wrong key");
        
        return await _maintenanceLogic.Download(category, video);
    }

    [HttpPost]
    public bool Store(string apiKey, string category, Models.Video video)
    {
        if (!IsValidKey(apiKey)) throw new Exception("Wrong key");
        _maintenanceLogic.Add(category, video) ;
        return true;
    }
}