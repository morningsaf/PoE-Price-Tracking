using System.Text.Json.Serialization;

namespace PoE_Price_Tracking
{
    public class PriceEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "";

        [JsonPropertyName("trend")]
        public string? Trend { get; set; } = "";
    }
}