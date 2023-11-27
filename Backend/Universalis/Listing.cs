using Newtonsoft.Json;
using ScottPlot;
using ScottPlot.Plottables;

namespace PDPWebsite.Universalis;

public record Listing
{
    /// <summary>
    ///     The item ID.
    /// </summary>
    [JsonProperty("itemID")]
    public required int ItemId { get; init; }

    /// <summary>
    ///     The world ID, if applicable.
    /// </summary>
    [JsonProperty("worldID")]
    public required int? WorldId { get; init; }

    /// <summary>
    ///     The last upload time for this endpoint, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonProperty("lastUploadTime")]
    public required long LastUploadTimeUnixMilliseconds { get; set; }

    /// <summary>
    ///     The currently-shown listings.
    /// </summary>
    [JsonProperty("listings")]
    public required List<ListingView> Listings { get; set; } = new();

    /// <summary>
    ///     The currently-shown sales.
    /// </summary>
    [JsonProperty("recentHistory")]
    public required List<SaleView> RecentHistory { get; set; } = new();

    /// <summary>
    ///     The DC name, if applicable.
    /// </summary>
    [JsonProperty("dcName")]
    public required string DcName { get; init; }

    /// <summary>
    ///     The region name, if applicable.
    /// </summary>
    [JsonProperty("regionName")]
    public required string RegionName { get; init; }

    /// <summary>
    ///     The average listing price, with outliers removed beyond 3 standard deviations of the mean.
    /// </summary>
    [JsonProperty("currentAveragePrice")]
    public required float CurrentAveragePrice { get; set; }

    /// <summary>
    ///     The average NQ listing price, with outliers removed beyond 3 standard deviations of the mean.
    /// </summary>
    [JsonProperty("currentAveragePriceNQ")]
    public required float CurrentAveragePriceNq { get; set; }

    /// <summary>
    ///     The average HQ listing price, with outliers removed beyond 3 standard deviations of the mean.
    /// </summary>
    [JsonProperty("currentAveragePriceHQ")]
    public required float CurrentAveragePriceHq { get; set; }

    /// <summary>
    ///     The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes
    ///     first).
    ///     This number will tend to be the same for every item, because the number of shown sales is the same and over the
    ///     same period.
    ///     This statistic is more useful in historical queries.
    /// </summary>
    [JsonProperty("regularSaleVelocity")]
    public required float SaleVelocity { get; init; }

    /// <summary>
    ///     The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever
    ///     comes first).
    ///     This number will tend to be the same for every item, because the number of shown sales is the same and over the
    ///     same period.
    ///     This statistic is more useful in historical queries.
    /// </summary>
    [JsonProperty("nqSaleVelocity")]
    public required float SaleVelocityNq { get; init; }

    /// <summary>
    ///     The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever
    ///     comes first).
    ///     This number will tend to be the same for every item, because the number of shown sales is the same and over the
    ///     same period.
    ///     This statistic is more useful in historical queries.
    /// </summary>
    [JsonProperty("hqSaleVelocity")]
    public required float SaleVelocityHq { get; init; }

    /// <summary>
    ///     The average sale price, with outliers removed beyond 3 standard deviations of the mean.
    /// </summary>
    [JsonProperty("averagePrice")]
    public required float AveragePrice { get; set; }

    /// <summary>
    ///     The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean.
    /// </summary>
    [JsonProperty("averagePriceNQ")]
    public required float AveragePriceNq { get; set; }

    /// <summary>
    ///     The average HQ sale price, with outliers removed beyond 3 standard deviations of the mean.
    /// </summary>
    [JsonProperty("averagePriceHQ")]
    public required float AveragePriceHq { get; set; }

    /// <summary>
    ///     The minimum listing price.
    /// </summary>
    [JsonProperty("minPrice")]
    public required int MinPrice { get; set; }

    /// <summary>
    ///     The minimum NQ listing price.
    /// </summary>
    [JsonProperty("minPriceNQ")]
    public required int MinPriceNq { get; set; }

    /// <summary>
    ///     The minimum HQ listing price.
    /// </summary>
    [JsonProperty("minPriceHQ")]
    public required int MinPriceHq { get; set; }

    /// <summary>
    ///     The maximum listing price.
    /// </summary>
    [JsonProperty("maxPrice")]
    public required int MaxPrice { get; set; }

    /// <summary>
    ///     The maximum NQ listing price.
    /// </summary>
    [JsonProperty("maxPriceNQ")]
    public required int MaxPriceNq { get; set; }

    /// <summary>
    ///     The maximum HQ listing price.
    /// </summary>
    [JsonProperty("maxPriceHQ")]
    public required int MaxPriceHq { get; set; }

    /// <summary>
    ///     A map of quantities to listing counts, representing the number of listings of each quantity.
    /// </summary>
    [JsonProperty("stackSizeHistogram")]
    public required SortedDictionary<int, int> StackSizeHistogram { get; init; } = new();

    /// <summary>
    ///     A map of quantities to NQ listing counts, representing the number of listings of each quantity.
    /// </summary>
    [JsonProperty("stackSizeHistogramNQ")]
    public required SortedDictionary<int, int> StackSizeHistogramNq { get; init; } = new();

    /// <summary>
    ///     A map of quantities to HQ listing counts, representing the number of listings of each quantity.
    /// </summary>
    [JsonProperty("stackSizeHistogramHQ")]
    public required SortedDictionary<int, int> StackSizeHistogramHq { get; init; } = new();

    /// <summary>
    ///     The world name, if applicable.
    /// </summary>
    [JsonProperty("worldName")]
    public required string WorldName { get; init; }

