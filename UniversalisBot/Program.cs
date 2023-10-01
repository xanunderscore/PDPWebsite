using Microsoft.EntityFrameworkCore;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Starting up");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddSingleton<EnvironmentContainer>();
builder.Services.AddSingleton<DiscordConnection>();
builder.Services.AddDbContext<DB>(conf => conf.UseNpgsql("Database=PDP"));
builder.Services.AddSpaStaticFiles(opt => opt.RootPath = "src");
builder.Logging.ClearProviders();
builder.Host.UseNLog();

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();
app.UseSpaStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
await app.Services.GetRequiredService<DiscordConnection>().Start();
app.UseSpa(opt =>
{
    opt.Options.SourcePath = "src";
    if (app.Environment.IsDevelopment())
        opt.UseProxyToSpaDevelopmentServer("http://localhost:3000");
});
app.Run();

LogManager.Shutdown();