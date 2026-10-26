# Arquitectura de Ticketing.Query

[← Volver al README](../../README.md) · [Arquitectura general](../arquitectura.md)

Microservicio de **lectura**, independiente de `Ticketing.Command`: tiene sus propias capas, su propio
`Program.cs` y su propia base de datos (PostgreSQL), y no referencia ningún proyecto del otro servicio. No usa
event sourcing: el estado vive en tablas relacionales. Cómo encaja con `Command`, en la
[arquitectura general](../arquitectura.md); las decisiones, en [*Decisiones técnicas*](decisiones-tecnicas.md).

## Qué hay

| Antes | Ahora |
|---|---|
| Solo `Ticketing.Query.Api` | `Api`, `Application`, `Domain`, `Infrastructure` y `UnitTest` |
| Sin base de datos | PostgreSQL 16 con EF Core y Npgsql |
| Sin modelo | `Ticket`, `TicketType`, `Employee`, `BaseAudit` y `Address` |
| Sin casos de uso | Feature `ReadTicket` (comando, handler y validador) con MediatR |
| Sin migraciones | Migración `Initial` aplicada al arrancar |

## Dependencias entre proyectos

Las dependencias apuntan hacia dentro.

```mermaid
flowchart RL
    API["<b>Ticketing.Query.Api</b><br/>Controller · composition root<br/>Migrate() al arrancar"]
    APP["<b>Ticketing.Query.Application</b><br/>Feature/ReadTicket<br/>MediatR · FluentValidation"]
    DOM["<b>Ticketing.Query.Domain</b><br/>Entidades · BaseAudit · Address"]
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

- `Domain` no tiene paquetes de EF Core.
- `Application` referencia solo `Ticketing.Common`.
- `Microsoft.EntityFrameworkCore.Design` está en `Api` (proyecto de inicio) e `Infrastructure`, con
  `PrivateAssets=all`, para que `dotnet ef` funcione con las migraciones en `Infrastructure`.

## Vista de componentes

Cada recuadro es una clase o un conjunto de clases reales de `Ticketing.Query`. El handler de `ReadTicket`
todavía no usa `TicketDbContext`: hoy la base de datos solo se toca al arrancar, con `Migrate()`.

```mermaid
flowchart LR
    Client([Cliente HTTP])

    subgraph API["Api"]
        direction TB
        Ctrl["TicketCreate<br/>GET api/v1/TicketCreate/ReadTicket"]
        Prog["Program.cs<br/>Migrate() al arrancar"]
    end

    subgraph APP["Application"]
        direction TB
        Cmd["ReadTicketCommand"]
        Handler["ReadTicketCommandHandler<br/>sin implementar"]
        Val["ReadTicketValidator"]
    end

    subgraph DOM["Domain"]
        direction TB
        Ent["Ticket · TicketType · Employee<br/>BaseAudit · Address"]
    end

    subgraph INF["Infrastructure"]
        direction TB
        Ctx["TicketDbContext"]
        Cfg["TicketConfiguration · EmployeeConfiguration<br/>TicketTypeConfiguration"]
        Mig["Migrations<br/>Initial"]
    end

    subgraph DB["PostgreSQL 16 · eventspostgres"]
        direction TB
        Tables[("tickets · employees<br/>ticket_types · ticket_employees")]
    end

    Client -->|JSON| Ctrl
    Ctrl -->|MediatR Send| Cmd
    Cmd --> Handler
    Val -.->|valida| Cmd
    Prog -->|Migrate| Ctx
    Ctx -->|ApplyConfigurationsFromAssembly| Cfg
    Cfg -->|mapea| Ent
    Mig -->|crea el esquema| Tables
    Ctx -->|Npgsql| Tables

    classDef api fill:#dbeafe,stroke:#2563eb,color:#0f172a
    classDef app fill:#dcfce7,stroke:#16a34a,color:#0f172a
    classDef dom fill:#fef9c3,stroke:#ca8a04,color:#0f172a
    classDef inf fill:#ffedd5,stroke:#ea580c,color:#0f172a
    classDef db fill:#e2e8f0,stroke:#475569,color:#0f172a
    class Ctrl,Prog api
    class Cmd,Handler,Val app
    class Ent dom
    class Ctx,Cfg,Mig inf
    class Tables db
