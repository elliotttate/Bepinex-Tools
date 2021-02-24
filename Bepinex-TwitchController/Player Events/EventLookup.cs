using System;
using System.Collections.Generic;
using System.Linq;
using TwitchController.Player_Events.Models;
using System.Collections.Concurrent;

namespace TwitchController.Player_Events
{
    public class EventLookup
    {
        private readonly TwitchController controller;

        public EventLookup(TwitchController twitchController)
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
        // Parameter: ID, Action, BitCost, CooldownSeconds
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

        public bool AddEvent(string EventID, Action Action, int BitCost, int CooldownSeconds)
        {
            if (!EventDictionary.Keys.Contains(EventID))
            {
                EventDictionary.Add(EventID, new EventInfo(Action, BitCost, CooldownSeconds));
                return true;
            }
            controller._log.LogError($"Event with ID: {EventID} already registered!");
            return false;
        }
        public bool AddTimedEvent(string EventID, Action Action, int BitCost, int CooldownSeconds, Action TimedAction, int TimerLength)
        {
            if (!EventDictionary.Keys.Contains(EventID))
            {
                EventDictionary.Add(EventID, new TimedEventInfo(Action, BitCost, CooldownSeconds, TimedAction,TimerLength));
                return true;
            }
            controller._log.LogError($"Event with ID: {EventID} already registered!");
            return false;
        }

        public string getBitCosts()
        {
            string message = "";
            foreach (KeyValuePair<string, EventInfo> pair in EventDictionary)
            {
                var costText = pair.Key + " costs " + pair.Value.BitCost + " bits ||| ";
                message += costText;
            }
            return message;
        }

        public void Lookup(string EventText)
        {
            if (EventDictionary.Keys.Contains(EventText))
            {
                ActionQueue.Add(new KeyValuePair<string, EventInfo>(EventText, EventDictionary[EventText]));
                controller.timer.AddQueueEvent(EventText);
            }
        }

        public void Lookup(string EventText, int bits)
        {
            KeyValuePair<string, EventInfo> Event = EventDictionary.FirstOrDefault(it => EventText.Contains(it.Key));
            controller._log.LogMessage(Event.Key);
            if (!Event.Equals(default(KeyValuePair<string, EventInfo>)) && bits >= Event.Value.BitCost)
            {
                controller._log.LogMessage(Event.Key);
                ActionQueue.Add(Event);
                controller.timer.AddQueueEvent(Event.Key);
            }

        }

        public void ConfigureEventCosts(List<ConfigEventInfo> configInfo)
        {
            foreach (ConfigEventInfo i in configInfo)
            {
                if (EventDictionary.TryGetValue(i.EventName, out EventInfo eventInfo))
                {
                    if(eventInfo.BitCost != i.BitCost || eventInfo.CooldownSeconds != i.Cooldown)
                    {
                        eventInfo.BitCost = i.BitCost;
                        eventInfo.CooldownSeconds = i.Cooldown;
                        controller._log.LogMessage("Updating " + i.EventName + " to cost " + i.BitCost + " with a cooldown of " + i.Cooldown);
                    }
                }
            }
        }
    }
}
