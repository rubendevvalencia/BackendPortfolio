using Ticketing.Common.Messages;

namespace Ticketing.Common.Events
{
    public abstract class EventBase : Message
    {
        public EventBase(string type)
        {
            Type = type;
        }
        public string Type {get; set; }
    }
}


