using MongoDB.Driver;

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

        //Devuelve la colección de Mongo de TDocument con el nombre indicado;
        //el nombre de cada colección se decide al registrarla en ConfigureService
        public IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName)
        {
            IMongoCollection<TDocument> collection = _database.GetCollection<TDocument>(collectionName);
            return collection;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return null;
        }
    }
}
