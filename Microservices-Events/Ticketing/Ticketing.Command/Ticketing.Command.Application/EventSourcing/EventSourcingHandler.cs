using Ticketing.Command.Domain.Aggregates;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Domain.Interfaces.Events;
using Ticketing.Command.Domain.Enums;
using Ticketing.Common.Events;

namespace Ticketing.Command.Application.EventSourcing
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

            //La versión que el agregado tenía en el almacén antes de los cambios pendientes
            int expectedVersion = aggregate.VersionBeforeChanges;

            //El nombre de la clase del agregado coincide con el valor del enum
            eAggregateType aggregateType = Enum.Parse<eAggregateType>(typeof(TAggregate).Name);

            await _eventStore.SaveEventsAsync(aggregate.Id, aggregateType, changes, expectedVersion, cancellationToken);
        }
    }
}
