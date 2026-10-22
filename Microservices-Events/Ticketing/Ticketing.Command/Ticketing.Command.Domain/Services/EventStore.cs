using System.Reflection.Metadata;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Enums;
using Ticketing.Common.Events;
using Ticketing.Command.Domain.Interfaces.Events;

namespace Ticketing.Command.Domain.Services
{
    public class EventStore : IEventStore
    {
        private readonly IEventModelRepository _eventModelRepository;
        public EventStore (IEventModelRepository eventModelRepository) => _eventModelRepository = eventModelRepository;
        public async Task<List<EventBase>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken)
        {
            List<EventModel> eventStream = await _eventModelRepository.FindByAggregateIdAsync(aggregateId, cancellationToken);

            if(eventStream is null || !eventStream.Any()) throw new Exception("El aggregate no tiene eventos");

            var list = eventStream.OrderBy(o => o.Version).Select(o => o.EventData).ToList();
            return list;
        }

        public async Task SaveEventsAsync(string aggregateId, eAggregateType aggregateType, IEnumerable<EventBase> events, int expectedVersion, CancellationToken cancellationToken)
        {
            List<EventModel> eventStream = await _eventModelRepository.FindByAggregateIdAsync(aggregateId, cancellationToken);

            //Un agregado sin eventos parte de la versión 0
            int lastVersion = 0;
            if (eventStream.Any()) lastVersion = eventStream.Max(o => o.Version);
            if (lastVersion != expectedVersion) throw new Exception("Concurrence Error");

            int version = expectedVersion;
            foreach (EventBase @event in events)
            {
                version++;

                var eventModel = new EventModel
                {
                    TimeStamp = DateTime.UtcNow,
                    AggregateIdentifier = aggregateId,
                    AggregateType = aggregateType,
                    Version = version,
                    EventType = Enum.Parse<eEventType>(@event.Type), //El nombre del evento coincide con el valor del enum
                    EventData = @event
                };

                await _eventModelRepository.InsertOneAsync(eventModel, cancellationToken);
            }
        }

    }
}

