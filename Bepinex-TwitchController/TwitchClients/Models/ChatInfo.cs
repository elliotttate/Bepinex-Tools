using System.Text.Json.Serialization;

namespace TwitchController
{
    public class ChatInfo
    {
        [JsonPropertyName("chatter_count")]
        public int ChatterCount { get; set; }

        [JsonPropertyName("chatters")]
        public Chatters Chatters { get; set; }
    }
}