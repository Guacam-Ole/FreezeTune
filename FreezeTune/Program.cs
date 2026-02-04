using FreezeTune;
using Microsoft.Extensions.Options;
using FreezeTune.Logic;
using FreezeTune.Repositories;
using FreezeTune.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);
builder.Services.Configure<Config>(builder.Configuration);
builder.Services.AddSingleton<Config>(sp => sp.GetRequiredService<IOptions<Config>>().Value);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<IDatabaseRepository, DatabaseRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<IMaintenanceLogic, MaintenanceLogic>();
builder.Services.AddSingleton<ProgressService>();

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

app.MapControllers();

app.Run();