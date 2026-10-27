using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ticketing.Common.Events;
using Ticketing.Query.Domain.Interfaces;
using Ticketing.Query.Infrastructure.Converters;

namespace Ticketing.Query.Infrastructure.Consumers
{
    public class ConsumerHostedService : IHostedService
    {
        private readonly ConsumerConfig _config;
        private readonly ILogger<ConsumerHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ConsumerHostedService(ConsumerConfig config, ILogger<ConsumerHostedService> logger, IServiceProvider serviceProvider)
        {
            _config = config;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("El event consumer service está trabajando");

            //Topic de Kafka del que se leen los eventos que publica el lado Command; el nombre es compartido con Command
            string topic = KafkaTopics.TicketEvents;

            //El consumer lee clave y valor como texto UTF-8; el valor es el JSON del evento
            var consumer = new ConsumerBuilder<string, string>(_config)
                .SetKeyDeserializer(Deserializers.Utf8)
                .SetValueDeserializer(Deserializers.Utf8)
                .Build();

            //Se suscribe al topic y empieza a recibir mensajes
            consumer.Subscribe(topic);

            //Bucle infinito: lee un mensaje, lo procesa y confirma; después pasa al siguiente
            while(true)
            {
                //Consume() bloquea el hilo hasta que llega un mensaje
                var consumerResult = consumer.Consume();
                if(consumerResult is null) continue;
                if(consumerResult.Message is null) continue;

                //El converter lee el campo Type del JSON para saber qué clase de evento crear
                var options = new JsonSerializerOptions
                {
                    Converters = {new EventConverter()}
                };

                //Convierte el JSON en un evento concreto (TicketCreatedEvent, TicketUpdatedEvent...) usando EventBase como tipo común
                var @event = JsonSerializer.Deserialize<EventBase>(consumerResult.Message.Value, options);

                if(@event is null) throw new ArgumentNullException(nameof(@event), "No se pudo procesar el json parser");

                //El servicio es singleton y el handler usa servicios scoped (MediatR, DbContext): se crea un scope por mensaje
                using IServiceScope scope = _serviceProvider.CreateScope();
                var eventHandler = scope.ServiceProvider.GetRequiredService<IEventHandler>();

                //Según el tipo real del evento se llama a la sobrecarga On correspondiente
                Task handling = @event switch
                {
                    TicketCreatedEvent created => eventHandler.On(created),
                    TicketUpdatedEvent updated => eventHandler.On(updated),
                    _ => throw new InvalidOperationException($"Evento no soportado: {@event.GetType().Name}")
                };

                //Se espera a que termine la proyección antes de confirmar
                await handling;

                //Commit del offset: Kafka no volverá a entregar este mensaje. Si falla antes, se reintenta
                consumer.Commit(consumerResult);

            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }
}


