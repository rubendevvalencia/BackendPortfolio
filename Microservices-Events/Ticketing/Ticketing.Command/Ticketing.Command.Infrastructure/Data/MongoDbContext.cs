using MongoDB.Driver;
using Ticketing.Command.Domain.Common;

namespace Ticketing.Command.Infrastructure.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        public IMongoClient Client { get; }

        public MongoDbContext(IMongoClient mongoClient, string databaseName)
        {
            Client = mongoClient;
            _database = mongoClient.GetDatabase(databaseName);
        }

        public IMongoCollection<TDocument> GetCollection<TDocument>()
        {
            string collectionName = GetCollectionName(typeof(TDocument));
            IMongoCollection<TDocument> collection = _database.GetCollection<TDocument>(collectionName);
            return collection;
        }

        private static string GetCollectionName(Type documentType)
        {
            object? attribute = documentType
                .GetCustomAttributes(typeof(BsonCollectionAttribute), true)
                .FirstOrDefault();

            if (attribute is null)
            {
                throw new InvalidOperationException($"{documentType.Name} no tiene [BsonCollection].");
            }

            BsonCollectionAttribute bsonCollection = (BsonCollectionAttribute)attribute;
            string? collectionName = bsonCollection.CollectionName;

            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new InvalidOperationException($"{documentType.Name} tiene un nombre de colección vacío.");
            }

            return collectionName;
        }
    }
}
