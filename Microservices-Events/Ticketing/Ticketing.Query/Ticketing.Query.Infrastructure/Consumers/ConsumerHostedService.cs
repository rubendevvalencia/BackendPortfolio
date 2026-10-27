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

        //Permite pedirle al bucle que pare desde StopAsync, y la tarea del bucle para esperar a que termine
        private CancellationTokenSource? _stoppingSource;
        private Task? _consuming;

        public ConsumerHostedService(ConsumerConfig config, ILogger<ConsumerHostedService> logger, IServiceProvider serviceProvider)
        {
            _config = config;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }


        //Consume() bloquea el hilo: el bucle se lanza en un hilo del pool y StartAsync vuelve enseguida para que el host termine de arrancar
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppingSource = new CancellationTokenSource();
            CancellationToken stoppingToken = _stoppingSource.Token;

            _consuming = Task.Run(() => ConsumeAsync(stoppingToken));

            return Task.CompletedTask;
        }

        //Pide parar al bucle y espera a que cierre el consumer; si el host se impacienta (cancellationToken) deja de esperar
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if(_stoppingSource is null) return;
            if(_consuming is null) return;

            _stoppingSource.Cancel();

            await _consuming.WaitAsync(cancellationToken);
        }

        private async Task ConsumeAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("El event consumer service está trabajando");

            //Topic de Kafka del que se leen los eventos que publica el lado Command; el nombre es compartido con Command
            string topic = KafkaTopics.TicketEvents;

            //El consumer lee clave y valor como texto UTF-8; el valor es el JSON del evento
            using IConsumer<string, string> consumer = new ConsumerBuilder<string, string>(_config)
                .SetKeyDeserializer(Deserializers.Utf8)
                .SetValueDeserializer(Deserializers.Utf8)
                .Build();

            //Se suscribe al topic y empieza a recibir mensajes
            consumer.Subscribe(topic);

            try
            {
                //Bucle: lee un mensaje, lo procesa y confirma; después pasa al siguiente hasta que StopAsync pida parar
                while(!stoppingToken.IsCancellationRequested)
                {
                    //Consume() bloquea hasta que llega un mensaje; con el token lanza OperationCanceledException al parar
                    var consumerResult = consumer.Consume(stoppingToken);
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
            catch(OperationCanceledException)
            {
                //StopAsync pidió parar: no es un error
                _logger.LogInformation("El event consumer service se está deteniendo");
            }
            catch(Exception ex)
            {
                //Aquí nadie espera la tarea hasta StopAsync: sin este log el consumer moriría sin dejar rastro
                _logger.LogError(ex, "El event consumer service se ha detenido por un error");
                throw;
            }
            finally
            {
                //Close() confirma la salida del grupo para que Kafka reasigne las particiones sin esperar al timeout
                consumer.Close();
            }
        }

    }
}
