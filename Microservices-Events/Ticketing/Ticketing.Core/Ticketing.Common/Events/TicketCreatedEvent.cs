namespace Ticketing.Common.Events
{
    public class TicketCreatedEvent : EventBase
    {
        public required string? UserName {get; set; }
        public string? TyperError {get; set;}
        public string? DetailError {get; set;}
        public TicketCreatedEvent() : base(nameof(TicketCreatedEvent)) //nameOf es un string y por defecto toma el tipo de evento
        {
        }
    }

}


