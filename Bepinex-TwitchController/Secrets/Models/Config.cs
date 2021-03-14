using System.Collections.Generic;

namespace TwitchController
{
    internal class Config
    {
        public string ClientId { get; set; } = "TwitchTokenGenerator client_id";

        public string UsernameToken { get; set; } = "Access token for your account";

        public string UsernameRefreshToken { get; set; } = "Refresh Token for your account";

        public string UsernameId { get; set; } = "Id of your account";

        public string Username { get; set; } = "name of your account";

        public string BotName { get; set; } = "Streamlabs";

        public string TipsRegEx { get; set; } = "(?<user>.*) just tipped (?<donation>.*)!";

    }

}
