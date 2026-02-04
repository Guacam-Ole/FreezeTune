using FreezeTune.Logic;
using FreezeTune.Models;
using FreezeTune.Services;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

[Route("Maintenance")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceLogic _maintenanceLogic;
    private readonly Config _config;
    private readonly ProgressService _progressService;

    public MaintenanceController(IMaintenanceLogic maintenanceLogic, Config config, ProgressService progressService)
    {
        _maintenanceLogic = maintenanceLogic;
        _config = config;
        _progressService = progressService;
    }

    private void ValidateKey(string category, string key)
    {
        if (!_config.Categories.Contains(category)) throw new Exception("Wrong Catgory");
        var masterKey = Environment.GetEnvironmentVariable("FREEZEAPIKEY");
        if (key == masterKey) return;
        if (_config.CategoryKeys==null || !_config.CategoryKeys.TryGetValue(category, out var value)) throw new Exception("Wrong key");
        if (value != key) throw new Exception("Wrong Key"); 
    }

    [HttpGet("Date")]
    public Video GetDate(string category)
    {
         return _maintenanceLogic.Init(category);
    }

    [HttpPost("Download")]
    public async Task<Video> Download(string apiKey, string category, string? sessionId, [FromBody] Video video)
    {
        try
        {
            ValidateKey(category, apiKey);

            var result = await _maintenanceLogic.Download(category, video, sessionId);
            if (sessionId != null) _progressService.Remove(sessionId);
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            if (sessionId != null) _progressService.Remove(sessionId);
            return new Video { Error = e.Message };
        }
    }

    [HttpGet("Progress")]
    public ActionResult<ProgressInfo> GetProgress(string sessionId)
    {
        var progress = _progressService.Get(sessionId);
        if (progress == null) return NotFound();
        return progress;
    }

    [HttpPost("Temp")]
    public Dictionary<int,string> GetTempImages(string apiKey, string category, [FromBody] Video video)
    {
        ValidateKey(category, apiKey);

        return _maintenanceLogic.GetTmpImages(category, video);
    }
    
    [HttpPost("Store")]
    public bool Store(string apiKey, string category, [FromBody] Video video)
    {
        
        ValidateKey(category, apiKey);
        _maintenanceLogic.Add(category, video) ;
        return true;
    }
}