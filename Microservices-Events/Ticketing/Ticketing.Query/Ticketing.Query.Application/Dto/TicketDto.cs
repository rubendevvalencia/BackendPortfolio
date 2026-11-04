using Ticketing.Common.Events;

namespace Ticketing.Query.Application.Dto
{
    public class TicketDto
    {
        public string? Description {get; set;}
        public Guid? TicketTypeId {get; set;}
        public eEventType? TicketType {get; set;}
        public DateTime? LastUpdatedAt { get; set;}
        public string? LastUpdatedBy { get; set; }
        
    }
    
}


