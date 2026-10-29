# Event store y modelo de eventos

[← Volver al README](../../README.md)

Cómo se representa un evento y cómo acaba en MongoDB (y en Kafka). De dónde salen los eventos, en
[*Agregados*](agregados.md).

## Idea

Se guarda **cada cosa que le pasó** a un ticket como un documento inmutable; el estado actual sale de reproducir
sus eventos. Todos los eventos de todos los agregados viven en una colección, `eventStore`, de la base
`ticketingApp`. El event store **solo añade**: nunca modifica ni borra. Cada evento guardado se publica además en
Kafka; la fuente de verdad es Mongo.

```
EventModel                      ← sobre: agregado, versión, cuándo, tipo
└── EventData : EventBase       ← carga: el evento concreto
        └── TicketCreatedEvent
```

### `EventModel` (Domain)

| Campo | Para qué |
|---|---|
| `Id` | Documento. Vacío al crear: Mongo genera el `ObjectId` |
| `TimeStamp` | Cuándo se guardó, en UTC |
| `AggregateIdentifier` | A qué ticket pertenece; común a todos sus eventos |
| `AggregateType` / `EventType` | Enums `eAggregateType` y `eEventType` |
| `Version` | Posición dentro del agregado: 1, 2, 3… sin huecos |
| `EventData` | El evento concreto (`EventBase`) |

