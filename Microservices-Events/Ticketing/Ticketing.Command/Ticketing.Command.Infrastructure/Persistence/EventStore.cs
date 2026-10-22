using System.Reflection.Metadata;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Enums;
using Ticketing.Command.Domain.Exceptions;
using Ticketing.Common.Events;
using Ticketing.Command.Domain.Interfaces.Events;

namespace Ticketing.Command.Infrastructure.Persistence
{
    public class EventStore : IEventStore
    {
        private readonly IEventModelRepository _eventModelRepository;
        public EventStore (IEventModelRepository eventModelRepository) => _eventModelRepository = eventModelRepository;
        public async Task<List<EventBase>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken)
        {
            List<EventModel> eventStream = await _eventModelRepository.FindByAggregateIdAsync(aggregateId, cancellationToken);
            if(eventStream is null || !eventStream.Any()) throw new AggregateNotFoundException(aggregateId);

            return eventStream.OrderBy(o => o.Version).Select(o => o.EventData).ToList();
        }

        //Lo llama EventSourcingHandler.SaveAsync con los cambios pendientes del agregado. Solo añade eventos: nunca modifica los ya guardados
        public async Task SaveEventsAsync(string aggregateId, eAggregateType aggregateType, IEnumerable<EventBase> events, int expectedVersion, CancellationToken cancellationToken)
        {
            List<EventModel> eventStream = await _eventModelRepository.FindByAggregateIdAsync(aggregateId, cancellationToken);

            //Un agregado sin eventos parte de la versión 0
            int lastVersion = 0;
            if (eventStream.Any()) lastVersion = eventStream.Max(o => o.Version);

            //Si la última versión guardada no es la que el agregado tenía al cargarse, otra operación escribió antes
            if (lastVersion != expectedVersion) throw new ConcurrencyException(aggregateId);

            //Cada evento nuevo continúa la numeración a partir de la versión esperada: expectedVersion + 1, + 2...
            int version = expectedVersion;
            foreach (EventBase @event in events)
            {
                version++;

                //Envuelve el evento en el documento que se guarda: a qué agregado pertenece, en qué versión y cuándo
                var eventModel = new EventModel
                {
                    TimeStamp = DateTime.UtcNow,
                    AggregateIdentifier = aggregateId,
                    AggregateType = aggregateType,
                    Version = version,
                    EventType = Enum.Parse<eEventType>(@event.Type), //El nombre del evento coincide con el valor del enum
                    EventData = @event
                };

                //IEventModelRepository se resuelve por DI a EventModelRepository, que inserta en Mongo dentro de la transacción abierta por el UoW
                await _eventModelRepository.InsertOneAsync(eventModel, cancellationToken);
            }
        }

    }
}

