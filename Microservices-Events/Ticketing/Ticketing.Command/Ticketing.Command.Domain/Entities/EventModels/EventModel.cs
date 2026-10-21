using System.Diagnostics.Tracing;
using Ticketing.Command.Domain.Common;
using Ticketing.Command.Domain.Enum;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Entities.EventModels
{
    [BsonCollection("eventStore")] //Esto es una collection personalizada, no tiene que ver con MongoDb
    public class EventModel //Modelo base para los eventos
    {
        public string Id {get; set; } = string.Empty; //Vacío al crear: Mongo genera el ObjectId al insertar, si se usa int mongodb no autoincrementa
        public DateTime TimeStamp {get; set;} = DateTime.UtcNow;
        public required string AggregateIdentifier {get; set;} = string.Empty;
        public eAggregateType AggregateType {get; set;}
        public int Version {get; set;} = 0;
        public eEventType EventType {get; set; }
        public EventBase? EventData {get; set; } //Los datos vendrán por aquí de forma dinámica
    }
}


