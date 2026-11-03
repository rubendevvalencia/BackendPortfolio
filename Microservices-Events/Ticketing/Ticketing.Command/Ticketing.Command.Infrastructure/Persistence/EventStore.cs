using System.Reflection.Metadata;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Enums;
using Ticketing.Command.Domain.Exceptions;
using Ticketing.Common.Events;
using Ticketing.Command.Domain.Interfaces.Events;
using Confluent.Kafka;
using Ticketing.Common.Common.Producers.Interface;

namespace Ticketing.Command.Infrastructure.Persistence
{
    public class EventStore : IEventStore
    {
        private readonly IEventModelRepository _eventModelRepo;
        private readonly ProducerConfig _confKakfa;
        private readonly IEventProducer _eventProducer;

        public EventStore (IEventModelRepository eventModelRepository, ProducerConfig confKafka, IEventProducer eventProducer)
        {
            _eventModelRepo = eventModelRepository;
            _confKakfa = confKafka;
            _eventProducer = eventProducer;
        } 
        public async Task<List<EventBase>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken)
        {
            List<EventModel> eventStream = await _eventModelRepo.FindByAggregateIdAsync(aggregateId, cancellationToken);
            if(eventStream is null || !eventStream.Any()) throw new AggregateNotFoundException(aggregateId);

            return eventStream.OrderBy(o => o.Version).Select(o => o.EventData).ToList();
        }

        //Lo llama EventSourcingHandler.SaveAsync con los cambios pendientes del agregado. Solo añade eventos: nunca modifica los ya guardados
        public async Task SaveEventsAsync(string aggregateId, eAggregateType aggregateType, IEnumerable<EventBase> events, int expectedVersion, CancellationToken cancellationToken)
        {
            List<EventModel> eventStream = await _eventModelRepo.FindByAggregateIdAsync(aggregateId, cancellationToken);

            //Un agregado sin eventos parte de la versión 0
            int lastVersion = 0;
            if (eventStream.Any()) lastVersion = eventStream.Max(o => o.Version);

            //Si la última versión guardada no es la que el agregado tenía al cargarse, otra operación escribió antes
            if (lastVersion != expectedVersion) throw new ConcurrencyException(aggregateId);

            //Topic de Kafka al que se publican los eventos; el nombre es compartido con Query
            string topic = KafkaTopics.TicketEvents;

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
                    EventType = @event.Type, //El evento ya trae su tipo como eEventType
                    EventData = @event
                };

                //IEventModelRepository se resuelve por DI a EventModelRepository, que inserta en Mongo dentro de la transacción abierta por el UoW
                await _eventModelRepo.AddAsync(eventModel, cancellationToken);
                
                //Se manda a kafka el event
                await _eventProducer.ProduceAsync(topic, @event);
            }
        }

    }
}

