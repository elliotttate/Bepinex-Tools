using System.Text.Json.Serialization;

namespace TwitchController
{
    public class ListenRequestData
    {
        [JsonPropertyName("topics")]
        public string[] Topics { get; set; }

        [JsonPropertyName("auth_token")]
        public string AuthToken { get; set; }
    }
}