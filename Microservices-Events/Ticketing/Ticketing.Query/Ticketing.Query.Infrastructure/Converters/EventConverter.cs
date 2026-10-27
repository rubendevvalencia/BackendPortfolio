using System.Text.Json;
using System.Text.Json.Serialization;
using Ticketing.Common.Events;

namespace Ticketing.Query.Infrastructure.Converters
{
    public class EventConverter : JsonConverter<EventBase>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableFrom(typeof(EventBase));
        }

        public override EventBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(!JsonDocument.TryParseValue(ref reader, out var doc)) throw new Exception($"Fallo haciendo el parse de {nameof(JsonDocument)}");
            if(!doc.RootElement.TryGetProperty("Type", out var type)) throw new Exception("No puedo detectar la propiedad type");
            var isNumber = type.TryGetInt32(out var typeDiscriminator);
            if (!isNumber) throw new Exception("La propiedad Type no es un número válido");

           var json = doc.RootElement.GetRawText();
         
           
           return (eEventType)typeDiscriminator switch
           {
               eEventType.TicketCreatedEvent => JsonSerializer.Deserialize<TicketCreatedEvent>(json, options),
               eEventType.TicketUpdatedEvent => JsonSerializer.Deserialize<TicketUpdatedEvent>(json, options),
               _ => throw new Exception($"{typeDiscriminator} no es soportado")
           };
        }

        public override void Write(Utf8JsonWriter writer, EventBase value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

    }
}


