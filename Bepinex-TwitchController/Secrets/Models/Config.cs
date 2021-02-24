using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwitchController
{
    class Config
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = "TwitchTokenGenerator client_id";

        [JsonPropertyName("bot_access_token")]
        public string BotAccessToken { get; set; } = "Access token of the account that will be using chat";

        [JsonPropertyName("username_token")]
        public string UsernameToken { get; set; } = "Access token for your account";

        [JsonPropertyName("username_id")]
        public string UsernameId { get; set; } = "Id of your account";

        [JsonPropertyName("username")]
        public string Username { get; set; } = "name of your account";

        [JsonPropertyName("bot_name")]
        public string BotName { get; set; } = "name of the account using chat";

        [JsonPropertyName("events")]
        public List<ConfigEventInfo> EventInfoList { get; set; } = new List<ConfigEventInfo>(){ new ConfigEventInfo(), };

    }

    public class ConfigEventInfo
    {
        [JsonPropertyName("event")]
        public string EventName { get; set; } = "unique chat string [Integration]";

        [JsonPropertyName("bit_cost")]
        public int BitCost { get; set; } = 100;

        [JsonPropertyName("cooldown")]
        public int Cooldown { get; set; } = 5;
    }
}
