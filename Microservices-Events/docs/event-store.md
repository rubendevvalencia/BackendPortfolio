# Event store y modelo de eventos

[← Volver al README](../README.md)

Cómo se representa un evento y cómo acaba en MongoDB.

## Idea

En lugar de guardar "el ticket" y modificarlo, se guarda **cada cosa que le pasó** como un documento
inmutable. El estado actual de un ticket se obtendría reproduciendo sus eventos en orden. Todos los eventos
viven en una única colección, `eventStore`.

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
| `TimeStamp` | Momento del evento, en UTC. |
| `AggregateIdentifier` | A qué agregado (ticket) pertenece. Todos los eventos de un ticket comparten este valor. |
| `AggregateType` | Tipo de agregado (`TicketAggregate`). |
| `Version` | Posición del evento dentro de su agregado. |
| `EventType` | Tipo de evento (`TicketCreatedEvent`), como enum. |
| `EventData` | El evento concreto (`EventBase`). |

La colección se elige con el atributo propio `[BsonCollection("eventStore")]`. No es un atributo del
driver: lo lee `MongoDbContext` por reflexión, y por eso `Domain` no necesita referenciar MongoDB.

### `EventBase` y `Message` (Common)

`Message` aporta el `Id`; `EventBase` añade `Version` y `Type` (el nombre del evento, tomado con
`nameof(...)` en el constructor del evento concreto). Cada evento nuevo hereda de `EventBase`.

### `TicketCreatedEvent` (Common)

El único evento hoy: `UserName`, `TyperError` y `DetailError`. Nace del `TicketCreateCommand` con AutoMapper;
`MappingProfile` renombra `ErrorType → TyperError` y `ErrorDetail → DetailError`, e ignora `Id`, `Version` y
`Type`.

## Mapeo a BSON (`MongoClassMaps`)

Toda la configuración de Mongo vive en `Infrastructure/Mapping/MongoClassMaps.cs`, para que `Domain` y
`Common` sigan sin conocer el driver. Se registra una vez al arrancar, desde `AddInfrastructureServices`.

| Qué | Cómo | Por qué |
|---|---|---|
| `EventModel.Id` | `string` guardado como `ObjectId`, con `StringObjectIdGenerator` | Mongo lo genera si llega vacío. Con un `int` no autoincrementaría. |
| Enums (`AggregateType`, `EventType`) | Se guardan como **texto** | El documento es legible al inspeccionarlo. |
| `EventBase` | Clase raíz (`SetIsRootClass`) con `AddKnownType(TicketCreatedEvent)` | Habilita el polimorfismo: Mongo guarda `_t` con el tipo concreto y sabe reconstruirlo al leer. |
| `TicketCreatedEvent` | Discriminador `"TicketCreatedEvent"` | Valor estable de `_t`, independiente del nombre de la clase en C#. |

Cada `RegisterClassMap` va protegido con `IsClassMapRegistered`, porque el driver lanza excepción si se
registra dos veces (algo que ocurriría, por ejemplo, al levantar varios hosts de test en el mismo proceso).

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
    "Version": 0,
    "Type": "TicketCreatedEvent",
    "UserName": "...",
    "TyperError": "...",
    "DetailError": "..."
  }
}
```

## Lectura

`IEventModelRepository.FindByAggregateIdAsync(aggregateIdentifier)` devuelve todos los eventos de un
agregado: es la pieza sobre la que se apoyaría la reconstrucción del estado.

## Cómo añadir un evento nuevo

1. Clase en `Ticketing.Common/Events/` que herede de `EventBase` y pase `nameof(...)` al constructor base.
2. Valor nuevo en `eEventType`.
3. Registrar su mapeo en `MongoClassMaps` (`RegisterClassMap` + `SetDiscriminator`) y añadirlo con
   `AddKnownType` en el mapeo de `EventBase`.
4. Command, handler y validador en `Application/Feature/...`, y la entrada en `MappingProfile`.
