using Ticketing.Command.Application.Aggregates;
using Ticketing.Command.Domain.Abstracts;
using Ticketing.Command.Domain.Enum;
using Ticketing.Common.Events;

namespace Ticketing.Command.Application.Services
{
    public class EventSourcingHandler<TAggregate> : IEventSourcingHandler<TAggregate> where TAggregate : AggregateRoot
    {
        private readonly IEventStore _eventStore;

        public EventSourcingHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public Task<TAggregate> GetByIdAsync(string aggregateId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        public async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<EventBase> changes = aggregate.UncommittedChanges;

            //Versión previa al primer cambio pendiente: la del agregado menos los eventos aún sin guardar
            int expectedVersion = aggregate.Version - changes.Count;

            //El nombre de la clase del agregado coincide con el valor del enum
            eAggregateType aggregateType = Enum.Parse<eAggregateType>(typeof(TAggregate).Name);

            await _eventStore.SaveEventsAsync(aggregate.Id, aggregateType, changes, expectedVersion, cancellationToken);
        }
    }
}
