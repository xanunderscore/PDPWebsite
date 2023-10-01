using Newtonsoft.Json;
using ScottPlot;
using ScottPlot.Plottables;
using Color = ScottPlot.Color;

namespace PDPWebsite.Universalis.Models
{
    public record Listing
    {
        /// <summary>
        /// The item ID.
        /// </summary>
        [JsonProperty("itemID")]
        public int ItemId { get; init; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonProperty("worldID")]
        public int? WorldId { get; init; }

        /// <summary>
        /// The last upload time for this endpoint, in milliseconds since the UNIX epoch.
        /// </summary>
        [JsonProperty("lastUploadTime")]
        public long LastUploadTimeUnixMilliseconds { get; set; }

        /// <summary>
        /// The currently-shown listings.
        /// </summary>
        [JsonProperty("listings")]
        public List<ListingView> Listings { get; set; } = new();

        /// <summary>
        /// The currently-shown sales.
        /// </summary>
        [JsonProperty("recentHistory")]
        public List<SaleView> RecentHistory { get; set; } = new();

        /// <summary>
        /// The DC name, if applicable.
        /// </summary>
        [JsonProperty("dcName")]
        public string DcName { get; init; }

        /// <summary>
        /// The region name, if applicable.
        /// </summary>
        [JsonProperty("regionName")]
        public string RegionName { get; init; }

        /// <summary>
        /// The average listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("currentAveragePrice")]
        public float CurrentAveragePrice { get; set; }

        /// <summary>
        /// The average NQ listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("currentAveragePriceNQ")]
        public float CurrentAveragePriceNq { get; set; }

        /// <summary>
        /// The average HQ listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("currentAveragePriceHQ")]
        public float CurrentAveragePriceHq { get; set; }

        /// <summary>
        /// The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
        /// This statistic is more useful in historical queries.
        /// </summary>
        [JsonProperty("regularSaleVelocity")]
        public float SaleVelocity { get; init; }

        /// <summary>
        /// The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
        /// This statistic is more useful in historical queries.
        /// </summary>
        [JsonProperty("nqSaleVelocity")]
        public float SaleVelocityNq { get; init; }

        /// <summary>
        /// The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.
        /// This statistic is more useful in historical queries.
        /// </summary>
        [JsonProperty("hqSaleVelocity")]
        public float SaleVelocityHq { get; init; }

        /// <summary>
        /// The average sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("averagePrice")]
        public float AveragePrice { get; set; }

        /// <summary>
        /// The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("averagePriceNQ")]
        public float AveragePriceNq { get; set; }

        /// <summary>
        /// The average HQ sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("averagePriceHQ")]
        public float AveragePriceHq { get; set; }

        /// <summary>
        /// The minimum listing price.
        /// </summary>
        [JsonProperty("minPrice")]
        public int MinPrice { get; set; }

        /// <summary>
        /// The minimum NQ listing price.
        /// </summary>
        [JsonProperty("minPriceNQ")]
        public int MinPriceNq { get; set; }

        /// <summary>
        /// The minimum HQ listing price.
        /// </summary>
        [JsonProperty("minPriceHQ")]
        public int MinPriceHq { get; set; }

        /// <summary>
        /// The maximum listing price.
        /// </summary>
        [JsonProperty("maxPrice")]
        public int MaxPrice { get; set; }

        /// <summary>
        /// The maximum NQ listing price.
        /// </summary>
        [JsonProperty("maxPriceNQ")]
        public int MaxPriceNq { get; set; }

        /// <summary>
        /// The maximum HQ listing price.
        /// </summary>
        [JsonProperty("maxPriceHQ")]
        public int MaxPriceHq { get; set; }

        /// <summary>
        /// A map of quantities to listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogram")]
        public SortedDictionary<int, int> StackSizeHistogram { get; init; } = new();

        /// <summary>
        /// A map of quantities to NQ listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramNQ")]
        public SortedDictionary<int, int> StackSizeHistogramNq { get; init; } = new();

        /// <summary>
        /// A map of quantities to HQ listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramHQ")]
        public SortedDictionary<int, int> StackSizeHistogramHq { get; init; } = new();

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonProperty("worldName")]
        public string WorldName { get; init; }

        /// <summary>
        /// The last upload times in milliseconds since epoch for each world in the response, if this is a DC request.
        /// </summary>
        [JsonProperty("worldUploadTimes")]
        public Dictionary<int, long> WorldUploadTimes { get; set; }

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
        /// The time that this listing was posted, in seconds since the UNIX epoch.
        /// </summary>
        [JsonProperty("lastReviewTime")]
        public long LastReviewTimeUnixSeconds { get; init; }

