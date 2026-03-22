using System.Reflection;
using FreezeTune;
using Microsoft.Extensions.Options;
using FreezeTune.Logic;
using FreezeTune.Repositories;
using FreezeTune.Services;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);
builder.Services.AddLogging(cfg => cfg.SetMinimumLevel(LogLevel.Debug));
builder.Services.AddSerilog(cfg =>
{
    cfg.MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("job", Assembly.GetEntryAssembly()?.GetName().Name)
        .Enrich.WithProperty("desktop", Environment.GetEnvironmentVariable("DESKTOP_SESSION"))
        .Enrich.WithProperty("language", Environment.GetEnvironmentVariable("LANGUAGE"))
        .Enrich.WithProperty("lc", Environment.GetEnvironmentVariable("LC_NAME"))
        .Enrich.WithProperty("timezone", Environment.GetEnvironmentVariable("TZ"))
        .Enrich.WithProperty("dotnetVersion", Environment.GetEnvironmentVariable("DOTNET_VERSION"))
        .Enrich.WithProperty("inContainer", Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"))
        .WriteTo.GrafanaLoki(Environment.GetEnvironmentVariable("LOKIURL") ?? "http://thebeast:3100",
            propertiesAsLabels: ["job"]);
    cfg.WriteTo.Console(); //new  StringOutputFormatter() RenderedCompactJsonFormatter());
});
builder.Services.Configure<Config>(builder.Configuration);
builder.Services.AddSingleton<Config>(sp => sp.GetRequiredService<IOptions<Config>>().Value);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IMusicSearchRepository, MusicSearchRepository>();
builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<IDatabaseRepository, DatabaseRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<IMaintenanceLogic, MaintenanceLogic>();
builder.Services.AddSingleton<ProgressService>();
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddHostedService<MetricsBackgroundService>();

Console.WriteLine($"Application started at {Environment.GetEnvironmentVariable("ASPNETCORE_URLS")}");
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        ctx.Context.Response.Headers.Pragma = "no-cache";
        ctx.Context.Response.Headers.Expires = "0";
    }
});

app.UseAuthorization();
app.UseHttpMetrics();

app.MapControllers();
app.MapMetrics();

app.Run();