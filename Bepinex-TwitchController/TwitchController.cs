using System.Reflection;
using TwitchController.Player_Events;
using BepInEx.Logging;
using System.Threading.Tasks;
using UnityEngine.PlayerLoop;

namespace TwitchController
{

    public class TwitchController
    {

        public const string Version = "0.0.0.1";

        internal readonly Secrets _secrets;
        internal readonly ManualLogSource _log;
        internal readonly Assembly myAssembly = Assembly.GetExecutingAssembly();
        internal readonly EventLookup eventLookup;
        internal readonly TwitchEventManager eventManager;
        internal readonly TimerCooldown timer;

        public TwitchChatClient client;
        public TwitchPubSubClient pubsub;
        public Channel TextChannel;
        public Channel PubSubChannel;
        public System.Threading.CancellationToken cts;
        public System.Threading.CancellationToken cts2;


        public TwitchController(Secrets secrets, ManualLogSource log)
        {
            _secrets = secrets;
            _log = log;
            eventLookup = new EventLookup(this);
            eventManager = new TwitchEventManager(this);
            timer = new TimerCooldown(this);

            // Customize event configuration
            eventLookup.ConfigureEventCosts(secrets.eventConfigList);
        }

        public void Update()
        {
            timer.Update();
        }

        public async Task<bool> StartTwitchChatClient()
        {
            if(client is null)
            {
                cts = new System.Threading.CancellationToken();
                client = new TwitchChatClient(this);
            }

            if(!client.IsClientConnected())
            {
                await client.ConnectAsync("oauth:" + _secrets.access_token, _secrets.botname, cts);
                TextChannel = await client.JoinChannelAsync(_secrets.username, cts);
                TextChannel.MessageReceived += eventManager.ChatMessageReceived;
                return true;
            }
            return false;
        }

        public async Task<bool> StartTwitchPubSubClient()
        {
            if(pubsub is null)
            {
                cts2 = new System.Threading.CancellationToken();
                pubsub = new TwitchPubSubClient(this);
            }

            if (!pubsub.IsClientConnected())
            {
                await pubsub.ConnectAsync(_secrets.api_token, _secrets.nick_id, cts2);
                PubSubChannel = pubsub.JoinChannelAsync(_secrets.username, cts);
                PubSubChannel.MessageReceived += eventManager.PubSubMessageReceived;
                return true;
            }

            return false;
        }
    }
}