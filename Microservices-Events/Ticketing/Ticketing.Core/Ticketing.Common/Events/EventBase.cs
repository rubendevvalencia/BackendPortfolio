using Ticketing.Common.Messages;

namespace Ticketing.Common.Events
{
    public abstract class EventBase : Message
    {
        public eEventType Type {get; set; }
        public EventBase(eEventType type) => Type = type;
      
    }
}


