using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Starting up");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddControllersWithViews();
builder.Services.AddCors();
if (!builder.Environment.IsDevelopment())
    builder.Services.AddSpaStaticFiles(opt => opt.RootPath = "src");
builder.Services.AddSingleton<EnvironmentContainer>();
builder.Services.AddSingleton<DiscordConnection>();
builder.Services.AddSingleton<RedisClient>();
builder.Services.AddDbContext<DB>(conf => conf.UseNpgsql("Database=PDP"));
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSpaStaticFiles();
}
app.UseRouting();
app.UseCors(o => o.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");
await app.Services.GetRequiredService<DiscordConnection>().Start();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (!app.Environment.IsDevelopment())
    app.UseSpa(opt =>
    {
        opt.Options.SourcePath = "src";
    });
app.Run();

LogManager.Shutdown();

public class CorsHeaderAttribute : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "*");
        context.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "*");

        base.OnResultExecuting(context);
    }
}