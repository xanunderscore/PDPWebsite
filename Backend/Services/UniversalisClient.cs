using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using Newtonsoft.Json;
using PDPWebsite.Universalis;

namespace PDPWebsite.Services
{
    public class UniversalisClient : HttpClient
    {
        private readonly ConcurrentDictionary<int, Tuple<TaxRatesView, DateTime>> _taxRatesCache = new();
        private readonly ConcurrentDictionary<string, Tuple<Listing, DateTime>> _listingsCache = new();
        private readonly ConcurrentDictionary<string, Tuple<History, DateTime>> _historiesCache = new();
        private Tuple<World[], DateTime> _worldsCache;
        private Tuple<Datacenter[], DateTime> _datacentersCache;

        public UniversalisClient()
        {
            BaseAddress = new Uri("https://universalis.app/api/v2");
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var netVersion = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
            foreach (var productHeader in new[] { new ProductInfoHeaderValue("WildWolfUniversalisBot", version?.InformationalVersion ?? "0.0.0"), new ProductInfoHeaderValue(".NET", netVersion?.FrameworkDisplayName?.Split(' ')[1]), new ProductInfoHeaderValue("CoreCLR", assembly.ImageRuntimeVersion) })
            {
                DefaultRequestHeaders.UserAgent.Add(productHeader);
            }

            GetDatacenters().GetAwaiter().GetResult();
            foreach (var world in GetWorlds().Result)
            {
#pragma warning disable CS4014
                GetTaxRates(world.Id);
#pragma warning restore CS4014
            }
        }

        public async Task<Datacenter[]> GetDatacenters()
        {
            if (_datacentersCache != null && _datacentersCache.Item2 > DateTime.UtcNow.AddDays(-7))
                return _datacentersCache.Item1;
            var response = await GetAsync("data-centers");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var datacenters = JsonConvert.DeserializeObject<Datacenter[]>(content) ?? throw new HttpRequestException();
            _datacentersCache = new Tuple<Datacenter[], DateTime>(datacenters, DateTime.UtcNow);
            return datacenters;
        }

        public async Task<World[]> GetWorlds()
        {
            if (_worldsCache != null && _worldsCache.Item2 > DateTime.UtcNow.AddDays(-7))
                return _worldsCache.Item1;
            var response = await GetAsync("worlds");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var worlds = JsonConvert.DeserializeObject<World[]>(content) ?? throw new HttpRequestException();
            _worldsCache = new Tuple<World[], DateTime>(worlds, DateTime.UtcNow);
            return worlds;
        }

        public async Task<Listing> GetListing(string worldId, uint itemId, ListingQuery? query = null)
        {
            query ??= new ListingQuery { Entries = 50 };
            if (_listingsCache.TryGetValue(worldId + itemId, out var cachedListing) && cachedListing.Item2 > DateTime.UtcNow.AddHours(-3))
                return cachedListing.Item1;
            var response = await GetAsync($"{worldId}/{itemId}{query?.GetQuery()}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var listing = JsonConvert.DeserializeObject<Listing>(content) ?? throw new HttpRequestException();
            _listingsCache[worldId] = new Tuple<Listing, DateTime>(listing, DateTime.UtcNow);
            return listing;
        }

        public async Task<History> GetHistory(string worldId, uint itemId, HistoryQuery? query = null)
        {
            query ??= new HistoryQuery { EntriesToReturn = 500 };
            if (_historiesCache.TryGetValue(worldId + itemId, out var cachedHistory) && cachedHistory.Item2 > DateTime.UtcNow.AddHours(-3))
                return cachedHistory.Item1;
            var response = await GetAsync($"history/{worldId}/{itemId}{query.GetQuery()}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var history = JsonConvert.DeserializeObject<History>(content) ?? throw new HttpRequestException();
            _historiesCache[worldId] = new Tuple<History, DateTime>(history, DateTime.UtcNow);
            return history;
        }

        public async Task<TaxRatesView> GetTaxRates(int worldId)
        {
            if (_taxRatesCache.TryGetValue(worldId, out var cached) && cached.Item2 > DateTime.UtcNow.AddDays(-1))
                return cached.Item1;
            var response = await GetAsync($"tax-rates?world={worldId}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var taxRates = JsonConvert.DeserializeObject<TaxRatesView>(content) ?? throw new HttpRequestException();
            _taxRatesCache[worldId] = new Tuple<TaxRatesView, DateTime>(taxRates, DateTime.UtcNow);
            return taxRates;
        }

        public async Task<uint[]> GetMarketItems()
        {
            var response = await GetAsync("marketable");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var marketItems = JsonConvert.DeserializeObject<uint[]>(content) ?? throw new HttpRequestException();
            return marketItems;
        }
    }

    public class HistoryQuery
    {
        public int? EntriesToReturn;

        public DateTimeOffset? StatsWithin;

        public DateTimeOffset? EntriesWithin;

        public string GetQuery()
        {
            var sb = new StringBuilder("");
            if (EntriesToReturn != null)
                sb.AppendQuery("entries", EntriesToReturn);
            if (StatsWithin != null)
                sb.AppendQuery("statsWithin", StatsWithin.Value.ToUnixTimeMilliseconds());
            if (EntriesWithin != null)
                sb.AppendQuery("entriesWithin", (DateTimeOffset.UtcNow - EntriesWithin.Value).TotalSeconds);
            return sb.ToString();
        }
    }

    public class ListingQuery
    {
        public int? Listings;

        public int? Entries;

        public bool? NoGst;

        public bool? HQ;

        public DateTime? StatsWithin;

        public DateTime? EntriesWithin;

        public string GetQuery()
        {
            var builder = new StringBuilder("");

            if (Listings != null)
                builder.AppendQuery("listings", Listings);

            if (Entries != null)
                builder.AppendQuery("entries", Entries);

            if (NoGst != null)
                builder.AppendQuery("noGst", NoGst);

            if (HQ != null)
                builder.AppendQuery("hq", HQ);

            if (StatsWithin != null)
                builder.AppendQuery("statsWithin", StatsWithin);

            if (EntriesWithin != null)
                builder.AppendQuery("entriesWithin", EntriesWithin);

            return builder.ToString();
        }
    }

    public static partial class Extensions
    {
        public static StringBuilder AppendQuery(this StringBuilder builder, string key, object value)
        {
            builder.Append(builder.Length == 0 ? "?" : "&");
            if (value is bool b)
                return builder.Append($"{key}={(b ? "true" : "false")}");
            else
                return builder.Append($"{key}={value}");
        }
    }
}
