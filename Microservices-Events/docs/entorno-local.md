# Entorno local

[← Volver al README](../README.md)

Cómo levantar la infraestructura compartida y cada uno de los dos microservicios. Son independientes: puedes
arrancar solo `Ticketing.Command` (necesita MongoDB y Kafka) o solo `Ticketing.Query` (necesita PostgreSQL).

## Requisitos

- .NET SDK 10
- Docker (Docker Desktop en Windows), con soporte de `start_interval` en el *healthcheck* (Docker Engine 25
  o posterior)

## 1. Infraestructura

Un solo compose levanta los almacenes de los dos servicios. Desde la raíz del repo:

```bash
docker compose -f Microservices-Events/Docker/docker-compose.yml up -d
```

Para arrancar solo lo de un servicio, indica los servicios de Compose al final: `... up -d mongodb kafka`
para `Command` (Kafka arrastra a Zookeeper) o `... up -d postgres` para `Query`.

| Servicio de Compose | Lo usa | Contenedor |
|---|---|---|
| `mongodb` | `Ticketing.Command` | `mongodb` |
| `zookeeper`, `kafka` y `kafka-ui` | `Ticketing.Command` (productor); `Ticketing.Query` (consumidor) | `zookeper`, `kafka`, `kafkaui` |
| `postgres` | `Ticketing.Query` | `postgresql` |

**Para parar sin perder los contenedores**, usa `docker compose -f ... stop`. `down` los elimina. Vale para
los cinco contenedores.

### MongoDB

| Elemento | Valor |
|---|---|
| Proyecto de Compose | `docker-micro-events-production` |
| Contenedor | `mongodb`, imagen `mongo:7.0` |
| Comando | `--replSet rs0 --bind_ip_all --port 27017` |
| Puerto | `27017:27017` |
| Red | `microserviceNetwork` |
| Volúmenes con nombre | `mongodb_data` (`/data/db`) y `mongodb_config` (`/data/configdb`), para que los datos sobrevivan a un reinicio |
| `extra_hosts` | `host.docker.internal:host-gateway` |
| Autenticación | Ninguna |

