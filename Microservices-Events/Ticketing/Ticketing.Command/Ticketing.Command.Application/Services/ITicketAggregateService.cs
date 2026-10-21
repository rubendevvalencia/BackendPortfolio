using Ticketing.Command.Domain.Entities.Aggregates;

namespace Ticketing.Command.Application.Services
{
    public interface ITicketAggregateService : IAggregateService<TicketAggregate>
    {
        TicketAggregate Create(string? userName, string? errorType, string? errorDetail);
    }
}
