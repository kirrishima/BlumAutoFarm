using System.Text.Json.Serialization;

namespace Blum.Models.Json
{
    public class BlumAccessTokenJson
    {
        public class TokenInfo
        {
            [JsonPropertyName("access")]
            public string? Access { get; set; }

            [JsonPropertyName("refresh")]
            public string? Refresh { get; set; }
        }
        [JsonPropertyName("token")]
        public TokenInfo? Token { get; set; }
    }
}
