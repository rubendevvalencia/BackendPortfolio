using System.Text.Json;
using System.Text.Json.Serialization;
using Ticketing.Common.Events;

namespace Ticketing.Query.Infrastructure.Converters
{
    public class EventConverter : JsonConverter<EventBase>
    {
        //Sin override de CanConvert: por defecto el converter solo se aplica a EventBase.
        //Si se aplicara también a TicketCreatedEvent y compañía, al deserializarlos se llamaría a sí mismo sin parar

        public override EventBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //Lee el JSON completo del evento; el using libera el documento al salir del método
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            //El campo Type (enum eEventType) indica qué clase de evento es; debe llegar como número y con esa capitalización
            bool hasType = root.TryGetProperty("Type", out JsonElement typeElement);
            if(!hasType) throw new JsonException("No puedo detectar la propiedad Type");

            //Type llega como número (lo normal) o como texto con el nombre del enum
            eEventType eventType;
            if(typeElement.ValueKind == JsonValueKind.Number)
            {
                bool isNumber = typeElement.TryGetInt32(out int typeDiscriminator);
                if(!isNumber) throw new JsonException("La propiedad Type no es un número válido");
                eventType = (eEventType)typeDiscriminator;
            }
            else
            {
                string? typeName = typeElement.GetString();
                bool parsed = Enum.TryParse<eEventType>(typeName, out eventType);
                if(!parsed) throw new JsonException($"'{typeName}' no es un tipo de evento válido");
            }

            string json = root.GetRawText();

            //Con el tipo conocido se deserializa a la clase concreta
            return eventType switch
            {
                eEventType.TicketCreatedEvent => JsonSerializer.Deserialize<TicketCreatedEvent>(json, options),
                eEventType.TicketUpdatedEvent => JsonSerializer.Deserialize<TicketUpdatedEvent>(json, options),
                _ => throw new JsonException($"{eventType} no es soportado")
            };
        }

        //Este converter solo se usa para leer (consumer); escribir no está soportado
        public override void Write(Utf8JsonWriter writer, EventBase value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

    }
}


