using System;
using System.Collections.Generic;
using System.Linq;
using TwitchController.Player_Events.Models;

namespace TwitchController.Player_Events
{
    public class EventLookup
    {
        private readonly Controller controller;

        public EventLookup(Controller twitchController)
        {
            controller = twitchController;
        }

        // Queue for events
        public List<KeyValuePair<string, EventInfo>> ActionQueue = new List<KeyValuePair<string, EventInfo>>();

        // Queue for the cleanup code of timed events
        public List<Action> TimedActionsQueue = new List<Action>();

        // List with currently running timed events
        public List<string> RunningEventIDs = new List<string>();

        // List with currently running timed events
        public Dictionary<string, float> Cooldowns = new Dictionary<string, float>();

        //MAP OF EVENTS TO THEIR APPROPRIATE FUNCTIONS
        // Parameter: ID, Action<string, string>, BitCost, CooldownSeconds
        private readonly Dictionary<string, EventInfo> EventDictionary = new Dictionary<string, EventInfo>();

        public bool AddEvent(string EventID, EventInfo eventInfo)
        {
            if (!EventDictionary.Keys.Contains(EventID))
            {
                EventDictionary.Add(EventID, eventInfo);
                return true;
            }
            controller._log.LogError($"Event with ID: {EventID} already registered!");
            return false;
        }

        public bool AddEvent(string EventID, DataEvent eventInfo)
        {
            if (!EventDictionary.Keys.Contains(EventID))
            {
                EventDictionary.Add(EventID, eventInfo);
                return true;
            }
            controller._log.LogError($"Event with ID: {EventID} already registered!");
            return false;
        }

        public bool AddEvent(string EventID, Action<string, string> Action, int BitCost, int CooldownSeconds)
        {
            if (!EventDictionary.Keys.Contains(EventID))
            {
                EventDictionary.Add(EventID, new EventInfo(Action, BitCost, CooldownSeconds));
                return true;
            }
            controller._log.LogError($"Event with ID: {EventID} already registered!");
            return false;
        }

        public bool AddTimedEvent(string EventID, Action<string, string> Action, int BitCost, int CooldownSeconds, Action TimedAction, int TimerLength)
        {
            if (!EventDictionary.Keys.Contains(EventID))
            {
                EventDictionary.Add(EventID, new TimedEventInfo(Action, BitCost, CooldownSeconds, TimedAction, TimerLength));
                return true;
            }
            controller._log.LogError($"Event with ID: {EventID} already registered!");
            return false;
        }

        public bool TryGetEvent(string EventID, out EventInfo eventInfo)
        {
            return EventDictionary.TryGetValue(EventID, out eventInfo);
        }

        public string GetBitCosts()
        {
            string message = "";
            foreach (KeyValuePair<string, EventInfo> pair in EventDictionary)
            {
                if(pair.Value.BitCost > 0)
                {
                    string costText = $"[{pair.Key}]: {pair.Value.BitCost} bits ||| ";
                    message += costText;
                }
            }
            return message;
        }

        public void Lookup(string EventText, string perp, string userInput)
        {
            if (EventDictionary.TryGetValue(EventText.Trim(), out EventInfo eventInfo))
            {
                switch (eventInfo)
                {
                    case TimedEventInfo timed:
                        TimedEventInfo tei = new TimedEventInfo(perp, timed);
                        ActionQueue.Add(new KeyValuePair<string, EventInfo>(EventText, tei));
                        break;

                    default:
                        EventInfo ei = new EventInfo(perp, eventInfo);
                        ActionQueue.Add(new KeyValuePair<string, EventInfo>(EventText, ei));
                        break;
                }
            }
        }

        public void Lookup(string perp, int bits, string userInput)
        {
            KeyValuePair<string, EventInfo> Event = EventDictionary.Where(it => it.Value.BitCost > 0 && it.Value.BitCost <= bits)?.OrderByDescending(it => it.Value.BitCost)?.FirstOrDefault() ?? default;
            if (!Event.Equals(default(KeyValuePair<string, EventInfo>)))
            {
                switch (Event.Value)
                {
                    case TimedEventInfo timed:
                        TimedEventInfo tei = new TimedEventInfo(perp, timed);
                        //controller._log.LogMessage(Event.Key);
                        ActionQueue.Add(new KeyValuePair<string, EventInfo>(Event.Key, tei));
                        controller.timer.AddQueueEvent(Event.Key);
                        break;
                    case DataEvent dataEvent:
                        DataEvent de = new DataEvent(perp, dataEvent, userInput);
                        ActionQueue.Add(new KeyValuePair<string, EventInfo>(Event.Key, de));
                        controller.timer.AddQueueEvent(Event.Key);
                        break;
                    default:
                        EventInfo ei = new EventInfo(perp, Event.Value);
                        //controller._log.LogMessage(Event.Key);
                        ActionQueue.Add(new KeyValuePair<string, EventInfo>(Event.Key, ei));
                        controller.timer.AddQueueEvent(Event.Key);
                        break;
                }
            }
        }

    }
}
