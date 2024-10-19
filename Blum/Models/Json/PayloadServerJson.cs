using System.Text.Json.Serialization;

namespace Blum.Models.Json
{
    public class PayloadServerJson
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }
    }

    public class PayloadServersListJson
    {
        [JsonPropertyName("payloadServer")]
        public List<PayloadServerJson> PayloadServers { get; set; } = [];
    }
}
