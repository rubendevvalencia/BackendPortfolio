# Microservices-Events — Ticketing

Proyecto de microservicios **dirigidos por eventos** en **.NET 10** con **MongoDB**, **Kafka** y **PostgreSQL**. Es un proyecto de
aprendizaje independiente del monolito: no forma parte de la migración Strangler Fig, tiene su propia
solución y su propio Docker.

El objetivo no es la cantidad de funcionalidad sino entender, con código que funciona, cuatro ideas que
suelen explicarse solo en teoría:

- **CQRS a nivel de servicio:** el lado de escritura (`Ticketing.Command`) y el de consulta
  (`Ticketing.Query`) son servicios distintos, cada uno con sus proyectos.
- **Event Sourcing:** el estado no se guarda como una fila que se modifica, sino como una secuencia de
  eventos inmutables en un *event store*. El estado de un ticket es el resultado de reproducir sus eventos.
- **Transacciones en MongoDB:** sesiones y transacciones sobre un replica set, aisladas tras un Unit of
  Work para que la capa de aplicación no conozca el driver.
- **Mensajería con Kafka:** cada evento guardado se publica en un topic. Por ahora solo existe el productor;
  el servicio de consulta aún no consume.

## Piezas

| Pieza | Qué es |
|---|---|
| `Ticketing.Command` (Api, Application, Domain, Infrastructure) | Lado de escritura: `POST api/v1/TicketCreate/CreateTicket` crea un ticket, guarda su `TicketCreatedEvent` en el event store dentro de una transacción y lo publica en Kafka |
| `Ticketing.Common` | Contratos compartidos entre servicios: `Message`, `EventBase`, los eventos, `Response<T>`, `ErrorType` e `IEventProducer` |
| `Ticketing.Query` (Api, Application, Domain, Infrastructure) | Lado de lectura: modelo relacional en PostgreSQL con EF Core, migración `Initial` y el esqueleto del caso de uso `ReadTicket` (sin consumidor de Kafka todavía). Ver [*Estructura de Ticketing.Query*](docs/query-estructura.md) |
| Docker | MongoDB 7 como replica set de un nodo; Zookeeper, un broker de Kafka 3.7 y Kafka UI (`localhost:8080`); PostgreSQL 16 |
| CI | `ci-micro-events.yaml`: restaura, compila y ejecuta el proyecto de tests |

## Arquitectura

Clean Architecture con la misma regla que el monolito: **las dependencias apuntan siempre hacia dentro**,
hacia `Domain`.

### Visión general

```mermaid
flowchart LR
    Client([Cliente HTTP])
    CMD["<b>Ticketing.Command</b><br/>escritura · event sourcing"]
    QRY["<b>Ticketing.Query</b><br/>lectura · modelo relacional"]
    COM["<b>Ticketing.Common</b><br/>eventos · Response&lt;T&gt; · IEventProducer"]
    MONGO[("<b>MongoDB 7</b><br/>eventStore")]
    KAFKA[["<b>Kafka</b><br/>topic de eventos"]]
    PG[("<b>PostgreSQL 16</b><br/>tickets · employees")]

    Client -->|POST CreateTicket| CMD
    Client -->|GET ReadTicket| QRY
    CMD --> MONGO
    CMD -->|publica eventos| KAFKA
    KAFKA -.->|consumidor pendiente| QRY
    QRY --> PG
    CMD --> COM
    QRY --> COM
```

### Ticketing.Command

```mermaid
flowchart RL
    API["<b>Ticketing.Command.Api</b><br/>Controller · versionado · Swagger<br/>Composition root<br/><i>la única capa que conoce HTTP</i>"]
    APP["<b>Ticketing.Command.Application</b><br/>Command · handler (MediatR) · validador<br/>EventSourcingHandler"]
    DOM["<b>Ticketing.Command.Domain</b><br/>Agregado y servicio de dominio · EventModel<br/>Puertos: IUnitOfWork · IEventStore · IEventModelRepository<br/><i>cero paquetes externos</i>"]
    INF["<b>Ticketing.Command.Infrastructure</b><br/>UnitOfWork · EventStore · repositorio<br/>Mapeos BSON · índices · productor Kafka"]
    COM["<b>Ticketing.Common</b><br/>Message · eventos · Response&lt;T&gt; · ErrorType<br/>IEventProducer"]
    MONGO[("<b>MongoDB 7</b><br/>replica set rs0<br/>colección eventStore")]
    KAFKA[["<b>Kafka</b><br/>1 broker + Zookeeper<br/>topic de eventos"]]

    API --> APP
    APP --> DOM
    INF -.->|implementa| DOM
    API -.->|solo para registrar servicios| INF
    APP --> COM
    DOM --> COM
    API --> COM
    INF --> MONGO
    INF --> KAFKA
```

