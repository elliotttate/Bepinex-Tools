using System;

namespace TwitchController.Player_Events.Models
{
    public class EventInfo
    {
        public string Perp;
        public Action<string, string> Action;
        public int BitCost;
        public int CooldownSeconds;

        public EventInfo(Action<string, string> action, int bitCost, int cooldownSeconds)
        {
            Action = action;
            BitCost = bitCost;
            CooldownSeconds = cooldownSeconds;
        }

        public EventInfo(string perp, EventInfo eventInfo)
        {
            Perp = perp;
            Action = eventInfo.Action;
            BitCost = eventInfo.BitCost;
            CooldownSeconds = eventInfo.CooldownSeconds;
        }
    }

    public class TimedEventInfo : EventInfo
    {
        public Action TimedAction;
        public int TimerLength;

        public TimedEventInfo(Action<string, string> action, int bitCost, int cooldownSeconds, Action timedAction, int timerLength) : base(action, bitCost, cooldownSeconds)
        {
            TimedAction = timedAction;
            TimerLength = timerLength;
        }

        public TimedEventInfo(string perp, TimedEventInfo timedEventInfo) : base(perp, timedEventInfo)
        {

            TimedAction = timedEventInfo.TimedAction;
            TimerLength = timedEventInfo.TimerLength;
        }

    }

    public class DataEvent : EventInfo
    {
        public Action<string, string, string> DataAction;
        public string UserInput;

        public DataEvent(Action<string, string> action, int bitCost, Action<string, string, string> dataAction) : base(action, bitCost, 0)
        {
            DataAction = dataAction;
            UserInput = "";
        }

        public DataEvent(string perp, DataEvent dataEventInfo, string userInput) : base(perp, dataEventInfo)
        {
            DataAction = dataEventInfo.DataAction;
            UserInput = userInput;
        }
    }


}
