using System.Text.Json.Serialization;

namespace Blum.Models
{
    public class BlumDailyRewardJson
    {
        public class Day
        {
            public class Rewards
            {
                [JsonPropertyName("passes")]
                public int? Passes { get; set; } = null;

                [JsonPropertyName("points")]
                public string? Points { get; set; } = null;
            }

            [JsonPropertyName("ordinal")]
            public int? Ordinal { get; set; } = null;

            [JsonPropertyName("reward")]
            public Rewards Reward { get; set; } = new Rewards();
        }

        [JsonPropertyName("days")]
        public List<Day> Days { get; set; } = new List<Day>();
    }
}

