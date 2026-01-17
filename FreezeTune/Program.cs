using System.Text.Json;
using FreezeTune;
using FreezeTune.Logic;
using FreezeTune.Repositories;

var builder = WebApplication.CreateBuilder(args);


var configFile = File.ReadAllText("config.json");
var cfg =  JsonSerializer.Deserialize<Config> (configFile);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<Config>(cfg);
builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<IDatabaseRepository, DatabaseRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IYoutubeRepository, YoutubeRepository>();
builder.Services.AddScoped<IMaintenanceLogic, MaintenanceLogic>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();