using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Ticketing.Command.Domain.Interface.EventModels;
using Ticketing.Command.Infrastructure.Data;
using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;
using Ticketing.Command.Infrastructure.Interface;
using Ticketing.Command.Infrastructure.Interface.Session;
using Ticketing.Command.Infrastructure.Session;
using Ticketing.Command.Infrastructure.Repository.UnitOfWork;
using Ticketing.Command.Infrastructure.Repository.EventModelRepository;
using Ticketing.Command.Infrastructure.Repository.MongoRepository;
using Ticketing.Common.Events;

namespace Ticketing.Command.Infrastructure
{
    public static class ConfigureService
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("MongoDbConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Falta ConnectionStrings:MongoDbConnection.");
            }
            string? databaseName = configuration["MongoSettings:Database"];
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("Falta MongoSettings:Database.");
            }
            services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
            services.AddSingleton<MongoDbContext>(sp =>
            {
                IMongoClient client = sp.GetRequiredService<IMongoClient>();
                return new MongoDbContext(client, databaseName);
            });                                                //Reintenta automáticamente ante fallos transitorios

            services.AddScoped<IMongoSessionContext, MongoSessionContext>();
            services.AddScoped<ISession, MongoSession>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
            services.AddScoped<IEventModelRepository, EventModelRepository>();
          
            BsonClassMap.RegisterClassMap<EventBase>();
            BsonClassMap.RegisterClassMap<TicketCreatedEvent>();

            return services;
        }
    }
    
}