El nombre de la colección (`eventStore`) no vive en `Domain`: se fija en `Infrastructure`, al registrar
`IMongoCollection<EventModel>` (`GetCollection<T>(collectionName)`); los repositorios la reciben por constructor
(ver [*Decisiones*](decisiones-tecnicas.md#la-colección-se-registra-por-su-nombre-en-di)).

### `Message`, `EventBase` y `TicketCreatedEvent` (Common)

| Clase | Aporta |
|---|---|
| `Message` | Obligatorios: `Id` (en eventos de ticket, el **id del agregado**), `OcurredAt` (UTC) y `OcurredBy` (`string?`, quién lo provocó) |
| `EventBase : Message` | `Type`, el `eEventType` del evento (viaja como **número** en el JSON de Kafka). No lleva versión: esa vive en `EventModel.Version` |
| `TicketCreatedEvent` | `ErrorDetail`. Único evento que `Command` genera hoy (`TicketUpdatedEvent` existe, pero nada lo publica) |

`TicketAggregateService.Create` lo construye a mano: `Id` = id generado, `OcurredAt` = `DateTime.UtcNow`,
`OcurredBy` = `TicketCreateCommand.UserName`, `ErrorDetail` = `TicketCreateCommand.ErrorDetail`. El usuario viaja
en `OcurredBy` (ya no hay campo `UserName`) y el tipo de error no se incluye. `TicketUpdatedEvent` conserva su
`DetailError` y su `Username`.

> **Pendiente en `Query`:** `EventHandler.On(TicketCreatedEvent)` todavía lee `@event.UserName`; tiene que leer
> `@event.OcurredBy`.

## Guardar eventos

`EventStore.SaveEventsAsync(aggregateId, aggregateType, events, expectedVersion)` (`Infrastructure/Persistence`),
llamado por `SaveAsync` dentro de la transacción del caso de uso:

1. Lee los eventos ya guardados del agregado y calcula la última versión (0 si no hay).
2. Si no coincide con `expectedVersion`, lanza **`ConcurrencyException`**.
3. Por cada evento, sube la versión en uno y lo envuelve en un `EventModel` (`TimeStamp = UtcNow`,
   `EventType = event.Type`).
4. Lo inserta con `IEventModelRepository.AddAsync`.
5. Justo después lo publica en Kafka (`IEventProducer.ProduceAsync`, topic `KafkaTopics.TicketEvents`), antes del
   `Commit`. Detalle y límites en [*Mensajería*](../mensajeria-kafka.md#cuándo-se-publica).

## Control de concurrencia

Concurrencia **optimista**, en dos capas:

| Capa | Qué detecta |
|---|---|
| Comprobación de versión (`EventStore`, paso 2) | Que otra operación ya **confirmó** eventos del agregado desde que se cargó. Da un error claro antes de escribir |
| Índice único `ux_aggregate_version` sobre (`AggregateIdentifier`, `Version`) | Que dos operaciones guarden **la misma versión** a la vez: las dos pasan la comprobación, el índice solo deja entrar una |

El índice es la garantía real. `EventModelRepository` captura el `MongoWriteException` de clave duplicada y lo
convierte en `ConcurrencyException` (con la original como `InnerException`), para que `Domain` hable de
concurrencia y no de Mongo. El índice se crea al arrancar (`EnsureMongoIndexesAsync`), fuera de transacción, y es
idempotente. Índices de `eventStore`: `_id_` (único) y `ux_aggregate_version` (único, también sirve para leer por
agregado).

## Leer eventos

- `IEventModelRepository.FindByAggregateIdAsync(id)`: todos los `EventModel` del agregado, sin orden garantizado,
  con la sesión del contexto si la hay.
- `IEventStore.GetEventsAsync(id)`: los `EventData` **ordenados por `Version`**; sin eventos, lanza
  `AggregateNotFoundException`. Es lo que reproduce `GetByIdAsync`.

## Mapeo a BSON (`MongoClassMaps`)

Toda la configuración de Mongo está en `Infrastructure/Mapping`, para que `Domain` y `Common` no conozcan el
driver. Se registra una vez al arrancar, y cada `RegisterClassMap` se protege con `IsClassMapRegistered` (el
driver falla si se registra dos veces).

| Qué | Cómo | Por qué |
|---|---|---|
| `EventModel.Id` | `string` como `ObjectId` (`StringObjectIdGenerator`) | Mongo lo genera si llega vacío |
| Enums `AggregateType`, `EventType` | `EnumSerializer` a `BsonType.String` | Documento legible |
| `EventBase` | `AutoMap` + clase raíz + `AddKnownType(TicketCreatedEvent)` | Polimorfismo: `_t` guarda el tipo concreto |
| `Message.Id` (en `EventData`) | Convención de `AutoMap`: `Id` es el `_id` | Sin configurar |
| `TicketCreatedEvent` | `AutoMap` + discriminador `"TicketCreatedEvent"` | `_t` estable, independiente del nombre de la clase |

El discriminador está fijado, pero **los nombres de campo no**: renombrar una propiedad de un evento cambia el campo
en los documentos nuevos y deja los antiguos sin ese dato, sin error. Documento orientativo:

```json
{
  "_id": { "$oid": "..." },
  "TimeStamp": { "$date": "..." },
  "AggregateIdentifier": "019...-....",
  "AggregateType": "TicketAggregate",
  "Version": 1,
  "EventType": "TicketCreatedEvent",
  "EventData": {
    "_t": "TicketCreatedEvent",
    "_id": "019...-....",
    "Type": 1,
    "OcurredAt": { "$date": "..." },
    "OcurredBy": "...",
    "ErrorDetail": "..."
  }
}
```

`EventData.Type` no tiene serializador propio, así que se espera el número del enum (comportamiento por defecto
del driver); no he abierto el documento real para confirmarlo.

## Cómo añadir un evento nuevo

1. Clase en `Ticketing.Common/Events/` que herede de `EventBase` y pase su `eEventType` al constructor.
2. Valor nuevo en `eEventType` con el **siguiente número libre** (los existentes no se tocan: ya viajan por Kafka).
3. Su mapeo en `MongoClassMaps` (`RegisterClassMap` + `SetDiscriminator`), llamarlo desde `Register()` y añadirlo
   con `AddKnownType` en el de `EventBase`.
4. Su `case` en el `ApplyEvent` del servicio del agregado.
5. Command, handler y validador en `Application/Features/<Agregado>/<Orden>/`.

Kafka no necesita cambios (el productor serializa el tipo concreto), pero el consumidor de `Query` sí: un `case` en
`EventConverter`, un `On` en `IEventHandler`/`EventHandler` y su proyección. Un **agregado** nuevo necesita además
su valor en `eAggregateType` (mismo nombre que la clase) y su registro en DI.
