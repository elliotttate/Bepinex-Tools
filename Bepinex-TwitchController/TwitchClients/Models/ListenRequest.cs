using System.Text.Json.Serialization;

namespace TwitchController
{
    public class ListenRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("nonce")]
        public string Nonce { get; set; }

        [JsonPropertyName("data")]
        public ListenRequestData Data { get; set; }
    }
}