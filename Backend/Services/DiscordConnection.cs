using System.Text;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using PDPWebsite.Universalis;
using PDPWebsite.Universalis.Models;
using SkiaSharp;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace PDPWebsite.Services;

public class DiscordConnection : IDisposable
{
    public static UniversalisClient UniversalisClient { get; private set; } = null!;
    public DiscordSocketClient DiscordClient { get; }
    private static SocketTextChannel _errorChannel = null!;
    private EnvironmentContainer _environmentContainer;
    private readonly ILogger _logger;
    private const string ItemCountLeft = "With %d more";
    private const string Gil = "<:gil:1077843055941533768>";
    private CancellationTokenSource _cts = new();
    private List<Game> Games { get; } = new()
    {
        new("Universalis", ActivityType.Watching),
        new("with the market"),
        new("with the economy"),
    };

    public DiscordConnection(ILogger<DiscordConnection> logger, EnvironmentContainer environmentContainer)
    {
        UniversalisClient = new UniversalisClient();

        DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        });
        DiscordClient.Log += Log;
        DiscordClient.Ready += Ready;
        DiscordClient.SlashCommandExecuted += SlashCommandExecuted;
        _logger = logger;
        _environmentContainer = environmentContainer;
    }

    public async Task Start()
    {
        await Item.Load(UniversalisClient);
        await DiscordClient.LoginAsync(TokenType.Bot, _environmentContainer.Get("DISCORD_TOKEN"));
        await DiscordClient.StartAsync();
    }

    public async Task SendError(Exception exception)
    {
        await _errorChannel.SendMessageAsync(embed: new EmbedBuilder().WithTitle("Exception").WithDescription(exception.ToString()).Build());
    }

    public async Task Log(LogMessage arg)
    {
        if (arg.Exception != null)
            await SendError(arg.Exception);

        _logger.Log(arg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        }, arg.Exception, arg.Message);
    }

    public async Task SetActivity()
    {
        try
        {
            if (DiscordClient.ConnectionState != ConnectionState.Connected)
            {
                if (_cts.IsCancellationRequested)
                    return;
                await Task.Delay(1000, _cts.Token);
                SetActivity();
            }

            var next = Games[Random.Shared.Next(Games.Count)];
            await DiscordClient.SetActivityAsync(next);
            await Task.Delay(60000, _cts.Token);
            SetActivity();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task Ready()
    {
        try
        {
            foreach (var discordClientGuild in DiscordClient.Guilds)
            {
                var commands = await DiscordClient.Rest.GetGuildApplicationCommands(discordClientGuild.Id);
                foreach (var restGuildCommand in commands)
                {
                    await restGuildCommand.DeleteAsync();
                }
            }
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            await Log(new LogMessage(LogSeverity.Error, "UniversalisBot", json, exception));
        }

        SetActivity();
        _errorChannel = (SocketTextChannel)await DiscordClient.GetChannelAsync(1156096156124844084);

        var commandBuilder = new SlashCommandBuilder()
            .WithName("market")
            .WithDescription("Market related commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("search")
                .WithDescription("Searches the market for an item")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("item")
                    .WithDescription("The item to search for")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("server")
                    .WithDescription("The server to search on")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("error-bars")
                    .WithDescription("Shows error bars in the graph")
                    .WithType(ApplicationCommandOptionType.Boolean)))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("tax")
                .WithDescription("Gets the current market board tax")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("server")
                    .WithDescription("The server to get the tax for")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true)))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("worlds")
                .WithDescription("Lists out the datacenters and worlds")
                .WithType(ApplicationCommandOptionType.SubCommand));


        try
        {
            foreach (var discordClientGuild in DiscordClient.Guilds)
            {
                await DiscordClient.Rest.CreateGuildCommand(commandBuilder.Build(), discordClientGuild.Id);
            }
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            await Log(new LogMessage(LogSeverity.Error, "UniversalisBot", json, exception));
        }
    }

    private async Task SlashCommandExecuted(SocketSlashCommand arg)
    {
        var data = arg.Data.Options.First();
        await arg.RespondAsync("Thinking...");
        World[]? worlds;
        Datacenter[]? datacenters;
        StringBuilder? sb;
        try
        {
            _logger.Log(LogLevel.Trace, $"Got {data.Name} command from {arg.User.Username}#{arg.User.Discriminator} ({arg.User.Id})");
            switch (data.Name)
            {
                case "search":
                    {
                        worlds = await UniversalisClient.GetWorlds();
                        datacenters = await UniversalisClient.GetDatacenters();
                        var names = new List<string>();
                        foreach (var datacenter in datacenters)
                        {
                            if (!datacenter.Worlds.All(x => x < 1000)) continue;
                            names.Add(datacenter.Name);
                            names.Add(datacenter.Region);
                            names.AddRange(datacenter.Worlds.Select(x => worlds.First(t => t.Id == x).Name));
                        }
                        var item = data.Options.First(t => t.Name == "item").Value as string;
                        var server = data.Options.First(t => t.Name == "server").Value as string;
                        _logger.Log(LogLevel.Trace, $"Trying with server: {server} and item: {item}");
                        if (!names.Any(t => string.Equals(t, server, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find server {server}");
                            break;
                        }

                        server = names.First(t => string.Equals(t, server, StringComparison.InvariantCultureIgnoreCase));
                        var itemDatas = Item.Items.Where(t => t.Name.Contains(item!, StringComparison.InvariantCultureIgnoreCase)).ToList();
                        if (!itemDatas.Any())
                        {
                            await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find item {item}");
                            break;
                        }
                        _logger.Log(LogLevel.Trace, $"Found server and item count {itemDatas.Count}");
                        var builder = new EmbedBuilder();
                        sb = new StringBuilder();
                        if (itemDatas.Count > 1)
                        {
                            for (var i = 0; i < itemDatas.Count; i++)
                            {
                                var countLeft = ItemCountLeft.Replace("%d", $"{itemDatas.Count - i}");
                                var str = $"`{i + 1}` {itemDatas[i].Name}";
                                sb.AppendLine(sb.Length + str.Length < 4096 - countLeft.Length ? str : countLeft);
                            }

                            builder.WithTitle($"Multiple items with name `{item}`");
                            builder.WithDescription(sb.ToString());
                            await arg.ModifyOriginalResponseAsync(msg =>
                            {
                                msg.Content = null;
                                msg.Embed = builder.Build();
                            });
                            break;
                        }
                        _logger.Log(LogLevel.Trace, $"Found item {itemDatas[0].Name}");
                        var itemData = itemDatas.First();
                        var listing = await UniversalisClient.GetListing(server, itemData.Id);
                        var history = await UniversalisClient.GetHistory(server, itemData.Id);
                        _logger.Log(LogLevel.Trace, $"Got listing and history");
                        var priceHistory = history.GetPriceHistory();
                        var hplt = history.GetPlot(data.Options.FirstOrDefault(t => t.Name == "error-bars")?.Value as bool? ?? false).GetImage(1100, 400);
                        var mplt = listing.GetPlot().GetImage(1100, 200);
                        var plt = new SKBitmap(1100, 600);
                        using (var canvas = new SKCanvas(plt))
                        {
                            canvas.DrawImage(SKImage.FromEncodedData(hplt.GetImageBytes()), 0, 0);
                            canvas.DrawImage(SKImage.FromEncodedData(mplt.GetImageBytes()), 0, 400);
                        }

                        var stream = new MemoryStream();
                        plt.Encode(SKEncodedImageFormat.Png, 80).SaveTo(stream);
                        sb.AppendLine($"**Current Average Price**: {listing.CurrentAveragePrice}");
                        sb.AppendLine($"**Historical Average Price**: {priceHistory.AveragePrice}");
                        sb.AppendLine($"**Average NQ**: {listing.CurrentAveragePriceNq}");
                        sb.AppendLine($"**Historical Average NQ**: {priceHistory.AveragePriceNq ?? 0}");
                        sb.AppendLine($"**Average HQ**: {listing.CurrentAveragePriceHq}");
                        sb.AppendLine($"**Historical Average HQ**: {priceHistory.AveragePriceHq ?? 0}");
                        builder.WithTitle($"Prices for {itemData.Name} on {server}");
                        builder.WithColor(Color.Teal);
                        builder.WithDescription(sb.ToString());
                        sb.Clear();
                        if (listing.Listings.Count > 0)
                        {
                            var gil = new StringBuilder();
                            var total = new StringBuilder();
                            foreach (var listingView in listing.Listings.Take(10))
                            {
                                gil.AppendLine(listingView.PricePerUnit + Gil);
                                sb.AppendLine($"x{listingView.Quantity} {(!string.IsNullOrWhiteSpace(listingView.WorldName) ? $"[**{listingView.WorldName ?? server}**]" : "")}");
                                total.AppendLine(listingView.Total + Gil);
                            }

                            builder.AddField("Price per unit", gil.ToString(), true);
                            builder.AddField("Quantity", sb.ToString(), true);
                            builder.AddField("Total price", total.ToString(), true);
                        }
                        else
                        {
                            builder.AddField("No listings", "No listings found for this item");
                        }
                        builder.WithImageUrl("attachment://plot.png");
                        var attachments = new List<FileAttachment>
                    {
                        new(stream, "plot.png")
                    };
                        using var iconStream = new MemoryStream();
                        var icon = itemData.GetIconTexture();
                        if (icon != null)
                        {
                            icon.Encode(SKEncodedImageFormat.Png, 80).SaveTo(iconStream);
                            builder.WithThumbnailUrl("attachment://icon.png");
                            attachments.Add(new FileAttachment(iconStream, "icon.png"));
                        }
                        _logger.Log(LogLevel.Trace, "Updating message with embed");
                        await arg.ModifyOriginalResponseAsync(msg =>
                        {
                            msg.Content = null;
                            msg.Embed = builder.Build();
                            msg.Attachments = attachments;
                        });
                        break;
                    }
                case "tax":
                    worlds = await UniversalisClient.GetWorlds();
                    if (worlds == null || !worlds.Any())
                    {
                        await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not retrieve worlds.");
                        break;
                    }

                    var serverInp = data.Options.First(j => j.Name == "server").Value as string;
                    _logger.Log(LogLevel.Trace, $"Trying with server {serverInp}");
                    if (!worlds.Any(t => string.Equals(t.Name, serverInp, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"No world exists of name: {serverInp}");
                        break;
                    }
                    var world = worlds.First(t => string.Equals(t.Name, serverInp, StringComparison.InvariantCultureIgnoreCase));
                    _logger.Log(LogLevel.Trace, $"Found server {world.Name}");
                    var tax = await UniversalisClient.GetTaxRates(world.Id);
                    _logger.Log(LogLevel.Trace, $"Got tax data: {tax}");
                    var embedBuilder = new EmbedBuilder();
                    embedBuilder.WithTitle($"Market tax rates for: {world.Name}");
                    embedBuilder.AddField("Limsa Lominsa", $"{tax.LimsaLominsa}%", true);
                    embedBuilder.AddField("Gridania", $"{tax.Gridania}%", true);
                    embedBuilder.AddField("Ul'dah", $"{tax.Uldah}%", true);
                    embedBuilder.AddField("Ishgard", $"{tax.Ishgard}%", true);
                    embedBuilder.AddField("Kugane", $"{tax.Kugane}%", true);
                    embedBuilder.AddField("Crystarium", $"{tax.Crystarium}%", true);
                    embedBuilder.AddField("Old Sharlayan", $"{tax.OldSharlayan}%", true);
                    await arg.ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Content = null;
                        msg.Embed = embedBuilder.Build();
                    });
                    break;
                case "worlds":
                    _logger.Log(LogLevel.Trace, "Getting worlds");
                    worlds = await UniversalisClient.GetWorlds();
                    datacenters = await UniversalisClient.GetDatacenters();
                    if (worlds == null || !worlds.Any() || datacenters == null || !datacenters.Any())
                    {
                        await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not retrieve datacenters or worlds.");
                        break;
                    }
                    _logger.Log(LogLevel.Trace, "Filtering worlds");
                    var serverMap = datacenters.Where(t => t.Worlds.Any(k => k < 1000)).Select(t => new { t.Name, t.Region, Worlds = t.Worlds.Select(k => worlds.First(j => j.Id == k)).ToArray() }).GroupBy(t => t.Region).ToArray();
                    sb = new StringBuilder();
                    var embeds = new List<Embed>();
                    foreach (var grouping in serverMap)
                    {
                        var embed = new EmbedBuilder();
                        embed.WithTitle($"Worlds in `{grouping.Key}`");
                        embed.WithColor(Color.Teal);
                        foreach (var datacenter in grouping)
                        {
                            foreach (var w in datacenter.Worlds)
                            {
                                sb.AppendLine($"{w.Name}");
                            }

                            embed.AddField(datacenter.Name, sb.ToString(), true);
                            sb.Clear();
                        }
                        embeds.Add(embed.Build());
                    }
                    _logger.Log(LogLevel.Trace, "Built datacenter embeds");
                    await arg.ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Content = null;
                        msg.Embeds = embeds.ToArray();
                    });
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Error, e, "Error in slash command");
            await arg.ModifyOriginalResponseAsync(msg => msg.Content = $"An error occurred. Notified mods.");
        }
    }

    public async Task DisposeAsync()
    {
        _cts.Cancel();
        await DiscordClient.StopAsync();
        await DiscordClient.LogoutAsync();
        await DiscordClient.DisposeAsync();
        UniversalisClient.Dispose();
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}