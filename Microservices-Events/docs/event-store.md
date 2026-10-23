# Event store y modelo de eventos

[← Volver al README](../README.md)

Cómo se representa un evento y cómo acaba en MongoDB (y en Kafka). De dónde salen los eventos (el agregado)
está en [*Agregados y event sourcing*](agregados.md).

## Idea

En lugar de guardar "el ticket" y modificarlo, se guarda **cada cosa que le pasó** como un documento
inmutable. El estado actual de un ticket se obtiene reproduciendo sus eventos en orden. Todos los eventos
de todos los agregados viven en una única colección, `eventStore`, de la base de datos `ticketingApp`. El
event store **solo añade**: nunca modifica ni borra un evento guardado. Cada evento guardado se publica
además en Kafka; la fuente de verdad sigue siendo Mongo.

## Las tres capas de un evento

```
EventModel                      ← sobre: qué agregado, qué versión, cuándo, qué tipo
└── EventData : EventBase       ← carga: el evento concreto
        └── TicketCreatedEvent
```

### `EventModel` (Domain)

El registro que se guarda en la colección. Es un *envoltorio*: lleva los metadatos del evento y, dentro, el
evento en sí.

| Campo | Para qué |
|---|---|
| `Id` | Identificador del documento. Vacío al crear: Mongo genera el `ObjectId` al insertar. |
| `TimeStamp` | Momento en que se guardó el evento, en UTC. |
| `AggregateIdentifier` | A qué agregado (ticket) pertenece. Todos los eventos de un ticket comparten este valor. |
| `AggregateType` | Tipo de agregado (`TicketAggregate`), como enum `eAggregateType`. |
| `Version` | Posición del evento dentro de su agregado: 1, 2, 3… sin huecos. |
| `EventType` | Tipo de evento (`TicketCreatedEvent`), como enum `eEventType`. |
| `EventData` | El evento concreto (`EventBase`). |

La colección se elige con el atributo propio `[BsonCollection("eventStore")]` (`Domain/Common`). No es un
atributo del driver: lo lee `MongoDbContext` por reflexión, y por eso `Domain` no necesita referenciar
MongoDB. Si una clase no lo lleva, o lo lleva vacío, `GetCollection<T>` lanza `InvalidOperationException`.

### `Message` y `EventBase` (Common)

| Clase | Aporta |
|---|---|
| `Message` (abstracta) | `Id`, obligatorio (`required`). En los eventos de ticket es el **id del agregado**, no un id propio del evento. |
| `EventBase : Message` (abstracta) | `Type`: el nombre del evento, que el evento concreto pasa al constructor base con `nameof(...)`. |

`EventBase` no lleva versión: la versión vive solo en el sobre (`EventModel.Version`).

### `TicketCreatedEvent` (Common)

El único evento hoy:

| Campo | Viene de |
|---|---|
| `Id` | El id del ticket que genera `TicketAggregateService.Create` |
| `UserName` | `TicketCreateCommand.UserName` |
| `TyperError` | `TicketCreateCommand.ErrorType` |
| `DetailError` | `TicketCreateCommand.ErrorDetail` |

Lo construye a mano `TicketAggregateService.Create`; no hay librería de mapeo.

## Guardar eventos

`EventStore.SaveEventsAsync(aggregateId, aggregateType, events, expectedVersion)`, en
`Infrastructure/Persistence`. Lo llama `EventSourcingHandler.SaveAsync` con los cambios pendientes del
agregado, siempre dentro de la transacción que abrió el caso de uso.

1. Lee los eventos ya guardados del agregado (`FindByAggregateIdAsync`, dentro de la sesión).
2. Calcula la última versión guardada: 0 si no hay ninguno, el máximo de `Version` si los hay.
3. Si no coincide con `expectedVersion`, lanza **`ConcurrencyException`**: alguien guardó eventos de ese
   agregado después de que se cargara.
4. Por cada evento, sube la versión en uno (`expectedVersion + 1`, `+ 2`…) y lo envuelve en un `EventModel`
   con `TimeStamp = DateTime.UtcNow`, el agregado, la versión y `EventType = Enum.Parse<eEventType>(event.Type)`.
   **Convención:** el `Type` del evento debe coincidir con un valor de `eEventType`, o falla en tiempo de
   ejecución.
