using Newtonsoft.Json;
using PDPWebsite.FFXIV;
using ScottPlot;
using ScottPlot.TickGenerators;
using ScottPlot.TickGenerators.TimeUnits;
using Color = ScottPlot.Color;

namespace PDPWebsite.Universalis
{
    public record History
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
        /// The historical sales.
        /// </summary>
        [JsonProperty("entries")]
        public List<MinimizedSaleView> Sales { get; set; } = new();

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
        /// A map of quantities to sale counts, representing the number of sales of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogram")]
        public SortedDictionary<int, int> StackSizeHistogram { get; init; } = new();

        /// <summary>
        /// A map of quantities to NQ sale counts, representing the number of sales of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramNQ")]
        public SortedDictionary<int, int> StackSizeHistogramNq { get; init; } = new();

        /// <summary>
        /// A map of quantities to HQ sale counts, representing the number of sales of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramHQ")]
        public SortedDictionary<int, int> StackSizeHistogramHq { get; init; } = new();

        /// <summary>
        /// The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// </summary>
        [JsonProperty("regularSaleVelocity")]
        public float SaleVelocity { get; init; }

        /// <summary>
        /// The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// </summary>
        [JsonProperty("nqSaleVelocity")]
        public float SaleVelocityNq { get; init; }

        /// <summary>
        /// The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// </summary>
        [JsonProperty("hqSaleVelocity")]
        public float SaleVelocityHq { get; init; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonProperty("worldName")]
        public string WorldName { get; init; }

        public Plot GetPlot(IEnumerable<Item> items, bool errorBars = false)
        {
            var plt = new Plot();
            var salesTmp = Sales.Select(t => new SanitizedSale(t)).GroupBy(t => t.Date);

            var list = salesTmp.Select(f => new ProcessedSale(f)).OrderBy(t => t.Date).ToList();
            list.GetSalesPlot(plt, errorBars);
            plt.Legend();
            plt.Title($"Average Price of {items.First(t => t.Id == ItemId)!.Name}");
            plt.YLabel("Price");
            plt.Style.Background(Color.FromHex("31363A"), Color.FromHex("3A4149"));
            plt.XAxis.TickGenerator = new DateTimeFixedInterval(new Hour(), 3);
            return plt;
        }

