using System.Text.Json.Serialization;

namespace Blum.Models
{
    public class BlumBalanceJson
    {
        public class JsonFarming
        {
            [JsonPropertyName("startTime")]
            public long? StartTime { get; set; }

            [JsonPropertyName("endTime")]
            public long? EndTime { get; set; }
        }

        [JsonPropertyName("availableBalance")]
        public string? AvailableBalance { get; set; }

        [JsonPropertyName("isFastFarmingEnabled")]
        public bool IsFastFarmingEnabled { get; set; } = false;

        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }

        [JsonPropertyName("playPasses")]
        public int? PlayPasses { get; set; }

        [JsonPropertyName("farming")]
        public JsonFarming? Farming { get; set; }
    }
}