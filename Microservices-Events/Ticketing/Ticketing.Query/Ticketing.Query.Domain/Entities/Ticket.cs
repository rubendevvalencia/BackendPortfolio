using Ticketing.Query.Domain.Entities.Audit;
using Ticketing.Query.Domain.Events;

namespace Ticketing.Query.Domain.Entities
{
    public class Ticket : BaseAudit
    {
        public string? Description {get; set;}
        public int TicketTypeId {get; set;}
        public TicketType? TicketType {get; set;}
        public ICollection<Employee>? Employees {get; set;}
    }
    
}


