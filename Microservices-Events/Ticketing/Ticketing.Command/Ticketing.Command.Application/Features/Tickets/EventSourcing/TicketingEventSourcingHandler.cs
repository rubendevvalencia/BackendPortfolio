using Microsoft.VisualBasic;
using Ticketing.Command.Application.Aggregates;
using Ticketing.Command.Application.Services;
using Ticketing.Command.Domain.Abstracts;

namespace Ticketing.Command.Application.Feature.Tickets.EventSourcing
{
    public class TicketingEventSourcingHandler : IEventSourcingHandler<TicketAggregate>
    {
        private readonly IEventStore _eventStore;
        public TicketingEventSourcingHandler(IEventStore eventStore) => _eventStore = eventStore;

        public Task<TicketAggregate> GetByIdAsync(string aggregateId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        //public async Task<TicketAggregate> GetByIdAsync(string aggregateId, CancellationToken cancellationToken)
        //{
        //    var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
        //
        //    if(events is null || !events.Any()) return new AggregateRoot();
        //
        //
        //}


        public Task SaveAsync(TicketAggregate aggregate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }
}


