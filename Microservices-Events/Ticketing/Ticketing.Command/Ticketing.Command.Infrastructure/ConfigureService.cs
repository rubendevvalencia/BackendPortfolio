using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Ticketing.Command.Infrastructure.Data;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Infrastructure.Interface;
using Ticketing.Command.Infrastructure.Session;
using Ticketing.Command.Infrastructure.Repository.UnitOfWork;
using Ticketing.Command.Infrastructure.Repository.EventModelRepository;
using Ticketing.Command.Infrastructure.Repository.MongoRepository;
using Ticketing.Command.Infrastructure.Mapping;
using Ticketing.Command.Domain.Interfaces.Events;
using Ticketing.Command.Infrastructure.Persistence;
using Confluent.Kafka;
using Ticketing.Common.Common.Producers.Interface;

namespace Ticketing.Command.Infrastructure
{
    public static class ConfigureService
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("MongoDbConnection");
            if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Falta ConnectionStrings:MongoDbConnection.");
            
            string? databaseName = configuration["MongoSettings:Database"];
            if (string.IsNullOrWhiteSpace(databaseName)) throw new InvalidOperationException("Falta MongoSettings:Database.");
            
            services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
            services.AddSingleton<MongoDbContext>(sp =>
            {
                IMongoClient client = sp.GetRequiredService<IMongoClient>();
                return new MongoDbContext(client, databaseName);
            });                                                //Reintenta automáticamente ante fallos transitorios

            //La colección del event store se registra una vez con su nombre; IMongoCollection es thread-safe y se comparte como singleton
            services.AddSingleton<IMongoCollection<EventModel>>(sp =>
            {
                MongoDbContext dbContext = sp.GetRequiredService<MongoDbContext>();
                IMongoCollection<EventModel> collection = dbContext.GetCollection<EventModel>("eventStore");
                return collection;
            });
            services.AddSingleton(new ClientSessionOptions
            {
                DefaultTransactionOptions = new TransactionOptions()
            });

           
            string? kafkaHostName = configuration["KafkaSettings:HostName"];
            if (string.IsNullOrWhiteSpace(kafkaHostName)) throw new InvalidOperationException("Falta KafkaSettings:HostName.");
            
            string? kafkaPort = configuration["KafkaSettings:Port"];
            if (string.IsNullOrWhiteSpace(kafkaPort)) throw new InvalidOperationException("Falta KafkaSettings:Port.");
            
            ProducerConfig producerConfig = new ProducerConfig
            {
                BootstrapServers = $"{kafkaHostName}:{kafkaPort}"
            };

            services.AddSingleton(producerConfig);
            services.AddSingleton<IEventProducer, TicketEventProducer>();


            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IMongoSessionContext, MongoSessionContext>();
            services.AddScoped<IEventModelRepository, EventModelRepository>();
            services.AddScoped<IEventStore, EventStore>();
            services.AddScoped(typeof(IMongoBaseRepository<>), typeof(MongoRepository<>));
           
           //Mapea a mongoDb para evitar tener problemas
            MongoClassMaps.Register();

            return services;
        }
    }
    
}


