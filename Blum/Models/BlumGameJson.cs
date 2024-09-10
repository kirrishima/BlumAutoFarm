using System.Text.Json.Serialization;

namespace Blum.Models
{
    public class BlumGameJson
    {
        [JsonPropertyName("gameId")]
        public string? GameId { get; set; } = null;

        [JsonPropertyName("points")]
        public int? Points { get; set; } = null;
    }
}