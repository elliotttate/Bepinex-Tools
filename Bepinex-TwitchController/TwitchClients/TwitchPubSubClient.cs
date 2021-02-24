using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchController
{
    public class TwitchPubSubClient
    {

        public event EventHandler ConnectionClose;

        private IMessageClient _twitchMessageClient;

        protected ICollection<Channel> _channels = new List<Channel>();

        private readonly TwitchController controller;

        public TwitchPubSubClient(TwitchController twitchController)
        {
            controller = twitchController;
        }

        public bool IsClientConnected()
        {
            return _twitchMessageClient.IsConnected();
        }

        public bool IsChannelConnected(string channelName, out Channel outchannel)
        {
            outchannel = null;
            foreach (Channel channel in _channels)
            {
                if (channel.Name == channelName)
                {
                    outchannel = channel;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Triggered when the connection is closed from any reason.
        /// </summary>
        /// <param name="sender">IMessageClient</param>
        /// <param name="e">null</param>
        private void OnConnectionClosed(object sender, EventArgs e)
        {
            ConnectionClose?.Invoke(this, e);
        }

        /// <summary>
        /// Triggered when raw message received.
        /// </summary>
        /// <param name="sender">IMessageClient</param>
        /// <param name="e">Raw message</param>
        private void OnRawMessageReceived(object sender, string e)
        {
            if (TryParsePrivateMessage(e, out var message))
            {
                var c = _channels.FirstOrDefault(d => d.Name == message.Channel);
                c?.ReceiveMessage(message);
            }
        }

        /// <summary>
        /// Opens a connection to the server and start receiving messages.
        /// </summary>
        /// <param name="oauth">Your password should be an OAuth token authorized through our API with the chat:read scope (to read messages) and the  chat:edit scope (to send messages)</param>
        /// <param name="nick">Your nickname must be your Twitch username (login name) in lowercase</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task ConnectAsync(string oauth, string nickId, CancellationToken cancellationToken)
        {

            _twitchMessageClient = new WebSocketPubSubClient();
            _twitchMessageClient.MessageReceived += OnRawMessageReceived;
            _twitchMessageClient.ConnectionClosed += OnConnectionClosed;

            return _twitchMessageClient.ConnectAsync(oauth, nickId.ToLower(), cancellationToken);
        }

        /// <summary>
        /// Joins to given twitch channel. Connection must be established first.
        /// </summary>
        /// <param name="channelName">A channel name</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Channel JoinChannelAsync(string channelName, CancellationToken cancellationToken)
        {
            var cn = channelName.ToLower();

            if (!IsChannelConnected(channelName, out Channel channel))
            {
                 channel = new Channel(cn, _twitchMessageClient);
                _channels.Add(channel);
            }

            return channel;
        }

        /// <summary>
        /// Tries to parse raw message into message object.
        /// </summary>
        /// <param name="message">Raw message received from a server</param>
        /// <param name="msg">Output message object when successfully parsed</param>
        /// <returns></returns>
        public bool TryParsePrivateMessage(string message, out Message msg)
        {
            msg = new Message();

            var redemption = JsonSerializer.Deserialize<ChannelPointRedemptionMessage>(message);
            var regex = new Regex(".*login\":\"(?<user>.*?)\".*title\":\"(?<title>.*?)\".*");
            var regexBits = new Regex(".*user_name\":\"(?<user>.*?)\".*chat_message\":\"(?<title>.*?)\".*\"bits_used\":(?<bits>.*?),.*");

            var match = regex.Match("");
            //FOR SOME REASON YOU NEED THIS IN A TRY CATCH OR IT DOESN'T FUNCTION
            try
            {
                match = regex.Match(redemption.Data.Message);
                msg.Host = redemption.Data.Topic;
                if (match.Success)
                    msg.Text = match.Groups["title"].Value;
                if (!match.Success)
                {
                    match = regexBits.Match(redemption.Data.Message);
                    msg.Host = redemption.Data.Topic;
                    if (match.Success)
                        msg.Text = match.Groups["bits"].Value + ":" + match.Groups["title"].Value;
                }
            }
            catch (Exception e)
            {
                controller._log.LogError("THIS IS ERROR" + e.Message);
            }

            if (!match.Success)
                return false;

            var groups = match.Groups;

            msg.RawMessage = message;
            msg.User = groups["user"].Value;
            msg.Channel = controller._secrets.username;

            return true;
        }
    }
}