using System.Diagnostics.Tracing;
using Ticketing.Command.Domain.Common;
using Ticketing.Command.Domain.Enum;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Entities.EventModels
{
    [BsonCollection("eventStore")]
    public class EventModel //Modelo base para los eventos
    {
        public int Id {get; set; }
        public DateTime TimeStamp {get; set;} = DateTime.UtcNow;
        public required string AggregateIdentifier {get; set;} = string.Empty;
        public string AggregateType {get; set;} = string.Empty;
        public int Version {get; set;} = 0;
        public EventType Type {get; set; }
        public EventBase? EventData {get; set; } //Los datos vendrán por aquí de forma dinámica
    }
}


