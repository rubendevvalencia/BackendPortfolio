using System.Diagnostics.Tracing;
using MongoDB.Bson.Serialization.Attributes;
using Ticketing.Command.Domain.Common;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Entities
{
    [BsonCollection("eventStores")] //He declarado el atributo en common
    public class EventModelBase : DocumentBase //Modelo base para los eventos
    {
        [BsonElement("timestamp")]
        public DateTime TimeStamp {get; set;} = DateTime.UtcNow;

        [BsonElement("aggregateIdentifier")]
        public required string AggregateIdentifier {get; set;} = string.Empty;

        [BsonElement("aggregateType")]
        public string AggregateType {get; set;} = string.Empty;

        [BsonElement("version")]
        public int Version {get; set;} = 0;

        [BsonElement("eventType")]
        public string EventType {get; set; }  = string.Empty;

        public EventBase? EventData {get; set; } //Los datos vendrán por aquí de forma dinámica
    }
}


