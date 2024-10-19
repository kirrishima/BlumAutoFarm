using System.Text.Json.Serialization;

namespace Blum.Models.Json
{
    public class BlumGameJson
    {
        [JsonPropertyName("game_id")]
        public string? GameId { get; set; } = null;

        [JsonPropertyName("points")]
        public int? Points { get; set; } = null;

        [JsonPropertyName("dogs")]
        public int? DogsPoints { get; set; } = null;
    }
}