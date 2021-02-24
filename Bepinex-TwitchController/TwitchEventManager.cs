using System;

namespace TwitchController
{
    class TwitchEventManager
    {
        private readonly TwitchController controller;

        public TwitchEventManager(TwitchController twitchController)
        {
            controller = twitchController;
        }

        public void ChatMessageReceived(object sender, Message e)
        {
            if(e.Text.Trim() == ("{costs}"))
            {
                string msg = controller.eventLookup.getBitCosts();
                controller.TextChannel.SendMessageAsync(msg, controller.cts);
                controller._log.LogMessage(msg);
            }
        }

        public void PubSubMessageReceived(object sender, Message e)
        {
            if (e.Host == ChannelPointsHost())
            {
                controller.eventLookup.Lookup(e.Text);
            }
            if (e.Host == BitsHost())
            {
                controller.eventLookup.Lookup(e.Text, Int32.Parse(e.Text.Split(':')[0]));
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

    }
}