La primera vez, el contenedor tarda unos segundos en pasar a *healthy*: es el *healthcheck* inicializando el
replica set `rs0` (ver [*Decisiones técnicas de Command*](command/decisiones-tecnicas.md#mongodb-como-replica-set-de-un-solo-nodo)).
Lo comprueba cada 5 s, con hasta 30 reintentos. Hasta entonces `Ticketing.Command` no arranca y las
transacciones fallarían.

Comprobación:

```bash
docker ps --filter name=mongodb          # STATUS debe ser "healthy"
docker exec mongodb mongosh --quiet --eval "rs.status().ok"   # 1
```

> El replica set anuncia su miembro como `host.docker.internal:27017`. En Docker Desktop (Windows y macOS)
> ese nombre resuelve desde el host. En un Linux sin Docker Desktop hay que añadirlo a `/etc/hosts`
> apuntando a `127.0.0.1`, o el driver no encontrará el primario aunque `localhost:27017` responda.

### Kafka y su interfaz

Detalle de cada servicio, de los listeners y de cómo se publica en
[*Mensajería con Kafka*](mensajeria-kafka.md).

| Servicio | Contenedor | Puerto en el host | Para qué |
|---|---|---|---|
| Zookeeper | `zookeper` | `2181` | Coordina el broker |
| Kafka | `kafka` | `29092` (la API) y `9092` (otros contenedores) | Broker de un solo nodo |
| Kafka UI | `kafkaui` | `8080` | `http://localhost:8080`: topics y mensajes |

Los tres tardan unos segundos en estar listos. Comprobación:

```bash
docker ps --filter name=kafka --filter name=zookeper     # kafka, kafkaui y zookeper en "Up"
```

Después, en Kafka UI, el cluster `local` debe aparecer *online*. El topic no hace falta crearlo: se crea al
publicar el primer evento.

> Las imágenes de Zookeeper y Kafka son de `bitnamilegacy`. Bitnami retiró las imágenes `bitnami/*` de
> Docker Hub y las dejó archivadas ahí, sin actualizaciones. Vale para desarrollo local, no para producción.

### PostgreSQL

| Elemento | Valor |
|---|---|
| Contenedor | `postgresql`, imagen `postgres:16.0` |
| Puerto en el host | `5432` |
| Base de datos y usuario | `eventspostgres` y `postgresqlsa` |
| Volumen con nombre | `postgres_data` (`/var/lib/postgresql/data`) |

La contraseña no está en el compose: el contenedor la extrae de un `secrets.json` que contiene la cadena
`PostgresConnection`. El compose exige la variable `POSTGRES_SECRETS_DIR` en `Docker/.env`, con la carpeta
donde está ese fichero; sin ella `docker compose up` falla.

## 2. Ticketing.Command

### Configuración

[`appsettings.json`](../Ticketing/Ticketing.Command/Ticketing.Command.Api/appsettings.json) ya apunta al
compose:

| Clave | Valor | Uso |
|---|---|---|
| `ConnectionStrings:MongoDbConnection` | `mongodb://localhost:27017/?replicaSet=rs0` | Conexión al replica set |
| `MongoSettings:Database` | `ticketingApp` | Base de datos donde vive `eventStore` |
| `KafkaSettings:HostName` | `localhost` | Host del broker |
| `KafkaSettings:Port` | `29092` | Puerto del listener externo de Kafka |

Si falta cualquiera de las dos de Mongo o `HostName`/`Port` de Kafka, `AddInfrastructureServices` lanza
`InvalidOperationException` al arrancar (falla rápido, no en la primera petición). El nombre del topic ya no se
configura: es la constante `KafkaTopics.TicketEvents` (`KAFKA_TOPIC`) de `Ticketing.Common`.

`appsettings.Development.json` solo ajusta los niveles de log.

> La cadena de conexión no lleva credenciales porque MongoDB del compose arranca sin autenticación. Es solo
> para desarrollo local; si algún día se añade usuario, va a *User Secrets*, no a `appsettings.json`.

### Ejecutar

Con MongoDB ya *healthy* y Kafka en marcha:

```bash
dotnet run --project Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.Api
```

Antes de aceptar peticiones, la API crea el índice `ux_aggregate_version` (y con él la colección). Si Mongo
no responde, se queda esperando unos 30 s y termina con error. Kafka no se comprueba al arrancar: la API
arranca sin él, pero `CreateTicket` no funcionará (ver
[*Limitaciones*](mensajeria-kafka.md#limitaciones-actuales)).

| Perfil (`launchSettings.json`) | URLs |
|---|---|
| `http` (el de `dotnet run`) | `http://localhost:5166` |
| `https` | `https://localhost:7249` y `http://localhost:5166` |

Los dos arrancan en `Development`. Swagger (solo en `Development`): `http://localhost:5166/swagger`.

### Probar el endpoint

```http
POST http://localhost:5166/api/v1/TicketCreate/CreateTicket
Content-Type: application/json

{
  "userName": "ruben",
  "errorType": "Timeout",
  "errorDetail": "La pasarela de pago no respondió"
}
```

Respuesta esperada (200), con el id del ticket creado en `data`:

```json
{ "data": "019...-....", "isSuccess": true, "message": "", "errors": {}, "errorType": 1 }
```

Y el documento en Mongo, junto con sus índices:

```bash
docker exec mongodb mongosh --quiet ticketingApp --eval "db.eventStore.find().pretty()"
docker exec mongodb mongosh --quiet ticketingApp --eval "db.eventStore.getIndexes()"
```

Y el mismo evento publicado en Kafka: en `http://localhost:8080`, *Topics* → `KAFKA_TOPIC` → *Messages*. O
desde la consola del broker:

```bash
docker exec kafka kafka-console-consumer.sh --bootstrap-server kafka:9092 --topic KAFKA_TOPIC --from-beginning
```

## 3. Ticketing.Query

Con PostgreSQL en marcha (el contenedor `postgresql`):

```bash
dotnet run --project Microservices-Events/Ticketing/Ticketing.Query/Ticketing.Query.Api
```

La cadena `ConnectionStrings:PostgresConnection` va vacía en `appsettings.json`: se aporta por *User Secrets*
(o por `secrets.json`/variables de entorno) y debe coincidir con la contraseña del contenedor. Al arrancar,
la API aplica las migraciones y crea las tablas. Este servicio no necesita MongoDB.

La sección `ConsumerConfig` de `appsettings.json` ya apunta al compose (`localhost:29092`, grupo `TK_CONSUMER`).
`BootstrapServers` y `GroupId` se validan al arrancar. El consumidor se suscribe al topic `KAFKA_TOPIC`, y
**no lo crea** (`AllowAutoCreateTopics = false`): el topic aparece cuando `Command` publica su primer evento. No
he comprobado cómo se comporta el arranque de `Query` con Kafka caído o con el topic aún sin crear.

> **Antes de probar la proyección:** la migración `Initial` está desfasada respecto al modelo (ver
> [*Migraciones*](query/arquitectura.md#migraciones)) y hace falta al menos un empleado cargado en
> `employees` cuyo `Email` coincida con el `userName` del ticket (ver
> [*Cómo probarlo*](query/consumidor-kafka.md#cómo-probarlo)).

| Perfil (`launchSettings.json`) | URLs |
|---|---|
| `http` | `http://localhost:5270` |
| `https` | `https://localhost:7005` y `http://localhost:5270` |

`GET api/v1/TicketCreate/ReadTicket` existe, pero el handler aún no está implementado.

## Tests y CI

```bash
dotnet test Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.UnitTest
```

`ci-micro-events.yaml` se dispara solo si cambia algo dentro de `Microservices-Events/` (o el propio
workflow), en `push` y `pull_request` hacia `dev` y `main`. En `ubuntu-latest` con .NET 10: restaura y
compila `Ticketing.slnx` (los dos servicios) en `Release` y lanza el proyecto de tests de `Command`. Permisos
del token: solo `contents: read`. `Ticketing.Query.UnitTest` solo tiene el test vacío de la plantilla.
