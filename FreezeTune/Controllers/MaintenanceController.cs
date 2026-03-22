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
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(IMaintenanceLogic maintenanceLogic, Config config, ProgressService progressService,
        ILogger<MaintenanceController> logger)
    {
        _maintenanceLogic = maintenanceLogic;
        _config = config;
        _progressService = progressService;
        _logger = logger;
    }

    private string? ValidateKey(string? category, string? key)
    {
        if (key == null) throw new Exception("Wrong Key");
        var masterKey = Environment.GetEnvironmentVariable("FREEZEAPIKEY");
        if (key == masterKey) return null;
        if (category == null)
        {
            var matchingCategory = _config.Categories.FirstOrDefault(q => q.Password == key);
            return matchingCategory != null ? matchingCategory.Name : throw new Exception("Wrong Key");
        } 
        
        var categoryConfig = _config.Categories.FirstOrDefault(q => q.Name == category);
        if (categoryConfig == null) throw new Exception("Wrong Category");
        if (categoryConfig.Password == null || categoryConfig.Password != key) throw new Exception("Wrong key");
        return categoryConfig.Name;
    }

    [HttpGet("Date")]
    public Video GetDate(string category)
    {
        return _maintenanceLogic.Init(category);
    }

    [HttpGet("CheckUrl")]
    public ActionResult<string?> CheckUrl(string category, string url)
    {
        var date = _maintenanceLogic.CheckUrl(category, url);
        if (date == null) return Ok(null);
        return Ok(date.Value.ToString("dd.MM.yyyy"));
    }

    [HttpGet("CheckArtistTitle")]
    public ActionResult<string?> CheckArtistTitle(string category, string interpret, string title)
    {
        var date = _maintenanceLogic.CheckArtistTitle(category, interpret, title);
        if (date == null) return Ok(null);
        return Ok(date.Value.ToString("dd.MM.yyyy"));
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
            _logger.LogError(e, "Failed dot download for category '{Category}", category);
            if (sessionId != null) _progressService.Remove(sessionId);
            return new Video { Error = e.Message };
        }
    }

    [HttpGet("All")]
    public ActionResult<Dictionary<string, List<Daily>>> GetAll(string key)
    {
        var category=ValidateKey(null, key);
        
        return Ok(_maintenanceLogic.GetAllEntries().Where(q=>category==null || q.Key==category ).ToDictionary());
    }

    [HttpGet("Progress")]
    public ActionResult<ProgressInfo> GetProgress(string sessionId)
    {
        var progress = _progressService.Get(sessionId);
        if (progress == null) return NotFound();
        return progress;
    }

    [HttpPost("Temp")]
    public Dictionary<int, string> GetTempImages(string apiKey, string category, [FromBody] Video video)
    {
        ValidateKey(category, apiKey);

        return _maintenanceLogic.GetTmpImages(category, video);
    }

    [HttpPost("Store")]
    public bool Store(string apiKey, string category, [FromBody] Video video)
    {
        ValidateKey(category, apiKey);
        _maintenanceLogic.Add(category, video);
        return true;
    }
}