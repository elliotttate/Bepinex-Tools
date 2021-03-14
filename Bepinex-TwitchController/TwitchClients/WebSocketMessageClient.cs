using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchController
{
    internal class WebSocketMessageClient : IMessageClient
    {
        public event EventHandler<string> MessageReceived;

        public event EventHandler ConnectionClosed;

        private readonly ClientWebSocket _webSocketClient = new ClientWebSocket();

        private readonly Uri _webSocketServerUri;

        public WebSocketMessageClient(string webSocketServerUrl = /*"wss://irc.fdgt.dev:443") //*/"wss://irc-ws.chat.twitch.tv:443")
        {
            _webSocketServerUri = new Uri(webSocketServerUrl);
        }

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await _webSocketClient.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task ConnectAsync(string oauth, string nick, CancellationToken cancellationToken)
        {
            await _webSocketClient.ConnectAsync(_webSocketServerUri, cancellationToken);

            if (_webSocketClient.State == WebSocketState.Open)
            {

                await SendMessageAsync($"PASS {oauth}", cancellationToken);
                await SendMessageAsync($"NICK {nick}", cancellationToken);

                // start receiving messages in separeted thread
                System.Runtime.CompilerServices.ConfiguredTaskAwaitable receive = ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            return _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", cancellationToken);
        }

        public async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string message = await ReceiveMessageAsync(cancellationToken);
                    MessageReceived?.Invoke(this, message);
                }
                catch (WebSocketException)
                {
                    if (_webSocketClient.State != WebSocketState.Open)
                    {
                        ConnectionClosed?.Invoke(this, null);
                    }
                    _webSocketClient.Abort();
                    return;
                }
            }
        }

        public bool IsConnected()
        {
            return _webSocketClient.State == WebSocketState.Open;

        }

        /// <summary>
        /// Receives raw message from the opened connection.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<string> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            // RFC 1459 uses 512 bytes to hold one full message, therefore, it should be enough
            byte[] byteArray = new byte[512];
            ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(byteArray);

            WebSocketReceiveResult receivedResult = await _webSocketClient.ReceiveAsync(receiveBuffer, cancellationToken);

            byte[] msgBytes = receiveBuffer.Skip(receiveBuffer.Offset)
                .Take(receivedResult.Count)
                .ToArray();

            string receivedMessage = Encoding.UTF8.GetString(msgBytes);

            return receivedMessage;
        }
    }
}