        public ProcessedSale GetPriceHistory() => new(Sales.Select(t => new SanitizedSale(t)));
    }

    public class MinimizedSaleView
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
        /// The buyer's character name. This may be null.
        /// </summary>
        [JsonProperty("buyerName")]
        public string BuyerName { get; init; }

        /// <summary>
        /// Whether or not this was purchased from a mannequin. This may be null.
        /// </summary>
        [JsonProperty("onMannequin")]
        public bool? OnMannequin { get; init; }

        /// <summary>
        /// The sale time, in seconds since the UNIX epoch.
        /// </summary>
        [JsonProperty("timestamp")]
        public long TimestampUnixSeconds { get; init; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonProperty("worldName")]
        public string WorldName { get; init; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonProperty("worldID")]
        public int? WorldId { get; init; }
    }

    public class SanitizedSale
    {
        public int PricePerUnit { get; init; }
        public bool Hq { get; init; }
        public int Quantity { get; init; }
        public DateTimeOffset Date { get; init; }

        public SanitizedSale(MinimizedSaleView sale)
        {
            PricePerUnit = sale.PricePerUnit;
            Hq = sale.Hq;
            Quantity = sale.Quantity;
            var offset = DateTimeOffset.FromUnixTimeSeconds(sale.TimestampUnixSeconds);
            Date = offset.Date.AddHours(offset.Hour);
        }
    }

    public class ProcessedSale
    {
        public DateTimeOffset Date;
        public double AveragePrice;
        public double? AveragePriceNq;
        public double? AveragePriceHq;
        public double AverageQuantity;
        public double? AverageQuantityNq;
        public double? AverageQuantityHq;
        public double ErrorPrice;
        public double? ErrorPriceNq;
        public double? ErrorPriceHq;

        public ProcessedSale(IGrouping<DateTimeOffset, SanitizedSale> sales)
        {
            Date = sales.Key;
            AveragePrice = sales.Average(t => t.PricePerUnit);
            AverageQuantity = sales.Average(t => t.Quantity);
            ErrorPrice = sales.Select(t => t.PricePerUnit).Deviation();

            if (sales.Any(t => !t.Hq))
            {
                AveragePriceNq = sales.Where(t => !t.Hq).Average(t => t.PricePerUnit);
                AverageQuantityNq = sales.Where(t => !t.Hq).Average(t => t.Quantity);
                ErrorPriceNq = sales.Where(t => !t.Hq).Select(t => t.PricePerUnit).Deviation();
            }

            if (sales.Any(t => t.Hq))
            {
                AveragePriceHq = sales.Where(t => t.Hq).Average(t => t.PricePerUnit);
                AverageQuantityHq = sales.Where(t => t.Hq).Average(t => t.Quantity);
                ErrorPriceHq = sales.Where(t => t.Hq).Select(t => t.PricePerUnit).Deviation();
            }
        }

        public ProcessedSale(IEnumerable<SanitizedSale> sales)
        {
            Date = DateTimeOffset.Now;
            AveragePrice = sales.Average(t => t.PricePerUnit);
            AverageQuantity = sales.Average(t => t.Quantity);
            ErrorPrice = sales.Select(t => t.PricePerUnit).Deviation();

            if (sales.Any(t => !t.Hq))
            {
                AveragePriceNq = sales.Where(t => !t.Hq).Average(t => t.PricePerUnit);
                AverageQuantityNq = sales.Where(t => !t.Hq).Average(t => t.Quantity);
                ErrorPriceNq = sales.Where(t => !t.Hq).Select(t => t.PricePerUnit).Deviation();
            }

            if (sales.Any(t => t.Hq))
            {
                AveragePriceHq = sales.Where(t => t.Hq).Average(t => t.PricePerUnit);
                AverageQuantityHq = sales.Where(t => t.Hq).Average(t => t.Quantity);
                ErrorPriceHq = sales.Where(t => t.Hq).Select(t => t.PricePerUnit).Deviation();
            }
        }
    }

    public static partial class Extensions
    {
        public static double Deviation(this IEnumerable<int> values)
        {
            var avg = values.Average();
            var max = (double)values.Max();
            var min = values.Min();
            return avg - (max - min) / 2;
        }

        public static Dictionary<TK, TV> ToDictionaryOrdered<T, TK, TV>(this IEnumerable<T> values, Func<T, TK> key, Func<T, TV> value) where TK : notnull
        {
            return values.ToDictionary(key, value, EqualityComparer<TK>.Default).OrderBy(t => t.Key).ToDictionary(t => t.Key, t => t.Value);
        }

        public static void GetSalesPlot(this IEnumerable<ProcessedSale> sales, Plot plt, bool errorBars)
        {
            var avgSales = sales.ToDictionaryOrdered(sale => sale.Date.ToOADate(), sale => sale.AveragePrice);
            var avgSalesNq = sales.Where(sale => sale.AveragePriceNq.HasValue).ToDictionaryOrdered(sale => sale.Date.ToOADate(), sale => sale.AveragePriceNq!.Value);
            var avgSalesHq = sales.Where(sale => sale.AveragePriceHq.HasValue).ToDictionaryOrdered(sale => sale.Date.ToOADate(), sale => sale.AveragePriceHq!.Value);
            var avg = plt.Add.Scatter(avgSales.Keys.ToArray(), avgSales.Values.ToArray(), Color.FromARGB(128 * 256 ^ 3 + 127 * 256 + 255));
            avg.Label = "Average Price";

            if (avgSalesNq.Any())
            {
                var avgNq = plt.Add.Scatter(avgSalesNq.Keys.ToArray(), avgSalesNq.Values.ToArray(), Color.FromARGB(128 * 256 ^ 3 + 255 * 256));
                avgNq.Label = "Average Price (NQ)";
            }

            if (avgSalesHq.Any())
            {
                var avgHq = plt.Add.Scatter(avgSalesHq.Keys.ToArray(), avgSalesHq.Values.ToArray(), Color.FromARGB(128 * 256 ^ 3 + 255));
                avgHq.Label = "Average Price (HQ)";
            }
            //
            // var avg = plt.AddScatter(avgSales.Keys.ToArray(), avgSales.Values.ToArray(), label: "Average Price", color: Color.FromArgb(128, 0, 127, 255));
            // if(errorBars)
            //     avg.YError = sales.Select(sale => sale.ErrorPrice).ToArray();
            //
            // if (avgSalesNq.Any())
            // {
            //     var avgNq = plt.AddScatter(avgSalesNq.Keys.ToArray(), avgSalesNq.Values.ToArray(), label: "Average Price (NQ)", color: Color.FromArgb(128, 255, 0, 0));
            //     if(errorBars)
            //         avgNq.YError = sales.Where(sale => sale.ErrorPriceNq.HasValue).Select(sale => sale.ErrorPriceNq!.Value).ToArray();
            // }
            //
            // if (avgSalesHq.Any())
            // {
            //     var avgHq = plt.AddScatter(avgSalesHq.Keys.ToArray(), avgSalesHq.Values.ToArray(), label: "Average Price (HQ)", color: Color.Gold);
            //     if(errorBars)
            //         avgHq.YError = sales.Where(sale => sale.ErrorPriceHq.HasValue).Select(sale => sale.ErrorPriceHq!.Value).ToArray();
            // }

        }

        public static double ToOADate(this DateTimeOffset date)
        {
            return date.DateTime.ToOADate();
        }
    }
}
