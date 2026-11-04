using System.Text.Json;
using System.Text.Json.Serialization;
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
        //Opciones de lectura del JSON, creadas una sola vez: System.Text.Json cachea los metadatos por instancia de opciones
        //EventConverter lee el campo Type para saber qué clase de evento crear; JsonStringEnumConverter acepta el enum como número o como texto
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new EventConverter(),
                new JsonStringEnumConverter()
            }
        };

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


        //Consume() bloquea el hilo que lo llama: si el bucle corriera aquí, StartAsync no volvería nunca y el host no terminaría de arrancar
        //Por eso el bucle se lanza en otro hilo del pool con Task.Run y StartAsync vuelve enseguida
        //StartAsync no se conecta a Kafka: solo prepara el interruptor de parada y lanza el bucle. La conexión ocurre dentro de ConsumeAsync
        public Task StartAsync(CancellationToken cancellationToken)
        {
            //Interruptor propio del servicio: StopAsync lo activa (Cancel) para pedirle al bucle que pare
            _stoppingSource = new CancellationTokenSource();
            //Token que ve el bucle; se cancela cuando StopAsync llama a Cancel
            CancellationToken stoppingToken = _stoppingSource.Token;

            //Ese hilo del pool queda ocupado mientras la app viva; con un solo consumer es un coste aceptable
            //Se guarda la tarea para que StopAsync pueda esperar a que el bucle termine y cierre el consumer
            _consuming = Task.Run(() => ConsumeAsync(stoppingToken));

            //Devuelve ya, sin esperar al bucle: así el host sigue arrancando y la API HTTP queda disponible
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
                //Es un único hilo y es secuencial: no hay dos mensajes procesándose a la vez en esta instancia
                while(!stoppingToken.IsCancellationRequested)
                {
                    //Consume() bloquea solo este hilo del pool hasta que llega un mensaje; las peticiones HTTP de la API se atienden en otros hilos
                    //Con el token lanza OperationCanceledException al parar
                    var consumerResult = consumer.Consume(stoppingToken);
                    if(consumerResult is null) continue;
                    if(consumerResult.Message is null) continue;

                    //Convierte el JSON en un evento concreto (TicketCreatedEvent, TicketUpdatedEvent...) usando EventBase como tipo común
                    var @event = JsonSerializer.Deserialize<EventBase>(consumerResult.Message.Value, _jsonOptions);

                    if(@event is null) throw new ArgumentNullException(nameof(@event), "No se pudo procesar el json parser");

                    //El servicio es singleton y el handler usa servicios scoped (MediatR, DbContext): se crea un scope por mensaje
                    //Así cada mensaje tiene su propio DbContext y nunca se comparte uno entre hilos
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