```

## Árbol de carpetas

```
Ticketing.Query/
├── Ticketing.Query.Api/
│   ├── Controllers/                 TicketCreate, ActionResultController
│   ├── Modules/Versioning/
│   ├── Program.cs                   Secretos, DI, Migrate(), pipeline
│   └── appsettings.json             ConnectionStrings:PostgresConnection
├── Ticketing.Query.Application/
│   ├── ApplicationServices.cs       AddApplicationServices()
│   └── Feature/ReadTicket/          Command · CommandHandler · Validator
├── Ticketing.Query.Domain/
│   ├── Entities/                    Ticket · TicketType · Employee
│   │   └── Audit/BaseAudit.cs
│   └── ValueObjects/Address.cs
├── Ticketing.Query.Infrastructure/
│   ├── ConfigurationServices.cs     AddInfrastructureServices()
│   ├── Data/TicketDbContext.cs
│   ├── Persistence/                 Una IEntityTypeConfiguration por entidad
│   └── Migrations/
└── Ticketing.Query.UnitTest/
```

## Modelo de datos

```mermaid
erDiagram
    ticket_types ||--o{ tickets : "TicketTypeId"
    tickets }o--o{ employees : "ticket_employees"
    ticket_types { int Id PK
        string Name }
    tickets { guid Id PK
        string Description
        int TicketTypeId FK }
    employees { guid Id PK
        string FirstName
        string LastName
        string Email
        string Address_Street
        string Address_City
        string Address_Country }
```

- **`BaseAudit`** (base de `Ticket` y `Employee`): `Id` (`Guid`), `CreatedAt`/`CreatedBy`,
  `LastUpdatedAt`/`LastUpdatedBy`, `IsDeleted` y `DeletedAt`/`DeletedBy`. Las fechas de modificación y
  borrado son `DateTime?`.
- **`TicketType`**: catálogo con `Id` entero y sin auditoría.
- **`Address`**: value object (`[ComplexType]`) sin `Id`; sus columnas van en la tabla `employees`. Setters
  privados y constructor público que valida y recorta los valores.
- **Ticket ↔ Employee**: muchos a muchos con la tabla intermedia `ticket_employees`, sin clase propia.
- Tablas en minúsculas y plural: `tickets`, `employees`, `ticket_types`, `ticket_employees`.

## Infraestructura

`TicketDbContext` se registra con `AddDbContext` y `UseNpgsql(PostgresConnection)`. Las configuraciones de
entidad se descubren con `ApplyConfigurationsFromAssembly`.

La cadena `PostgresConnection` no se versiona: `appsettings.json` la declara vacía y se aporta por secretos.
Cómo levantar PostgreSQL y la API, en [*Entorno local*](../entorno-local.md#3-ticketingquery).

## Migraciones

`Program.cs` llama a `context.Database.Migrate()` al arrancar. Para crear una migración:

```bash
dotnet ef migrations add NombreMigracion \
  --project Ticketing.Query.Infrastructure \
  --startup-project Ticketing.Query.Api
```

## Capa de aplicación y API

- `ReadTicketCommand` → `IRequest<Response<bool>>`, con `ReadTicketCommandHandler` y `ReadTicketValidator`.
- `GET api/v1/TicketCreate/ReadTicket` envía el comando por MediatR.

## Pendiente

- Registrar MediatR y los validadores en `AddApplicationServices()`.
- Implementar `ReadTicket` (el handler lanza `NotImplementedException`).
- Consumidor de Kafka que proyecte `TicketCreatedEvent` sobre `tickets` (el productor está en
  [*Mensajería con Kafka*](../mensajeria-kafka.md)).
- Tests de `Ticketing.Query.UnitTest`.
