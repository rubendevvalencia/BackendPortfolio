using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Infrastructure.Data;

namespace Ticketing.Command.Infrastructure.Mapping
{
    //Los índices de Mongo se crean al arrancar, fuera de cualquier transacción
    public static class MongoIndexes
    {
        public static async Task EnsureMongoIndexesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
        {
            MongoDbContext dbContext = services.GetRequiredService<MongoDbContext>();
            IMongoCollection<EventModel> collection = dbContext.GetCollection<EventModel>();

            //Un agregado no puede tener dos eventos con la misma versión: es lo que garantiza la concurrencia de verdad
            IndexKeysDefinition<EventModel> keys = Builders<EventModel>.IndexKeys
                .Ascending(eventModel => eventModel.AggregateIdentifier)
                .Ascending(eventModel => eventModel.Version);

            CreateIndexOptions options = new CreateIndexOptions();
            options.Unique = true;
            options.Name = "ux_aggregate_version";

            CreateIndexModel<EventModel> indexModel = new CreateIndexModel<EventModel>(keys, options);
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
        }
    }
}
