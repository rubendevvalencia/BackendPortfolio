namespace Ticketing.Query.Domain.Entities
{
    public class TicketType
    {
        public int Id {get; set;}
        public string? Name { get; set; }
        public ICollection<Ticket>? Tickets { get; set; }

        public TicketType(int id, string name)
        {
            Id = id;
            Name = name;
        }

    }
}
