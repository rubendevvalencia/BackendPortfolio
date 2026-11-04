using System.Diagnostics.Tracing;
using Ticketing.Command.Domain.Enums;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Entities.EventModels
{
    //El nombre de la colección de Mongo ("eventStore") se fija en Infrastructure, al registrar IMongoCollection<EventModel>
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


