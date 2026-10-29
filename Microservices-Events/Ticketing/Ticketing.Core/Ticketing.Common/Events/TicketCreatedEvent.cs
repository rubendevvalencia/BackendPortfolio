namespace Ticketing.Common.Events
{
    public class TicketCreatedEvent : EventBase
    {
        public string? ErrorDetail {get; set;}
        public TicketCreatedEvent() : base(eEventType.TicketCreatedEvent)
        {
        }
    }

}


