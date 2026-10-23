using System.Text.Json;
using Confluent.Kafka;
using Ticketing.Common.Common.Producers.Interface;
using Ticketing.Common.Events;

namespace Ticketing.Command.Infrastructure.Persistence
{
    public class TicketEventProducer : IEventProducer
    {
        private readonly ProducerConfig _confKakfa;
        public TicketEventProducer(ProducerConfig confKakfa) => _confKakfa = confKakfa;
        public async Task ProduceAsync(string topic, EventBase @event) //manda el topic al broker
        {
            using var producer = new ProducerBuilder<string, string>(_confKakfa)
                .SetKeySerializer(Serializers.Utf8)
                .SetValueSerializer(Serializers.Utf8)
                .Build();

            string json = JsonSerializer.Serialize(@event, @event.GetType());

            Message<string, string> message = new Message<string, string>
            {
                Key = @event.Id, //ponemos el id por si luego a futuro hay particiones
                Value = json
            };

            var status = await producer.ProduceAsync(topic, message);

            if(status.Status == PersistenceStatus.NotPersisted) 
                throw new Exception(@$"No se pudo enviar el mensaje {@event.GetType().Name} 
                hacia el topic {topic}, por la siguiente razón: {status.Message} ");

        }

    }
}