        /// <summary>
        /// The price per unit sold.
        /// </summary>
        [JsonProperty("pricePerUnit")]
        public int PricePerUnit { get; set; }

        /// <summary>
        /// The stack size sold.
        /// </summary>
        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        /// <summary>
        /// The ID of the dye on this item.
        /// </summary>
        [JsonProperty("stainID")]
        public int DyeId { get; init; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonProperty("worldName")]
        public string WorldName { get; set; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonProperty("worldID")]
        public int? WorldId { get; set; }

        /// <summary>
        /// The creator's character name.
        /// </summary>
        [JsonProperty("creatorName")]
        public string CreatorName { get; init; }

        /// <summary>
        /// A SHA256 hash of the creator's ID.
        /// </summary>
        [JsonProperty("creatorID")]
        public string CreatorIdHash { get; set; }

        /// <summary>
        /// Whether or not the item is high-quality.
        /// </summary>
        [JsonProperty("hq")]
        public bool Hq { get; init; }

        /// <summary>
        /// Whether or not the item is crafted.
        /// </summary>
        [JsonProperty("isCrafted")]
        public bool IsCrafted { get; init; }

        /// <summary>
        /// A SHA256 hash of the ID of this listing. Due to some current client-side bugs, this will almost always be null.
        /// </summary>
        [JsonProperty("listingID")]
        public string ListingIdHash { get; set; }

        /// <summary>
        /// The materia on this item.
        /// </summary>
        [JsonProperty("materia")]
        public List<MateriaView> Materia { get; init; } = new();

        /// <summary>
        /// Whether or not the item is being sold on a mannequin.
        /// </summary>
        [JsonProperty("onMannequin")]
        public bool OnMannequin { get; init; }

        /// <summary>
        /// The city ID of the retainer.
        /// Limsa Lominsa = 1
        /// Gridania = 2
        /// Ul'dah = 3
        /// Ishgard = 4
        /// Kugane = 7
        /// Crystarium = 10
        /// </summary>
        [JsonProperty("retainerCity")]
        public int RetainerCityId { get; init; }

        /// <summary>
        /// A SHA256 hash of the retainer's ID.
        /// </summary>
        [JsonProperty("retainerID")]
        public string RetainerIdHash { get; set; }

        /// <summary>
        /// The retainer's name.
        /// </summary>
        [JsonProperty("retainerName")]
        public string RetainerName { get; init; }

        /// <summary>
        /// A SHA256 hash of the seller's ID.
        /// </summary>
        [JsonProperty("sellerID")]
        public string SellerIdHash { get; set; }

        /// <summary>
        /// The total price.
        /// </summary>
        [JsonProperty("total")]
        public int Total { get; set; }
    }

    public record MateriaView
    {
        /// <summary>
        /// The materia slot.
        /// </summary>
        [JsonProperty("slotID")]
        public int SlotId { get; init; }

        /// <summary>
        /// The materia item ID.
        /// </summary>
        [JsonProperty("materiaID")]
        public int MateriaId { get; init; }
    }

    public record SaleView
    {
        /// <summary>
        /// Whether or not the item was high-quality.
        /// </summary>
        [JsonProperty("hq")]
        public bool Hq { get; init; }

        /// <summary>
        /// The price per unit sold.
        /// </summary>
        [JsonProperty("pricePerUnit")]
        public int PricePerUnit { get; init; }

        /// <summary>
        /// The stack size sold.
        /// </summary>
        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        /// <summary>
        /// The sale time, in seconds since the UNIX epoch.
        /// </summary>
        [JsonProperty("timestamp")]
        public long TimestampUnixSeconds { get; init; }

        /// <summary>
        /// Whether or not this was purchased from a mannequin. This may be null.
        /// </summary>
        [JsonProperty("onMannequin")]
        public bool? OnMannequin { get; init; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonProperty("worldName")]
        public string WorldName { get; set; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonProperty("worldID")]
        public int? WorldId { get; set; }

        /// <summary>
        /// The buyer name.
        /// </summary>
        [JsonProperty("buyerName")]
        public string BuyerName { get; init; }

        /// <summary>
        /// The total price.
        /// </summary>
        [JsonProperty("total")]
        public int Total { get; init; }
    }
}
