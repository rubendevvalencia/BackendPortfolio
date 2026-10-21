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

        //Devuelve la colección de Mongo de TDocument; 
        //El nombre sale del atributo [BsonCollection] de esa clase
        public IMongoCollection<TDocument> GetCollection<TDocument>()
        {
            string collectionName = GetCollectionName(typeof(TDocument));
            IMongoCollection<TDocument> collection = _database.GetCollection<TDocument>(collectionName);
            return collection;
        }
        
        //Reflexión: pregunta a la clase si lleva la etiqueta [BsonCollection]; 
        //.NET crea el atributo con el valor escrito en ella
        private static string GetCollectionName(Type documentType)
        {
           
            //reflexión: Como hemos declarado el atributo, automáticamente al elegir la colección, detecta las entidades
            object? attribute = documentType
                .GetCustomAttributes(typeof(BsonCollectionAttribute), true) 
                .FirstOrDefault();

            if (attribute is null)
            {
                throw new InvalidOperationException($"{documentType.Name} no tiene [BsonCollection].");
            }

            //Se convierte al tipo concreto para poder leer CollectionName (p. ej. "eventStore")
            BsonCollectionAttribute bsonCollection = (BsonCollectionAttribute)attribute;
            string? collectionName = bsonCollection.CollectionName;

            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new InvalidOperationException($"{documentType.Name} tiene un nombre de colección vacío.");
            }

            return collectionName;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return null;
        }
    }
}
