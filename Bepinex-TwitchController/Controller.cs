using BepInEx.Logging;
using System.Reflection;
using System.Threading.Tasks;
using TwitchController.Player_Events;

namespace TwitchController
{

    public class Controller
    {

        public const string Version = "0.0.0.1";

        internal readonly Secrets _secrets;
        internal readonly ManualLogSource _log;
        internal readonly Assembly myAssembly = Assembly.GetExecutingAssembly();
        internal readonly TwitchEventManager eventManager;
        internal readonly TimerCooldown timer;
        public readonly EventLookup eventLookup;
        public static bool HypeTrain = false;
        public static int HypeLevel = 1;

        public TwitchChatClient client;
        public TwitchPubSubClient pubsub;
        public Channel TextChannel;
        public Channel PubSubChannel;
        public System.Threading.CancellationToken cts;
        public System.Threading.CancellationToken cts2;

        public static Controller _instance;


        public Controller(Secrets secrets, ManualLogSource log)
        {
            _instance = this;
            _secrets = secrets;
            _log = log;
            eventLookup = new EventLookup(this);
            eventManager = new TwitchEventManager(this);
            timer = new TimerCooldown(this);
        }

        public void Update()
        {
            timer.Update();
        }

        public async Task<bool> StartTwitchChatClient()
        {
            if (client is null)
            {
                cts = new System.Threading.CancellationToken();
                client = new TwitchChatClient(this);
            }

            if (!client.IsClientConnected())
            {
                await client.ConnectAsync("oauth:" + _secrets.api_token, _secrets.username, cts);
                TextChannel = await client.JoinChannelAsync(_secrets.username, cts);
                TextChannel.MessageReceived += eventManager.ChatMessageReceived;
                await TextChannel.SendMessageAsync($"ModBot Connected!", cts);
                _log.LogWarning("ModBot Connected");
                return true;
            }
            return false;
        }

        public async Task<bool> StartTwitchPubSubClient()
        {
            if (pubsub is null)
            {
                cts2 = new System.Threading.CancellationToken();
                pubsub = new TwitchPubSubClient(this);
            }

            if (!pubsub.IsClientConnected())
            {
                await pubsub.ConnectAsync(_secrets.api_token, _secrets.nick_id, cts2);
                PubSubChannel = pubsub.JoinChannel(_secrets.username);
                PubSubChannel.MessageReceived += eventManager.PubSubMessageReceived;
                return true;
            }

            return false;
        }
    }
}