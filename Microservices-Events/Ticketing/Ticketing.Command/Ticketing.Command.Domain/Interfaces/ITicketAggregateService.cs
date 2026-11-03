using Ticketing.Command.Domain.Aggregates;
using Ticketing.Command.Domain.Aggregates.Tickets;

namespace Ticketing.Command.Domain.Interfaces
{
    public interface ITicketAggregateService : IAggregateService<TicketAggregate>
    {
        TicketAggregate Create(string? userName, string? errorType, string? errorDetail);
    }
}