`Ticketing.Command.Domain` no tiene un solo `PackageReference`: los puertos viven ahí, en quien los *usa*, e
`Infrastructure` los implementa con el driver de MongoDB. Los componentes uno a uno, con el recorrido de la
petición, están en [*Arquitectura*](docs/arquitectura.md#vista-de-componentes).

### Ticketing.Query

```mermaid
flowchart RL
    API["<b>Ticketing.Query.Api</b><br/>Controller · composition root<br/>Migrate() al arrancar"]
    APP["<b>Ticketing.Query.Application</b><br/>Feature ReadTicket<br/>MediatR · FluentValidation"]
    DOM["<b>Ticketing.Query.Domain</b><br/>Ticket · TicketType · Employee<br/>BaseAudit · Address"]
    INF["<b>Ticketing.Query.Infrastructure</b><br/>TicketDbContext · configuraciones<br/>Migrations · Npgsql"]
    COM["<b>Ticketing.Common</b><br/>Response&lt;T&gt;"]
    PG[("<b>PostgreSQL 16</b><br/>eventspostgres")]

    API --> APP
    API -.->|solo para registrar servicios| INF
    API --> COM
    APP --> COM
    INF --> DOM
    INF --> PG
```

Estructura, modelo de datos y migraciones en [*Estructura de Ticketing.Query*](docs/query-estructura.md).

## Estructura

```
Microservices-Events/
├── Docker/docker-compose.yml          MongoDB (replica set rs0), Zookeeper, Kafka, Kafka UI y PostgreSQL
├── docs/                              Documentación (ver tabla siguiente)
└── Ticketing/
    ├── Ticketing.slnx
    ├── Ticketing.Core/
    │   └── Ticketing.Common/          Message, EventBase, eventos, Response<T>, ErrorType, IEventProducer
    ├── Ticketing.Command/
    │   ├── Ticketing.Command.Api/             Controllers, versionado, Swagger, composition root
    │   ├── Ticketing.Command.Application/     Command, handler y validador (MediatR); EventSourcingHandler
    │   ├── Ticketing.Command.Domain/          Agregados y su servicio, EventModel, enums, excepciones, puertos
    │   ├── Ticketing.Command.Infrastructure/  MongoDB: contexto, sesión, UoW, repositorio, EventStore, mapeos, índices; productor de Kafka
    │   └── Ticketing.Command.UnitTest/
    └── Ticketing.Query/
        ├── Ticketing.Query.Api/             Controller, versionado, composition root; Migrate() al arrancar
        ├── Ticketing.Query.Application/     Feature ReadTicket (MediatR, FluentValidation)
        ├── Ticketing.Query.Domain/          Entidades, BaseAudit y value object Address
        ├── Ticketing.Query.Infrastructure/  TicketDbContext, configuraciones de entidad y migraciones (PostgreSQL)
        └── Ticketing.Query.UnitTest/
```

## Documentación

| Documento | Qué cuenta |
|---|---|
| [Arquitectura](docs/arquitectura.md) | Proyectos y dependencias, arranque, registro en DI y recorrido completo de una petición |
| [Estructura de Ticketing.Query](docs/query-estructura.md) | Proyectos y dependencias del lado de lectura, modelo de datos en PostgreSQL, migraciones y lo que queda pendiente |
| [Agregados y event sourcing](docs/agregados.md) | Cómo se modela un ticket como agregado inmutable, qué significan sus versiones y cómo se reproduce desde sus eventos |
| [Event store y modelo de eventos](docs/event-store.md) | Cómo se guarda (y publica) un evento, el control de concurrencia, el mapeo a MongoDB y cómo añadir un evento nuevo |
| [Mensajería con Kafka](docs/mensajeria-kafka.md) | El compose de Kafka, los dos listeners, el productor, cuándo se publica cada evento y las limitaciones actuales |
| [Decisiones técnicas](docs/decisiones-tecnicas.md) | Las decisiones que tienen algo que explicar: replica set, UoW sobre Mongo, agregado inmutable, Kafka, dónde vive cada pieza |
| [Entorno local](docs/entorno-local.md) | Cómo levantar MongoDB y Kafka, ejecutar la API, probar el endpoint, tests y CI |

## Stack

.NET 10 · ASP.NET Core (controllers) · MongoDB 7 + `MongoDB.Driver` 3.12 · Kafka 3.7 + `Confluent.Kafka` 2.16 · PostgreSQL 16 + EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10 ·
MediatR 14 · FluentValidation 12 · Asp.Versioning 10 · Swashbuckle 10 · xUnit.

## Puesta en marcha rápida

```bash
docker compose -f Microservices-Events/Docker/docker-compose.yml up -d
dotnet run --project Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.Api
```

La API necesita MongoDB *healthy* antes de arrancar, y Kafka en marcha para poder crear tickets. Los
detalles y el porqué de cada paso, en
[*Entorno local*](docs/entorno-local.md).
