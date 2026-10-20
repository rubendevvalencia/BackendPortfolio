using Ticketing.Command.Domain.Entities.EventModels;

namespace Ticketing.Command.Infrastructure.Mapping
{
    public class Mapper
    {
        public EventModel ToEventModelMongoDb (EventModel model)
        {
               //Es necesario para trabajar con mongoDb
            //[BsonId] 
            //[BsonRepresentation(MongoDB.Bson.BsonType.String)]
            //Object? Id {get; set; }
            //[BsonElement("timestamp")]
            //public DateTime TimeStamp {get; set;} = DateTime.UtcNow;
            //
            //[BsonElement("aggregateIdentifier")]
            //public required string AggregateIdentifier {get; set;} = string.Empty;
            //
            //[BsonElement("aggregateType")]
            //public string AggregateType {get; set;} = string.Empty;
            //
            //[BsonElement("version")]
            //public int Version {get; set;} = 0;
            //
            //[BsonElement("eventType")]
            //public string EventType {get; set; }  = string.Empty;

            return null;
        }
    }
}


