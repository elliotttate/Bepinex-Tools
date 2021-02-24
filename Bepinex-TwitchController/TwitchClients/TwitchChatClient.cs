using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchController
{
    public class TwitchChatClient
    {

        public event EventHandler ConnectionClose;

        private IMessageClient _twitchMessageClient;

        protected ICollection<Channel> _channels = new List<Channel>();

        private readonly TwitchController controller;

        public TwitchChatClient(TwitchController twitchController)
        {
            controller = twitchController;
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
        /// Triggered when raw message received.
        /// </summary>
        /// <param name="sender">IMessageClient</param>
        /// <param name="e">Raw message</param>
        private void OnRawMessageReceived(object sender, string e)
        {
            // About once every five minutes, the server sends a PING.
            // To ensure that your connection to the server is not prematurely terminated, reply with PONG
            if (e.StartsWith("PING"))
            {
                Task pong = SendPongResponseAsync();
                pong.RunSynchronously();
                if(pong.Status != TaskStatus.RanToCompletion)
                    controller._log.LogError($"Sending Pong Failed! {pong.Status}");
            }

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
        public Task ConnectAsync(string oauth, string nick, CancellationToken cancellationToken)
        {
            _twitchMessageClient = new WebSocketMessageClient();
            _twitchMessageClient.MessageReceived += OnRawMessageReceived;
            _twitchMessageClient.ConnectionClosed += OnConnectionClosed;

            return _twitchMessageClient.ConnectAsync(oauth, nick.ToLower(), cancellationToken);
        }

        /// <summary>
        /// Joins to given twitch channel. Connection must be established first.
        /// </summary>
        /// <param name="channelName">A channel name</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Channel> JoinChannelAsync(string channelName, CancellationToken cancellationToken)
        {
            var cn = channelName.ToLower();
            await _twitchMessageClient.SendMessageAsync($"JOIN #{cn}", cancellationToken);

            if (!IsChannelConnected(channelName, out Channel channel))
            {
                channel = new Channel(cn, _twitchMessageClient);
                _channels.Add(channel);
            }
            
            return channel;
        }

        /// <summary>
        /// About once every five minutes, the server will send a PING :tmi.twitch.tv. 
        /// To ensure that your connection to the server is not prematurely terminated, reply with PONG :tmi.twitch.tv.
        /// </summary>
        /// <returns></returns>
        private Task SendPongResponseAsync()
        {
            return _twitchMessageClient.SendMessageAsync("PONG :tmi.twitch.tv\r\n", CancellationToken.None);
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
            var regex = new Regex(":(?<user>.*)!(.*)@(?<host>.*) PRIVMSG #(?<channel>.*) :(?<text>.*)");
            var match = regex.Match(message);

            if (!match.Success)
                return false;

            var groups = match.Groups;

            msg.RawMessage = message;
            msg.User = groups["user"].Value;
            msg.Host = groups["host"].Value;
            msg.Channel = groups["channel"].Value;
            msg.Text = groups["text"].Value;

            return true;
        }
    }
}