﻿using LitJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchController.Player_Events.Models;
using TwitchController.TwitchClients.Models;

namespace TwitchController
{
    public class TwitchPubSubClient
    {

        public event EventHandler ConnectionClose;

        public event EventHandler<string> MessageRecieved;

        private IMessageClient _twitchMessageClient;

        protected Channel _channel;

        private readonly Controller controller;

        public TwitchPubSubClient(Controller twitchController)
        {
            controller = twitchController;
        }

        public bool IsClientConnected()
        {
            return _twitchMessageClient?.IsConnected() ?? false;
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
            try
            {
                if(this.TryParsePrivateMessage(e, out Message message))
                {
                    _channel?.ReceiveMessage(message);
                }
            }
            catch(Exception ex) 
            {
                Controller._instance._log.LogError($"{ex}");
            }

            try
            {
                MessageRecieved?.Invoke(sender, e);
            }
            catch { }
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
        /// Attempts to Reconnect asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task ReConnectAsync()
        {
            return _twitchMessageClient.ConnectAsync(controller._secrets.api_token, controller._secrets.nick_id, controller.cts2);
        }

        /// <summary>
        /// Joins to given twitch channel. Connection must be established first.
        /// </summary>
        /// <param name="channelName">A channel name</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Channel JoinChannel(string channelName)
        {
            _channel = new Channel(channelName.ToLower(), _twitchMessageClient);

            return _channel;
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
            JsonData data = JsonMapper.ToObject(new JsonReader(message));

            string msgType = data[0].ToString().ToLower();
            List<string> keys = data.Keys.ToList();

            switch(msgType)
            {
                case "pong":
                    Controller._instance._log.LogError("PubSub Pong Recieved!");
                    return false;
                case "response":
                    if(keys.Contains("error") && !string.IsNullOrWhiteSpace(data[keys.IndexOf("error")].ToString()))
                    {
                        Controller._instance._log.LogFatal($"Failed to properly connect to PubSub! Restarting game REQUIRED!");
                    }
                    else
                    {
                        Controller._instance._log.LogError($"Connected to PubSub!");
                    }
                    return false;
                case "reconnect":
                    Controller._instance._log.LogFatal($"Twitch Server Restarting connection will be lost within 30 seconds.");
                    _twitchMessageClient.DisconnectAsync(Controller._instance.cts2).Wait();
                    break;
                case "message":
                    MessageResponse messageResponse = JsonMapper.ToObject<MessageResponse>(message);
                    var MR = messageResponse.data.message.Replace(@"\", "");
                    var host = messageResponse.data.topic;

                    switch(host.Split('.')[0])
                    {
                        case "channel-subscribe-events-v1":
                            try
                            {
                                SubEvent subEvent = JsonMapper.ToObject<SubEvent>(MR);

                                msg.Channel = subEvent.channel_name.ToLower();
                                msg.Host = host;
                                msg.RawMessage = message;
                                msg.TriggerText = subEvent.sub_plan;
                                msg.User = subEvent.is_gift && !string.IsNullOrEmpty(subEvent.user_name)  ? subEvent.recipient_display_name : subEvent.display_name;

                                return true;

                            }
                            catch(Exception e)
                            {
                                Controller._instance._log.LogFatal($"Failed to convert {MR} into SubEvent.");
                                Controller._instance._log.LogFatal(e);
                                return false;
                            }
                        case "channel-bits-events-v2":
                            try
                            {
                                BitsEvent bitsEvent = JsonMapper.ToObject<BitsEvent>(MR);
                                msg.Channel = bitsEvent.data.channel_name.ToLower();
                                msg.Host = host;
                                msg.RawMessage = message;
                                msg.TriggerText = bitsEvent.data.bits_used.ToString();
                                msg.User = bitsEvent.is_anonymous ? "Anonymous" : bitsEvent.data.user_name;

                                return true;
                            }
                            catch(Exception e)
                            {
                                Controller._instance._log.LogFatal($"Failed to convert {MR} into BitsEvent.");
                                Controller._instance._log.LogFatal(e);
                                return false;
                            }
                        case "channel-points-channel-v1":
                            if(MR.Contains("reward-redeemed"))
                            {
                                try
                                {
                                    JsonReader reader = new JsonReader(MR) { SkipNonMembers = true, AllowComments = true, AllowSingleQuotedStrings = true };
                                    ChannelPointsMessageResponse pointsMessage = JsonMapper.ToObject<ChannelPointsMessageResponse>(reader);

                                    msg.Host = $"channel-points-channel-v1.{controller._secrets.nick_id}";
                                    msg.Channel = controller._secrets.username.ToLower();
                                    msg.RawMessage = message;
                                    msg.User = pointsMessage.data.redemption.user.display_name;
                                    msg.TriggerText = pointsMessage.data.redemption.reward.title;

                                    return true;
                                }
                                catch(Exception e)
                                {
                                    Controller._instance._log.LogFatal($"Failed to convert {MR} into Points Event.");
                                    Controller._instance._log.LogFatal(e);
                                    return false;
                                }
                            }
                            return false;
                        case "hype-train-events-v1":
                            if(MR.Contains("hype-train-start"))
                            {
                                Controller.HypeTrain = true;
                                if(controller.eventLookup.TryGetEvent("HypeTrain", out EventInfo eventInfo))
                                {
                                    eventInfo.BitCost = 100;
                                }
                                controller.eventLookup.Lookup("HypeTrainStart", "!!!HYPETRAIN STARTED!!!");

                                return false;
                            }
                            else if(MR.Contains("hype-train-level-up"))
                            {
                                Controller.HypeLevel += 1;
                                controller.eventLookup.Lookup($"HypeTrainLevel{Controller.HypeLevel}", $"!!!LEVEL {Controller.HypeLevel} HYPETRAIN!!!");
                                return false;
                            }
                            else if(MR.Contains("hype-train-end"))
                            {
                                Controller.HypeTrain = false;
                                Controller.HypeLevel = 1;
                                if(controller.eventLookup.TryGetEvent("HypeTrain", out EventInfo eventInfo))
                                {
                                    eventInfo.BitCost = 0;
                                }
                                controller.eventLookup.Lookup("HypeTrainEnd", $"!!!HYPETRAIN FINISHED!!!");

                                return false;
                            }
                            return false;
                        default:
                            Controller._instance._log.LogFatal($"PubSub Event Failed to Parse \n {message}");
                            return false;
                    }

                default:
                    break;
            }

            return false;
        }
    }
}