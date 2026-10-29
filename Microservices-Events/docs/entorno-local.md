# Entorno local

[← Volver al README](../README.md)

Cómo levantar la infraestructura y cada microservicio. Son independientes: `Ticketing.Command` necesita MongoDB y
Kafka; `Ticketing.Query` necesita PostgreSQL (y Kafka para proyectar).

**Requisitos:** .NET SDK 10 y Docker (Docker Engine 25 o posterior, por `start_interval` en el *healthcheck*).

## 1. Infraestructura

```bash
docker compose -f Microservices-Events/Docker/docker-compose.yml up -d
```

Para solo un servicio, añade los de Compose al final: `mongodb kafka` para `Command` o `postgres` para `Query`.
Para parar sin perder los contenedores usa `stop`; `down` los elimina.

| Servicio | Contenedor | Puerto | Lo usa |
|---|---|---|---|
| `mongodb` (`mongo:7.0`, `--replSet rs0`) | `mongodb` | `27017` | Command |
| `zookeeper`, `kafka`, `kafka-ui` | `zookeper`, `kafka`, `kafkaui` | `2181`, `29092`/`9092`, `8080` | Command y Query |
| `postgres` (`postgres:16.0`) | `postgresql` | `5432` | Query |

**MongoDB.** Tarda unos segundos en pasar a *healthy*: el *healthcheck* inicializa el replica set `rs0` (ver
[*Decisiones de Command*](command/decisiones-tecnicas.md#mongodb-como-replica-set-de-un-solo-nodo)). Hasta
entonces `Command` no arranca. Comprobación: `docker ps --filter name=mongodb` (debe ser `healthy`). El miembro se
anuncia como `host.docker.internal:27017`: en Docker Desktop resuelve desde el host; en Linux sin él hay que
añadirlo a `/etc/hosts` apuntando a `127.0.0.1`. No hay autenticación (solo desarrollo local).

**Kafka.** Kafka UI en `http://localhost:8080` (cluster `local`). El topic no hace falta crearlo: lo crea el
primer evento publicado. Las imágenes son de `bitnamilegacy` (archivadas, sin actualizaciones): vale para
desarrollo local. Detalle en [*Mensajería con Kafka*](mensajeria-kafka.md).

**PostgreSQL.** Base `eventspostgres`, usuario `postgresqlsa`, volumen `postgres_data`. La contraseña no está en el
compose: sale de un `secrets.json` con la cadena `PostgresConnection`, y el compose exige `POSTGRES_SECRETS_DIR` en
`Docker/.env` con la carpeta de ese fichero (sin ella `docker compose up` falla).

## 2. Ticketing.Command

`appsettings.json` ya apunta al compose:

| Clave | Valor |
|---|---|
| `ConnectionStrings:MongoDbConnection` | `mongodb://localhost:27017/?replicaSet=rs0` |
| `MongoSettings:Database` | `ticketingApp` |
| `KafkaSettings:HostName` / `Port` | `localhost` / `29092` |

Si falta alguna, `AddInfrastructureServices` lanza `InvalidOperationException` al arrancar. El topic no se
configura: es la constante `KafkaTopics.TicketEvents`.

```bash
dotnet run --project Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.Api
```

Antes de aceptar peticiones crea el índice `ux_aggregate_version` (y la colección). Con Mongo caído el arranque
se queda esperando ~30 s y falla; Kafka no se comprueba al arrancar (pero `CreateTicket` no funcionaría sin él).
URLs: `http://localhost:5166` (perfil `http`) y `https://localhost:7249`; Swagger en `/swagger` (solo
`Development`).

### Probar el endpoint

```http
POST http://localhost:5166/api/v1/TicketCreate/CreateTicket
Content-Type: application/json

{ "userName": "ruben", "errorType": "Timeout", "errorDetail": "La pasarela de pago no respondió" }
```

Respuesta 200 con el id del ticket en `data`:

```json
{ "data": "019...-....", "isSuccess": true, "message": "", "errors": {}, "errorType": 1 }
```

Si el body no valida (los tres campos son obligatorios), responde **400** sin llegar al handler ni a Mongo, con
`errorType: 2` y los errores agrupados por propiedad en `errors`. Ante una excepción no controlada responde
**500** con un mensaje genérico; el detalle va solo al log (ver
[*Decisiones*](command/decisiones-tecnicas.md#un-middleware-global-para-las-excepciones)).

Comprobar el resultado:

```bash
docker exec mongodb mongosh --quiet ticketingApp --eval "db.eventStore.find().pretty()"
docker exec kafka kafka-console-consumer.sh --bootstrap-server kafka:9092 --topic KAFKA_TOPIC --from-beginning
```

## 3. Ticketing.Query

```bash
dotnet run --project Microservices-Events/Ticketing/Ticketing.Query/Ticketing.Query.Api
```

- `ConnectionStrings:PostgresConnection` va vacía en `appsettings.json`: se aporta por *User Secrets* o
  `secrets.json` y debe coincidir con la contraseña del contenedor. Al arrancar se aplican las migraciones.
- `ConsumerConfig` ya apunta al compose (`localhost:29092`, grupo `TK_CONSUMER`). El consumidor **no crea** el
  topic: aparece cuando `Command` publica su primer evento.
- URLs: `http://localhost:5270` y `https://localhost:7005`.

> **Antes de probar la proyección:** la migración `Initial` está desfasada (ver
> [*Migraciones*](query/arquitectura.md#migraciones)) y hace falta un empleado en `employees` cuyo `Email`
> coincida con el `userName` del ticket (ver [*Cómo probarlo*](query/consumidor-kafka.md#cómo-probarlo)).

`GET api/v1/Ticket/AllTickets` devuelve los tickets proyectados (`TicketDto`). Peculiaridades: lee `TicketGetQuery`
del **body** aunque sea un `GET`, y el handler exige un `Id` GUID válido sin usarlo para filtrar.

## Tests y CI

```bash
dotnet test Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.UnitTest
```

`Command` tiene tests del middleware, del validador y del servicio de agregado (ver
[*Tests*](command/arquitectura.md#tests)); `Ticketing.Query.UnitTest` solo tiene el test vacío de la plantilla.
`ci-micro-events.yaml` se dispara si cambia algo en `Microservices-Events/` (push y PR hacia `dev` y `main`): en
`ubuntu-latest` restaura, compila `Ticketing.slnx` en `Release` y lanza los tests de `Command`.
