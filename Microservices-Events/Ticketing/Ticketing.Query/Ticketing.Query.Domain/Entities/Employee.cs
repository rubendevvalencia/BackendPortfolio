using Ticketing.Query.Domain.Entities.Audit;
using Ticketing.Query.Domain.ValueObjects;

namespace Ticketing.Query.Domain.Entities
{
    public class Employee : BaseAudit
    {
        public string? FirstName {get; set;}
        public string? LastName {get; set;}
        public string? Email {get; set;}
        public Address? Address {get; set;}
        public ICollection<Ticket>? Tickets {get; set;}
    }

}