5. Inserta cada `EventModel` con `IEventModelRepository.AddAsync`.
6. Justo después de cada inserción, publica ese mismo evento en Kafka con `IEventProducer.ProduceAsync`,
   en el topic de `KafkaSettings:Topic` (si falta, lanza `Exception("No se encontró el topic")`). Se hace
   dentro de la transacción y antes del `Commit`: si el envío falla, la transacción se aborta; si falla el
   `Commit`, el evento ya está en Kafka. Detalle y límites en
   [*Mensajería con Kafka*](mensajeria-kafka.md#cuándo-se-publica).

## Control de concurrencia

Concurrencia **optimista**, en dos capas:

| Capa | Dónde | Qué detecta |
|---|---|---|
| Comprobación de versión | `EventStore.SaveEventsAsync` (paso 3) | Que otra operación ya **confirmó** eventos del agregado desde que este se cargó. Es la comprobación clásica de event sourcing y da un error claro antes de escribir. |
| Índice único `ux_aggregate_version` | `MongoIndexes`, sobre (`AggregateIdentifier`, `Version`) | Que dos operaciones intenten guardar **la misma versión** a la vez: la comprobación anterior la pasan las dos, el índice solo deja entrar una. |

El índice es la garantía real; la comprobación de versión es la que da el mensaje. `EventModelRepository`
captura el `MongoWriteException` de clave duplicada (`ServerErrorCategory.DuplicateKey`) y lo convierte en
`ConcurrencyException` con la excepción original como `InnerException`, para que `Domain` hable de
concurrencia y no de Mongo.

El índice se crea al arrancar (`EnsureMongoIndexesAsync`), fuera de cualquier transacción, y
`CreateOneAsync` es idempotente: si ya existe con la misma definición, no hace nada.

## Leer eventos

| Método | Qué hace |
|---|---|
| `IEventModelRepository.FindByAggregateIdAsync(aggregateIdentifier)` | Todos los `EventModel` de un agregado, sin orden garantizado. Usa la sesión del contexto si la hay. Aprovecha el índice `ux_aggregate_version` (su primer campo es `AggregateIdentifier`). |
| `IEventStore.GetEventsAsync(aggregateId)` | Los eventos (`EventData`) de un agregado, **ordenados por `Version`**. Si no hay ninguno, lanza `AggregateNotFoundException`. Es lo que reproduce `EventSourcingHandler.GetByIdAsync`. |

## Mapeo a BSON (`MongoClassMaps`)

Toda la configuración de Mongo vive en `Infrastructure/Mapping/MongoClassMaps.cs`, para que `Domain` y
`Common` sigan sin conocer el driver. Se registra una vez al arrancar, desde `AddInfrastructureServices`.

| Qué | Cómo | Por qué |
|---|---|---|
| `EventModel.Id` | `string` guardado como `ObjectId`, con `StringObjectIdGenerator` | Mongo lo genera si llega vacío. Con un `int` no autoincrementaría. |
| Enums (`AggregateType`, `EventType`) | `EnumSerializer` con `BsonType.String`: se guardan como **texto** | El documento es legible al inspeccionarlo. |
| `EventBase` | `AutoMap` + clase raíz (`SetIsRootClass`) + `AddKnownType(TicketCreatedEvent)` | Habilita el polimorfismo: Mongo guarda `_t` con el tipo concreto y sabe reconstruirlo al leer, incluso si la subclase no se ha usado antes. |
| `Message.Id` (dentro de `EventData`) | Convención de `AutoMap`: una propiedad `Id` es el `_id` | No se configura; por eso dentro de `EventData` aparece `_id` con el id del ticket, como `string`. |
| `TicketCreatedEvent` | `AutoMap` + discriminador `"TicketCreatedEvent"` | Valor estable de `_t`, independiente del nombre de la clase en C#. |

Cada `RegisterClassMap` va protegido con `IsClassMapRegistered`, porque el driver lanza excepción si se
registra dos veces (algo que ocurriría, por ejemplo, al levantar varios hosts de test en el mismo proceso).

El discriminador está fijado, pero **los nombres de campo no**: `AutoMap` usa el nombre de cada propiedad
C#. Renombrar una propiedad de un evento cambia el campo en los documentos nuevos y deja los antiguos sin
leer ese dato, sin error.

### Cómo se ve un documento

Orientativo, según el mapeo anterior:

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
    "Type": "TicketCreatedEvent",
    "UserName": "...",
    "TyperError": "...",
    "DetailError": "..."
  }
}
```

`AggregateIdentifier` y `EventData._id` llevan el mismo valor.

## Índices de `eventStore`

| Nombre | Campos | Opciones | Para qué |
|---|---|---|---|
| `_id_` | `_id` | Único (lo crea Mongo) | Identidad del documento |
| `ux_aggregate_version` | `AggregateIdentifier` ↑, `Version` ↑ | Único | Concurrencia y lectura por agregado |

## Cómo añadir un evento nuevo

1. Clase en `Ticketing.Common/Events/` que herede de `EventBase` y pase `nameof(...)` al constructor base.
2. Valor nuevo en `eEventType`, **con el mismo nombre** que la clase (lo exige `Enum.Parse` en `EventStore`).
3. Registrar su mapeo en `MongoClassMaps` (`RegisterClassMap` + `SetDiscriminator`), llamarlo desde
   `Register()` y añadirlo con `AddKnownType` en el mapeo de `EventBase`.
4. Su `case` en el `ApplyEvent` del servicio del agregado (ver [*Agregados*](agregados.md)).
5. Command, handler y validador en `Application/Features/<Agregado>/<Orden>/`.

La publicación en Kafka no necesita cambios: `TicketEventProducer` serializa el evento con su tipo concreto.

Un **agregado** nuevo necesita además su valor en `eAggregateType`, con el mismo nombre que la clase (lo
exige `Enum.Parse` en `EventSourcingHandler`), y su registro en DI.
