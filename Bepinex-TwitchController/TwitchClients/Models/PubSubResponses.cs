using LitJson;
using System.Collections.Generic;

namespace TwitchController.TwitchClients.Models
{
    public class MessageResponse
    {
        public string type { get; set; }

        public MessageData data { get; set; }

        public class MessageData
        {
            public string topic { get; set; }

            public string message { get; set; }

        }
    }

    public class ChannelPointsMessageResponse
    {
        public string type { get; set; }

        public MessageData data { get; set; }

        public class MessageData
        {
            public string timestamp { get; set; }

            public RedemptionData redemption { get; set; }

            public class RedemptionData
            {
                public string id { get; set; }
                public User user { get; set; }
                public string channel_id { get; set; }
                public string redeemed_at { get; set; }
                public Reward reward { get; set; }
                public string user_input { get; set; }
                public string status { get; set; }

                public class User
                {
                    public string id { get; set; }
                    public string login { get; set; }
                    public string display_name { get; set; }
                }

                public class Reward
                {
                    public string id { get; set; }
                    public string channel_id { get; set; }
                    public string title { get; set; }
                    public string prompt { get; set; }
                    public int cost { get; set; }
                    public bool is_user_input_required { get; set; }
                    public bool is_sub_only { get; set; }
                    public JsonData image { get; set; }
                    public JsonData default_image { get; set; }
                    public string background_color { get; set; }
                    public bool is_enabled { get; set; }
                    public bool is_paused { get; set; }
                    public bool is_in_stock { get; set; }
                    public JsonData max_per_stream { get; set; }
                    public bool should_redemptions_skip_request_queue { get; set; }
                    public string template_id { get; set; }
                    public string updated_for_indicator_at { get; set; }
                    public JsonData max_per_user_per_stream { get; set; }
                    public JsonData global_cooldown { get; set; }
                    public string redemptions_redeemed_current_stream { get; set; }
                    public string cooldown_expires_at { get; set; }

                }
            }
        }
    }


    public class BitsEvent
    {
        public Data data { get; set; }
        public string version { get; set; }
        public string message_type { get; set; }
        public string message_id { get; set; }
        public bool is_anonymous { get; set; }

        public class Data
        {
            public string user_name { get; set; }
            public string channel_name { get; set; }
            public string user_id { get; set; }
            public string channel_id { get; set; }
            public string time { get; set; }
            public string chat_message { get; set; }
            public int bits_used { get; set; }
            public int total_bits_used { get; set; }
            public string context { get; set; }
            public BadgeEntitlement badge_entitlement { get; set; }

            public class BadgeEntitlement
            {
                public int new_version { get; set; }
                public int previous_version { get; set; }
            }
        }
    }

    public class SubEvent
    {
        public string user_name { get; set; }
        public string display_name { get; set; }
        public string channel_name { get; set; }
        public string user_id { get; set; }
        public string channel_id { get; set; }
        public string time { get; set; }
        public string sub_plan { get; set; }
        public string sub_plan_name { get; set; }
        public int cumulative_months { get; set; }
        public int streak_months { get; set; }
        public string context { get; set; }
        public bool is_gift { get; set; }
        public SubMessage sub_message { get; set; }
        public string recipient_id { get; set; }
        public string recipient_user_name { get; set; }
        public string recipient_display_name { get; set; }
        public int multi_month_duration { get; set; }

        public class SubMessage
        {
            public string message { get; set; }
            public List<Emote> emotes { get; set; }

            public class Emote
            {
                public int start { get; set; }
                public int end { get; set; }
                public string id { get; set; }
            }
        }
    }
}
