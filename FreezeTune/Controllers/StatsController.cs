using FreezeTune.Models;
using FreezeTune.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FreezeTune.Controllers;

[Route("Stats")]
public class StatsController:ControllerBase
{
    private readonly IDatabaseRepository _databaseRepositor;

    public StatsController(IDatabaseRepository databaseRepositor)
    {
        _databaseRepositor = databaseRepositor;
    }
    
    [HttpGet]
    public List<Stats> GetMonthlyStats(string category)
    {
        return _databaseRepositor.GetMonthlyStats(category);
    }
}