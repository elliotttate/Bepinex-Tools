using System.Text.Json.Serialization;

namespace TwitchController
{
    public class ChannelPointRedemptionMessageData
    {
        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}