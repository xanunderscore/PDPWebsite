using Newtonsoft.Json;

namespace PDPWebsite.Universalis.Models
{
    public record Datacenter
    {
        [JsonProperty("name")]
        public string Name { get; init; }
    
        [JsonProperty("region")]
        public string Region { get; init; }
    
        [JsonProperty("worlds")]
        public int[] Worlds { get; init; }
    }

    public record World
    {
        [JsonProperty("id")]
        public int Id { get; init; }
    
        [JsonProperty("name")]
        public string Name { get; init; }
    }

    public record TaxRatesView
    {
        /// <summary>
        /// The percent retainer tax in Limsa Lominsa.
        /// </summary>
        [JsonProperty("Limsa Lominsa")]
        public int LimsaLominsa { get; init; }

        /// <summary>
        /// The percent retainer tax in Gridania.
        /// </summary>
        [JsonProperty("Gridania")]
        public int Gridania { get; init; }

        /// <summary>
        /// The percent retainer tax in Ul'dah.
        /// </summary>
        [JsonProperty("Ul'dah")]
        public int Uldah { get; init; }

        /// <summary>
        /// The percent retainer tax in Ishgard.
        /// </summary>
        [JsonProperty("Ishgard")]
        public int Ishgard { get; init; }

        /// <summary>
        /// The percent retainer tax in Kugane.
        /// </summary>
        [JsonProperty("Kugane")]
        public int Kugane { get; init; }

        /// <summary>
        /// The percent retainer tax in the Crystarium.
        /// </summary>
        [JsonProperty("Crystarium")]
        public int Crystarium { get; init; }

        /// <summary>
        /// The percent retainer tax in Old Sharlayan.
        /// </summary>
        [JsonProperty("Old Sharlayan")]
        public int OldSharlayan { get; init; }

        public override string ToString() => $"Limsa Lominsa: {LimsaLominsa}%, Gridania: {Gridania}%, Ul'dah: {Uldah}%, Ishgard: {Ishgard}%, Kugane: {Kugane}%, Crystarium: {Crystarium}%, Old Sharlayan: {OldSharlayan}%";
    }
}
