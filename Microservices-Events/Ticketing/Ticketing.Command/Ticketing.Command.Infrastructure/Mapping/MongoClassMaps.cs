using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Enum;
using Ticketing.Common.Events;

namespace Ticketing.Command.Infrastructure.Mapping
{
    //Toda la configuración de Mongo vive aquí, así el dominio y Ticketing.Common no conocen MongoDB
    public static class MongoClassMaps
    {
        public static void Register()
        {
            RegisterEventModel();
            RegisterEventBase();
            RegisterTicketCreatedEvent();
        }

        private static void RegisterEventModel()
        {
            //RegisterClassMap lanza excepción si se registra dos veces (p. ej. en tests de integración)
            bool isRegistered = BsonClassMap.IsClassMapRegistered(typeof(EventModel));
            if (isRegistered) return;
            
            BsonClassMap.RegisterClassMap<EventModel>(map =>
            {
                map.AutoMap();

                //Id string guardado como ObjectId; Mongo lo genera si llega vacío
                map.MapIdMember(eventModel => eventModel.Id)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId))
                    .SetIdGenerator(StringObjectIdGenerator.Instance);

                //Los enums se guardan como texto para que el documento sea legible
                map.MapMember(eventModel => eventModel.AggregateType)
                    .SetSerializer(new EnumSerializer<eAggregateType>(BsonType.String));
                map.MapMember(eventModel => eventModel.EventType)
                    .SetSerializer(new EnumSerializer<eEventType>(BsonType.String));
            });
        }

        private static void RegisterEventBase()
        {
            bool isRegistered = BsonClassMap.IsClassMapRegistered(typeof(EventBase));
            if (isRegistered) return;
            
            BsonClassMap.RegisterClassMap<EventBase>(map =>
            {
                map.AutoMap();
                map.MapIdMember(eventBase => eventBase.Id);

                //Clase raíz: Mongo guarda el campo "_t" con el tipo concreto del evento
                map.SetIsRootClass(true);

                //Sin esto, al leer un EventBase Mongo no conoce las subclases hasta que alguien las haya usado
                map.AddKnownType(typeof(TicketCreatedEvent));
            });
        }

        private static void RegisterTicketCreatedEvent()
        {
            bool isRegistered = BsonClassMap.IsClassMapRegistered(typeof(TicketCreatedEvent));
            if (isRegistered) return;
            
            BsonClassMap.RegisterClassMap<TicketCreatedEvent>(map =>
            {
                map.AutoMap();
                map.SetDiscriminator(nameof(TicketCreatedEvent));
            });
        }
    }
}
