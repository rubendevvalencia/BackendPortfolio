namespace Ticketing.Command.Domain.Common
{
    //Etiqueta propia que indica en qué colección de Mongo se guarda una clase;
    //MongoDbContext la lee para elegirla
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BsonCollectionAttribute : Attribute
    {
        public string? CollectionName {get; set;}

        public BsonCollectionAttribute(string collectionName)
        {
            CollectionName = collectionName;
        }
    }
}

