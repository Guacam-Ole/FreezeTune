using FreezeTune.Models;
using FreezeTune.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

[Route("Stats")]
public class StatsController:ControllerBase
{
    private readonly IDatabaseRepository _databaseRepositor;
    private readonly Config _config;

    public StatsController(IDatabaseRepository databaseRepositor, Config config)
    {
        _databaseRepositor = databaseRepositor;
        _config = config;
    }
    
    [HttpGet]
    public List<Stats> GetMonthlyStats(string category)
    {
        return !_config.Categories.Contains(category) ? throw new Exception("Unknown category") : _databaseRepositor.GetMonthlyStats(category);
    }
}