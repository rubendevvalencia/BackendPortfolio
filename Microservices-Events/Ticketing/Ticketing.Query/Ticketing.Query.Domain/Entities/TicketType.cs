using System.Diagnostics.CodeAnalysis;
using Ticketing.Query.Domain.Entities;

namespace Ticketing.Query.Domain.Events
{
    public class TicketType
    {
        public int Id {get; set;}
        public string? Name { get; set; }
        public ICollection<Ticket>? Tickets { get; set; }

        [SetsRequiredMembers]
        public TicketType(int id, string name)
        {
            Id = id;
            Name = name;
        }
        
    }
}


