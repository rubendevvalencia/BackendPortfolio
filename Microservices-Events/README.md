# Microservices-Events — Ticketing

Proyecto de microservicios **dirigidos por eventos** en **.NET 10** con **MongoDB**. Es un proyecto de
aprendizaje independiente del monolito: no forma parte de la migración Strangler Fig, tiene su propia
solución y su propio Docker.

El objetivo no es la cantidad de funcionalidad sino entender, con código que funciona, tres ideas que
suelen explicarse solo en teoría:

- **CQRS a nivel de servicio:** un servicio que escribe (`Ticketing.Command`) y otro que lee
  (`Ticketing.Query`), con proyectos y contratos distintos.
- **Event Sourcing:** el estado no se guarda como una fila que se modifica, sino como una secuencia de
  eventos inmutables en un *event store*.
- **Transacciones en MongoDB:** sesiones y transacciones sobre un replica set, aisladas tras un Unit of
  Work para que la capa de aplicación no conozca el driver.

## Piezas

| Pieza | Qué es |
|---|---|
| `Ticketing.Command` (API, Application, Domain, Infrastructure) | Lado de escritura: `POST api/v1/TicketCreate/CreateTicket` guarda un `TicketCreatedEvent` en el event store |
| `Ticketing.Common` | Contratos compartidos: eventos, `Response<T>` y `ErrorType` |
| `Ticketing.Query.Api` | Lado de lectura |
| Docker | MongoDB 7 como replica set de un nodo |
| CI | `ci-micro-events.yaml`: restaura, compila y ejecuta el proyecto de tests |

## Estructura

```
Microservices-Events/
├── Docker/docker-compose.yml          MongoDB como replica set (rs0)
└── Ticketing/
    ├── Ticketing.slnx
    ├── Ticketing.Core/
    │   └── Ticketing.Common/          Eventos, Message, Response<T>, ErrorType
    ├── Ticketing.Command/
    │   ├── Ticketing.Command.Api/             Controllers, versionado, Swagger
    │   ├── Ticketing.Command.Application/     Commands, handlers, validadores, mapeos
    │   ├── Ticketing.Command.Domain/          EventModel, enums, puertos (interfaces)
    │   ├── Ticketing.Command.Infrastructure/  MongoDB: contexto, sesiones, UoW, repositorios
    │   └── Ticketing.Command.UnitTest/
    └── Ticketing.Query/
        └── Ticketing.Query.Api/
```

## Documentación

| Documento | Qué cuenta |
|---|---|
| [Arquitectura](docs/arquitectura.md) | Capas, dependencias entre proyectos y recorrido completo de una petición |
| [Event store y modelo de eventos](docs/event-store.md) | Cómo se guarda un evento, el mapeo a MongoDB y el polimorfismo con discriminador |
| [Decisiones técnicas](docs/decisiones-tecnicas.md) | Las decisiones que tienen algo que explicar: por qué replica set, por qué UoW sobre Mongo, por qué los mapeos fuera del dominio |
| [Entorno local](docs/entorno-local.md) | Cómo levantar MongoDB, ejecutar la API y probar el endpoint |

## Stack

.NET 10 · ASP.NET Core (controllers) · MongoDB 7 + `MongoDB.Driver` 3.12 · MediatR 14 · FluentValidation 12 ·
AutoMapper 16 · Asp.Versioning 10 · Swashbuckle 10 · xUnit.

## Puesta en marcha rápida

```bash
docker compose -f Microservices-Events/Docker/docker-compose.yml up -d
dotnet run --project Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.Api
```

Los detalles y el porqué de cada paso, en [*Entorno local*](docs/entorno-local.md).
