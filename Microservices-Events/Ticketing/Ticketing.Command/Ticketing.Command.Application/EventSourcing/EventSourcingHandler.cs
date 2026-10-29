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
        private readonly IAggregateService<TAggregate> _aggregateService;

        public EventSourcingHandler(IEventStore eventStore, IAggregateService<TAggregate> aggregateService)
        {
            _eventStore = eventStore;
            _aggregateService = aggregateService;
        }

        public async Task<TAggregate> GetByIdAsync(string aggregateId, CancellationToken cancellationToken)
        {
            //El almacén devuelve los eventos ordenados por versión; el servicio del dominio los reproduce
            List<EventBase> events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
            TAggregate aggregate = _aggregateService.ReplayEvents(events);

            return aggregate;
        }


        public async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<EventBase> changes = aggregate.UncommittedChanges;

            //La versión que el agregado tenía en el almacén antes de los cambios pendientes
            int expectedVersion = aggregate.VersionBeforeChanges;

            //Cada agregado declara su tipo; ya no depende del nombre de la clase
            eAggregateType aggregateType = aggregate.AggregateType;
            //Aquí se hace el guardado físico
            await _eventStore.SaveEventsAsync(aggregate.Id, aggregateType, changes, expectedVersion, cancellationToken);
        }
    }
}
