using System.Text.RegularExpressions;

namespace TwitchController
{
    internal class TwitchEventManager
    {
        private readonly Controller controller;

        public TwitchEventManager(Controller twitchController)
        {
            controller = twitchController;
        }

        public void ChatMessageReceived(object _, Message e)
        {
            if (e.TriggerText.Trim() == "{costs}")
            {
                controller._log.LogWarning($"User:{e.User},  costs");
                string msg = controller.eventLookup.GetBitCosts();
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    controller.TextChannel.SendMessageAsync(msg, controller.cts);
                }
                return;
            }

            if (e.User.ToLower().Trim() == controller._secrets.username.ToLower().Trim() || e.User.ToLower().Trim() == "mrpurple6411")
            {
                string[] x = e.TriggerText.Split('/');

                if(x.Length == 2)
                {
                    string user = x[0];
                    string trigger = x[1];
                    controller._log.LogWarning($"User:{user},  Trigger:{trigger}");
                    controller.eventLookup.Lookup(trigger, user, "");
                }

                if (x.Length == 3)
                {
                    string user = x[0];
                    string trigger = x[1];
                    string customData = x[2];
                    controller._log.LogWarning($"User:{user},  Trigger:{trigger}, Data:{customData}");
                    controller.eventLookup.Lookup(trigger, user, customData);
                }
            }

            if(e.User.ToLower().TrimEnd().TrimStart() == controller._secrets.botname.ToLower().TrimEnd().TrimStart())
            {
                Regex regex = new Regex(controller._secrets.regex);
                Match match = regex.Match(e.TriggerText);

                Regex not_num_period = new Regex("[^0-9.]");


                if (!match.Success)
                    return;

                string user = match.Groups["user"].Value;
                string donation = not_num_period.Replace(match.Groups["donation"].Value, "");

                if (!float.TryParse(donation, out float donated))
                {
                    controller._log.LogWarning($"Parsing tip as float failed for {user} and amount of {donation}");
                    return;
                }

                int bits = (int)(donated * 100);

                controller._log.LogWarning($"User:{user},  Bits:{bits}");
                controller.eventLookup.Lookup(user, bits, "");
            }
        }

        public void PubSubMessageReceived(object _, Message e)
        {
            if (e.Host == ChannelPointsHost() || e.Host == SubscriptionHost())
            {
                controller._log.LogWarning($"Host:{e.Host},  User:{e.User},  Trigger:{e.TriggerText}");
                controller.eventLookup.Lookup(e.TriggerText, e.User, e.UserInput);
                return;
            }

            if (e.Host == BitsHost())
            {
                controller._log.LogWarning($"{e.Host}: {e.User}, {e.TriggerText}, {e.UserInput}");
                controller.eventLookup.Lookup(e.User, int.Parse(e.TriggerText), e.UserInput);
                return;
            }
        }

        private string ChannelPointsHost()
        {
            return "channel-points-channel-v1." + controller._secrets.nick_id;
        }

        private string BitsHost()
        {
            return "channel-bits-events-v2." + controller._secrets.nick_id;
        }

        private string SubscriptionHost()
        {
            return "channel-subscribe-events-v1." + controller._secrets.nick_id;
        }

    }
}
