namespace Ticketing.Common.Events
{
    public class TicketUpdatedEvent : EventBase
    {
        public TicketUpdatedEvent(eEventType type) : base(type)
        {
        }

        public string? Status {get; set;}
        public string? Description {get; set;}
        public string? Username {get; set;}
    }


}


