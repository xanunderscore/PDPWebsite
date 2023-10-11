using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NLog.Web;
using PDPWebsite.Hubs;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Starting up");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.Converters.Add(new UlongStringConverter());
});
builder.Services.AddRouting(o => o.LowercaseUrls = true);
builder.Services.AddSingleton<EnvironmentContainer>();
builder.Services.AddSingleton<UniversalisClient>();
builder.Services.AddSingleton<GameClient>();
builder.Services.AddSingleton<DiscordConnection>();
builder.Services.AddSingleton<RedisClient>();
builder.Services.AddScoped<AuthFilter>();
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
            Array.Empty<string>()
        }
    });
});
builder.Services.AddSignalR().AddJsonProtocol(opt =>
{
    opt.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opt.PayloadSerializerOptions.Converters.Add(new UlongStringConverter());
});
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(b =>
    {
        b.WithOrigins("http://localhost:3000").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

var app = builder.Build();

app.UseRouting();
app.UseCors();
app.UseMiddleware<CorsMiddleware>();
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
app.MapHub<MainHub>("/sigr");
app.Run();

LogManager.Shutdown();

public class UlongStringConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ulong.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}