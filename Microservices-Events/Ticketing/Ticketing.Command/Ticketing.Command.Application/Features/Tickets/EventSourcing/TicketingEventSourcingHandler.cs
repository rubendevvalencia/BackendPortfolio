using Microsoft.VisualBasic;
using Ticketing.Command.Domain.Aggregates.Tickets;
using Ticketing.Command.Application.EventSourcing;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Domain.Interfaces.Events;

namespace Ticketing.Command.Application.Features.Tickets.EventSourcing
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


