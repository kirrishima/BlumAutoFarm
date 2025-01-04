using System.Text.Json.Serialization;

namespace Blum.Models.Json
{
    public class BlumDailyRewardJson
    {
        [JsonPropertyName("claim")]
        public string? Claim { get; set; } = null;

        [JsonPropertyName("claimed")]
        public bool Claimed { get; set; } = false;

        [JsonPropertyName("currentStreakDays")]
        public int? CurrentStreakDays { get; set; } = null;

        [JsonPropertyName("todayReward")]
        public TodayReward? TodayRewards { get; set; } = null;

        public class TodayReward
        {
            [JsonPropertyName("points")]
            public string? Points { get; set; } = null;

            [JsonPropertyName("passes")]
            public int? Passes { get; set; } = null;
        }
    }
}

