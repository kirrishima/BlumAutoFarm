using System.Text.Json.Serialization;

namespace Blum.Models.Json
{
    public class BlumPayloadJson
    {
        [JsonPropertyName("gameId")]
        public string GameId { get; set; }

        [JsonPropertyName("challenge")]
        public Challenge Challenge { get; set; }

        [JsonPropertyName("earnedPoints")]
        public EarnedPoints EarnedPoints { get; set; }

        [JsonPropertyName("assetClicks")]
        public Dictionary<string, AssetClick> AssetClicks { get; set; }
    }

    public class Challenge
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("nonce")]
        public int Nonce { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; }
    }

    public class EarnedPoints
    {
        [JsonPropertyName("BP")]
        public Point BP { get; set; }
    }

    public class Point
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }

    public class AssetClick
    {
        [JsonPropertyName("clicks")]
        public int Clicks { get; set; }
    }

    public class PayloadServerResponseJson
    {
        [JsonPropertyName("payload")]
        public string? Payload { get; set; }
    }
}