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

        public event EventHandler<string> MessageRecieved;

        private IMessageClient _twitchMessageClient;

        protected Channel _channel;

        private readonly Controller controller;

        public TwitchChatClient(Controller twitchController)
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
            return _twitchMessageClient?.IsConnected() ?? false;
        }

        public bool IsChannelConnected(string channelName, out Channel outchannel)
        {
            outchannel = null;
                if (_channel?.Name == channelName)
                {
                    outchannel = _channel;
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Triggered when raw message received.
        /// </summary>
        /// <param name="sender">IMessageClient</param>
        /// <param name="e">Raw message</param>
        private async void OnRawMessageReceived(object sender, string e)
        {
            // About once every five minutes, the server sends a PING.
            // To ensure that your connection to the server is not prematurely terminated, reply with PONG
            if (e.StartsWith("PING"))
            {
                controller._log.LogMessage($"Twitch Client Ping");
                Task pong = SendPongResponseAsync();
                await pong;

                if (pong.Status != TaskStatus.RanToCompletion)
                {
                    controller._log.LogError($"Sending Pong Failed! {pong.Status}");
                }
                if (pong.Status == TaskStatus.RanToCompletion)
                {
                    controller._log.LogMessage($"Twitch Client replied with Pong.");
                }

                return;
            }

            try
            {
                MessageRecieved?.Invoke(sender, e);
            }
            catch { };


            if (TryParsePrivateMessage(e, out Message message))
            {
                _channel?.ReceiveMessage(message);
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
        /// Attempts to Reconnect asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task ReConnectAsync()
        {
            return _twitchMessageClient.ConnectAsync("oauth:" + controller._secrets.api_token, controller._secrets.username, controller.cts);
        }

        /// <summary>
        /// Joins to given twitch channel. Connection must be established first.
        /// </summary>
        /// <param name="channelName">A channel name</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Channel> JoinChannelAsync(string channelName, CancellationToken cancellationToken)
        {
            await _twitchMessageClient.SendMessageAsync($"JOIN #{channelName.ToLower()}", cancellationToken);

            _channel = new Channel(channelName.ToLower(), _twitchMessageClient);

            return _channel;
        }

        /// <summary>
        /// About once every five minutes, the server will send a PING :tmi.twitch.tv. 
        /// To ensure that your connection to the server is not prematurely terminated, reply with PONG :tmi.twitch.tv.
        /// </summary>
        /// <returns></returns>
        private Task SendPongResponseAsync()
        {
            return _twitchMessageClient.SendMessageAsync("PONG :tmi.twitch.tv", CancellationToken.None);
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
            Regex regex = new Regex(":(?<user>.*)!(.*)@(?<host>.*) PRIVMSG #(?<channel>.*) :(?<text>.*)");
            Match match = regex.Match(message);

            if (!match.Success)
            {
                return false;
            }

            GroupCollection groups = match.Groups;

            msg.RawMessage = message;
            msg.User = groups["user"].Value;
            msg.Host = groups["host"].Value;
            msg.TriggerText = groups["text"].Value;

            return true;
        }
    }
}