using Ticketing.Common.Events;
using Ticketing.Query.Domain.Entities.Audit;

namespace Ticketing.Query.Domain.Entities
{
    public class Ticket : BaseAudit
    {
        public string? Description {get; set;}
        public Guid? TicketTypeId {get; set;}
        public eEventType? TicketType {get; set;}
        public ICollection<Employee>? Employees {get; set;}
    }
    
}


