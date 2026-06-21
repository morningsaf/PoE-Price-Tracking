using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PoE_Price_Tracking
{
    public class UserData
    {
        [JsonPropertyName("league")]
        public string League { get; set; } = "Standard";

        [JsonPropertyName("tracked_items")]
        public List<string> TrackedItems { get; set; } = new();
    }
}


