using Ticketing.Command.Application.Aggregates;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Enum;
using Ticketing.Command.Domain.Interface.EventModels;
using Ticketing.Common.Events;

namespace Ticketing.Command.Application.Services
{
    public class EventSourcingHandler<TAggregate> : IEventSourcingHandler<TAggregate> where TAggregate : AggregateRoot
    {
        private readonly IEventModelRepository _eventModelRepo;

        public EventSourcingHandler(IEventModelRepository eventModelRepository)
        {
            _eventModelRepo = eventModelRepository;
        }

        public Task<TAggregate> GetByIdAsync(string aggregateId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        public async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<EventBase> changes = aggregate.UncommittedChanges;

            //Versión previa al primer cambio pendiente: la del agregado menos los eventos aún sin guardar
            int version = aggregate.Version - changes.Count;

            //El nombre de la clase del agregado coincide con el valor del enum
            eAggregateType aggregateType = Enum.Parse<eAggregateType>(typeof(TAggregate).Name);

            foreach (EventBase change in changes)
            {
                version++;

                var eventModel = new EventModel
                {
                    TimeStamp = DateTime.UtcNow,
                    AggregateIdentifier = aggregate.Id,
                    AggregateType = aggregateType,
                    Version = version,
                    EventType = Enum.Parse<eEventType>(change.Type), //El nombre del evento coincide con el valor del enum
                    EventData = change
                };

                await _eventModelRepo.InsertOneAsync(eventModel, cancellationToken);
            }
        }
    }
}
