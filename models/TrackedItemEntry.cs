using System.Text.Json.Serialization;

namespace PoE_Price_Tracking
{
    public class TrackedItemEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("league")]
        public string League { get; set; } = "Standard";
    }
}
