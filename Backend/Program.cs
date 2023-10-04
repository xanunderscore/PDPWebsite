using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Starting up");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddControllers();
builder.Services.AddRouting(o => o.LowercaseUrls = true);
builder.Services.AddCors();
if (!builder.Environment.IsDevelopment())
    builder.Services.AddSpaStaticFiles(opt => opt.RootPath = "src");
builder.Services.AddSingleton<EnvironmentContainer>();
builder.Services.AddSingleton<DiscordConnection>();
builder.Services.AddSingleton<RedisClient>();
builder.Services.AddDbContext<Database>(conf => conf.UseNpgsql("Database=pdp;Username=postgres;Password=postgres;Host=localhost;"));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Version = "v1", Description = "PDPWebsite API surface" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"GUID Token of the current logged in discord user",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSpaStaticFiles();
}
app.UseRouting();
app.UseMiddleware<CorsMiddleware>();
app.UseMiddleware<AuthMiddleware>();
app.UseMiddleware<OptionsMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");
await app.Services.GetRequiredService<DiscordConnection>().Start();
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<Database>().Database.MigrateAsync();
}
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