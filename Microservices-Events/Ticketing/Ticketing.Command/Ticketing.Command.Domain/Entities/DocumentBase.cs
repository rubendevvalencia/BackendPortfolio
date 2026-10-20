using MongoDB.Bson.Serialization.Attributes;

namespace Ticketing.Command.Domain.Entities
{
    public abstract class DocumentBase
    {
        //Es necesario para trabajar con mongoDb
        [BsonId] 
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        Object? Id {get; set; }
    }
}