    /// <summary>
    ///     The last upload times in milliseconds since epoch for each world in the response, if this is a DC request.
    /// </summary>
    [JsonProperty("worldUploadTimes")]
    public required Dictionary<int, long> WorldUploadTimes { get; set; }

    public Plot GetPlot()
    {
        var plt = new Plot();

        var data = StackSizeHistogram.Select(t => new Bar(t.Key, t.Value)).ToList();

        plt.Style.Background(Color.FromHex("31363A"), Color.FromHex("3A4149"));
        if (data.Count > 0)
            plt.Add.Bar(data.ToArray(), Color.FromHex("228B22"));
        plt.XLabel("Stack Size");
        plt.YLabel("Count");
        return plt;
    }
}

public record ListingView
{
    /// <summary>
    ///     The time that this listing was posted, in seconds since the UNIX epoch.
    /// </summary>
    [JsonProperty("lastReviewTime")]
    public required long LastReviewTimeUnixSeconds { get; init; }

    /// <summary>
    ///     The price per unit sold.
    /// </summary>
    [JsonProperty("pricePerUnit")]
    public required int PricePerUnit { get; set; }

    /// <summary>
    ///     The stack size sold.
    /// </summary>
    [JsonProperty("quantity")]
    public required int Quantity { get; init; }

    /// <summary>
    ///     The ID of the dye on this item.
    /// </summary>
    [JsonProperty("stainID")]
    public required int DyeId { get; init; }

    /// <summary>
    ///     The world name, if applicable.
    /// </summary>
    [JsonProperty("worldName")]
    public required string WorldName { get; set; }

    /// <summary>
    ///     The world ID, if applicable.
    /// </summary>
    [JsonProperty("worldID")]
    public required int? WorldId { get; set; }

    /// <summary>
    ///     The creator's character name.
    /// </summary>
    [JsonProperty("creatorName")]
    public required string CreatorName { get; init; }

    /// <summary>
    ///     A SHA256 hash of the creator's ID.
    /// </summary>
    [JsonProperty("creatorID")]
    public required string CreatorIdHash { get; set; }

    /// <summary>
    ///     Whether or not the item is high-quality.
    /// </summary>
    [JsonProperty("hq")]
    public required bool Hq { get; init; }

    /// <summary>
    ///     Whether or not the item is crafted.
    /// </summary>
    [JsonProperty("isCrafted")]
    public required bool IsCrafted { get; init; }

    /// <summary>
    ///     A SHA256 hash of the ID of this listing. Due to some current client-side bugs, this will almost always be null.
    /// </summary>
    [JsonProperty("listingID")]
    public required string ListingIdHash { get; set; }

    /// <summary>
    ///     The materia on this item.
    /// </summary>
    [JsonProperty("materia")]
    public required List<MateriaView> Materia { get; init; } = new();

    /// <summary>
    ///     Whether or not the item is being sold on a mannequin.
    /// </summary>
    [JsonProperty("onMannequin")]
    public required bool OnMannequin { get; init; }

    /// <summary>
    ///     The city ID of the retainer.
    ///     Limsa Lominsa = 1
    ///     Gridania = 2
    ///     Ul'dah = 3
    ///     Ishgard = 4
    ///     Kugane = 7
    ///     Crystarium = 10
    /// </summary>
    [JsonProperty("retainerCity")]
    public required int RetainerCityId { get; init; }

    /// <summary>
    ///     A SHA256 hash of the retainer's ID.
    /// </summary>
    [JsonProperty("retainerID")]
    public required string RetainerIdHash { get; set; }

    /// <summary>
    ///     The retainer's name.
    /// </summary>
    [JsonProperty("retainerName")]
    public required string RetainerName { get; init; }

    /// <summary>
    ///     A SHA256 hash of the seller's ID.
    /// </summary>
    [JsonProperty("sellerID")]
    public required string SellerIdHash { get; set; }

    /// <summary>
    ///     The total price.
    /// </summary>
    [JsonProperty("total")]
    public required int Total { get; set; }
}

public record MateriaView
{
    /// <summary>
    ///     The materia slot.
    /// </summary>
    [JsonProperty("slotID")]
    public required int SlotId { get; init; }

    /// <summary>
    ///     The materia item ID.
    /// </summary>
    [JsonProperty("materiaID")]
    public required int MateriaId { get; init; }
}

public record SaleView
{
    /// <summary>
    ///     Whether or not the item was high-quality.
    /// </summary>
    [JsonProperty("hq")]
    public required bool Hq { get; init; }

    /// <summary>
    ///     The price per unit sold.
    /// </summary>
    [JsonProperty("pricePerUnit")]
    public required int PricePerUnit { get; init; }

    /// <summary>
    ///     The stack size sold.
    /// </summary>
    [JsonProperty("quantity")]
    public required int Quantity { get; init; }

    /// <summary>
    ///     The sale time, in seconds since the UNIX epoch.
    /// </summary>
    [JsonProperty("timestamp")]
    public required long TimestampUnixSeconds { get; init; }

    /// <summary>
    ///     Whether or not this was purchased from a mannequin. This may be null.
    /// </summary>
    [JsonProperty("onMannequin")]
    public required bool? OnMannequin { get; init; }

    /// <summary>
    ///     The world name, if applicable.
    /// </summary>
    [JsonProperty("worldName")]
    public required string WorldName { get; set; }

    /// <summary>
    ///     The world ID, if applicable.
    /// </summary>
    [JsonProperty("worldID")]
    public required int? WorldId { get; set; }

    /// <summary>
    ///     The buyer name.
    /// </summary>
    [JsonProperty("buyerName")]
    public required string BuyerName { get; init; }

    /// <summary>
    ///     The total price.
    /// </summary>
    [JsonProperty("total")]
    public required int Total { get; init; }
}