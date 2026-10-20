using System.Diagnostics.Tracing;
using MongoDB.Bson.Serialization.Attributes;
using Ticketing.Command.Domain.Common;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Entities.EventModels
{
    public class EventModel //Modelo base para los eventos
    {
        
        public EventBase? EventData {get; set; } //Los datos vendrán por aquí de forma dinámica
    }
}


