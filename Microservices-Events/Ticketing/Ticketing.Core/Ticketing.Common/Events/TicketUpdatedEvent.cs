namespace Ticketing.Common.Events
{
    public class TicketUpdatedEvent : EventBase
    {
        public string? Status {get; set;}
        public string? DetailError {get; set;}
        public string? Username {get; set;}
        public TicketUpdatedEvent() : base(eEventType.TicketUpdatedEvent)
        {
        }

       
    }


}


