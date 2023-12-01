using System.Text;
using Discord;
using Discord.WebSocket;
using SkiaSharp;

namespace PDPWebsite.Discord;

[SlashCommand("market", "Market related commands")]
[AllowedChannel(1065695099285155980)]
public class Market : ISlashCommandProcessor
{
    private const string ItemCountLeft = "With %d more";
    private const string Gil = "<:gil:1077843055941533768>";
    private readonly SocketSlashCommand _arg;
    private readonly GameClient _gameClient;
    private readonly ILogger<Market> _logger;
    private readonly UniversalisClient _universalisClient;

    public Market(UniversalisClient universalisClient, ILogger<Market> logger, GameClient gameClient,
        SocketSlashCommand arg)
    {
        _universalisClient = universalisClient;
        _gameClient = gameClient;
        _logger = logger;
        _arg = arg;
    }

    [SlashCommand("search", "Searches the market for an item")]
    public async Task Search([SlashCommand("item", "The item to search for")] string item,
        [SlashCommand("server", "The server to search on")]
        string server,
        [SlashCommand("error-bars", "Shows error bars in the graph")]
        bool? errorBars)
    {
        var worlds = await _universalisClient.GetWorlds();
        var datacenters = await _universalisClient.GetDatacenters();
        var names = new List<string>();
        foreach (var datacenter in datacenters)
        {
            if (!datacenter.Worlds.All(x => x < 1000)) continue;
            names.Add(datacenter.Name);
            names.Add(datacenter.Region);
            names.AddRange(datacenter.Worlds.Select(x => worlds.First(t => t.Id == x).Name));
        }

        _logger.LogTrace("Trying with server: {server} and item: {item}", server, item);
        if (!names.Any(t => string.Equals(t, server, StringComparison.InvariantCultureIgnoreCase)))
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find server {server}");
            return;
        }

        server = names.First(t => string.Equals(t, server, StringComparison.InvariantCultureIgnoreCase));
        var itemDatas = _gameClient.MarketItems
            .Where(t => t.Name.Contains(item!, StringComparison.InvariantCultureIgnoreCase)).ToList();
        if (!itemDatas.Any())
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find item {item}");
            return;
        }

        _logger.LogTrace("Found server and item count {count}", itemDatas.Count);
        var builder = new EmbedBuilder();
        var sb = new StringBuilder();
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
            await _arg.ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = null;
                msg.Embed = builder.Build();
            });
            return;
        }

        _logger.LogTrace("Found item {name}", itemDatas[0].Name);
        var itemData = itemDatas.First();
        var listing = await _universalisClient.GetListing(server, itemData.Id);
        var history = await _universalisClient.GetHistory(server, itemData.Id);
        _logger.LogTrace("Got listing and history");
        var priceHistory = history.GetPriceHistory();
        var hPlt = history.GetPlot(_gameClient.MarketItems, errorBars ?? false).GetImage(1100, 400);
        var mPlt = listing.GetPlot().GetImage(1100, 200);
        var plt = new SKBitmap(1100, 600);
        using (var canvas = new SKCanvas(plt))
        {
            canvas.DrawImage(SKImage.FromEncodedData(hPlt.GetImageBytes()), 0, 0);
            canvas.DrawImage(SKImage.FromEncodedData(mPlt.GetImageBytes()), 0, 400);
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
                sb.AppendLine(
                    $"x{listingView.Quantity} {(!string.IsNullOrWhiteSpace(listingView.WorldName) ? $"[**{listingView.WorldName ?? server}**]" : "")}");
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
        var attachments = new List<FileAttachment> { new(stream, "plot.png") };
        using var iconStream = new MemoryStream();
        var icon = itemData.GetIconTexture();
        if (icon != null)
        {
            icon.Encode(SKEncodedImageFormat.Png, 80).SaveTo(iconStream);
            builder.WithThumbnailUrl("attachment://icon.png");
            attachments.Add(new FileAttachment(iconStream, "icon.png"));
        }

        _logger.LogTrace("Updating message with embed");
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = builder.Build();
            msg.Attachments = attachments;
        });
    }

    [SlashCommand("worlds", "Lists out the datacenters and worlds")]
    public async Task Worlds()
    {
        var worlds = await _universalisClient.GetWorlds();
        var datacenters = await _universalisClient.GetDatacenters();
        if (!worlds.Any() || !datacenters.Any())
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Could not retrieve datacenters or worlds.");
            return;
        }

        _logger.LogTrace("Filtering worlds");
        var serverMap = datacenters.Where(t => t.Worlds.Any(k => k < 1000)).Select(t =>
                new { t.Name, t.Region, Worlds = t.Worlds.Select(k => worlds.First(j => j.Id == k)).ToArray() })
            .GroupBy(t => t.Region).ToArray();
        var sb = new StringBuilder();
        var embeds = new List<Embed>();
        foreach (var grouping in serverMap)
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"Worlds in `{grouping.Key}`");
            embed.WithColor(Color.Teal);
            foreach (var datacenter in grouping)
            {
                foreach (var w in datacenter.Worlds) sb.AppendLine($"{w.Name}");

                embed.AddField(datacenter.Name, sb.ToString(), true);
                sb.Clear();
            }

            embeds.Add(embed.Build());
        }

        _logger.LogTrace("Built datacenter embeds");
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embeds = embeds.ToArray();
        });
    }

    [SlashCommand("tax", "Gets the current market board tax")]
    public async Task Tax([SlashCommand("server", "The server to check on")] string server)
    {
        var worlds = await _universalisClient.GetWorlds();
        if (!worlds.Any())
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Could not retrieve worlds.");
            return;
        }

        _logger.LogTrace("Trying with server {server}", server);
        if (!worlds.Any(t => string.Equals(t.Name, server, StringComparison.InvariantCultureIgnoreCase)))
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"No world exists of name: {server}");
            return;
        }

        var world = worlds.First(t => string.Equals(t.Name, server, StringComparison.InvariantCultureIgnoreCase));
        _logger.LogTrace("Found server {name}", world.Name);
        var tax = await _universalisClient.GetTaxRates(world.Id);
        _logger.LogTrace("Got tax data: {tax}", tax);
        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithTitle($"Market tax rates for: {world.Name}");
        embedBuilder.AddField("Limsa Lominsa", $"{tax.LimsaLominsa}%", true);
        embedBuilder.AddField("Gridania", $"{tax.Gridania}%", true);
        embedBuilder.AddField("Ul'dah", $"{tax.Uldah}%", true);
        embedBuilder.AddField("Ishgard", $"{tax.Ishgard}%", true);
        embedBuilder.AddField("Kugane", $"{tax.Kugane}%", true);
        embedBuilder.AddField("Crystarium", $"{tax.Crystarium}%", true);
        embedBuilder.AddField("Old Sharlayan", $"{tax.OldSharlayan}%", true);
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = embedBuilder.Build();
        });
    }
}