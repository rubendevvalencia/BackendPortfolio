using Ticketing.Command.Application.Aggregates;
using Ticketing.Command.Application.Services;

namespace Ticketing.Command.Application.Feature.Tickets.EventSourcing
{
    public class TicketingEventSourcingHandler : IEventSourcingHandler<TicketAggregate>
    {
        public Task<TicketAggregate> GetByIdAsync(string aggregateId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        public Task SaveAsync(TicketAggregate aggregate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }
}


