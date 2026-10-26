# Entorno local

[← Volver al README](../README.md)

Cómo levantar el proyecto (MongoDB, Kafka y PostgreSQL) y probar el endpoint de `Ticketing.Command`. El
servicio de consulta y su base de datos tienen su propio apartado: [PostgreSQL y `Ticketing.Query`](#6-postgresql-y-ticketingquery).

## Requisitos

- .NET SDK 10
- Docker (Docker Desktop en Windows), con soporte de `start_interval` en el *healthcheck* (Docker Engine 25
  o posterior)

## 1. MongoDB

Desde la raíz del repo:

```bash
docker compose -f Microservices-Events/Docker/docker-compose.yml up -d
```

Ese comando levanta también Zookeeper, Kafka y Kafka UI (ver el [apartado 2](#2-kafka-y-su-interfaz)) y
PostgreSQL (ver el [apartado 6](#6-postgresql-y-ticketingquery)). Para arrancar solo MongoDB:
`... up -d mongodb`.

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
replica set `rs0` (ver [*Decisiones técnicas*](decisiones-tecnicas.md#mongodb-como-replica-set-de-un-solo-nodo)).
Lo comprueba cada 5 s, con hasta 30 reintentos. Hasta entonces la API no arranca y las transacciones
fallarían.

Comprobación:

```bash
docker ps --filter name=mongodb          # STATUS debe ser "healthy"
docker exec mongodb mongosh --quiet --eval "rs.status().ok"   # 1
```

**Para parar sin perder los contenedores**, usa `docker compose -f ... stop`. `down` los elimina. Vale para
los cuatro servicios.

> El replica set anuncia su miembro como `host.docker.internal:27017`. En Docker Desktop (Windows y macOS)
> ese nombre resuelve desde el host. En un Linux sin Docker Desktop hay que añadirlo a `/etc/hosts`
> apuntando a `127.0.0.1`, o el driver no encontrará el primario aunque `localhost:27017` responda.

## 2. Kafka y su interfaz

Los levanta el mismo `docker compose ... up -d`. Detalle de cada servicio, de los listeners y de cómo se
publica en [*Mensajería con Kafka*](mensajeria-kafka.md).

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

## 3. Configuración de la API

[`appsettings.json`](../Ticketing/Ticketing.Command/Ticketing.Command.Api/appsettings.json) ya apunta al
compose:

| Clave | Valor | Uso |
|---|---|---|
| `ConnectionStrings:MongoDbConnection` | `mongodb://localhost:27017/?replicaSet=rs0` | Conexión al replica set |
| `MongoSettings:Database` | `ticketingApp` | Base de datos donde vive `eventStore` |
| `KafkaSettings:HostName` | `localhost` | Host del broker |
| `KafkaSettings:Port` | `29092` | Puerto del listener externo de Kafka |
| `KafkaSettings:Topic` | `KAFKA_TOPIC` | Topic al que se publican los eventos |

Si falta cualquiera de las dos de Mongo o `HostName`/`Port` de Kafka, `AddInfrastructureServices` lanza
`InvalidOperationException` al arrancar (falla rápido, no en la primera petición). `Topic` no se valida al
arrancar: si falta, falla al guardar el primer evento.

`appsettings.Development.json` solo ajusta los niveles de log.

> La cadena de conexión no lleva credenciales porque MongoDB del compose arranca sin autenticación. Es solo
> para desarrollo local; si algún día se añade usuario, va a *User Secrets*, no a `appsettings.json`.

## 4. Ejecutar

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

## 5. Probar el endpoint

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

Y el mismo evento publicado en Kafka: en `http://localhost:8080`, *Topics* → el valor de
`KafkaSettings:Topic` → *Messages*. O desde la consola del broker:

```bash
docker exec kafka kafka-console-consumer.sh --bootstrap-server kafka:9092 --topic KAFKA_TOPIC --from-beginning
```

## 6. PostgreSQL y Ticketing.Query

PostgreSQL lo levanta el mismo compose y es la base del servicio de consulta.

| Elemento | Valor |
|---|---|
| Contenedor | `postgresql`, imagen `postgres:16.0` |
| Puerto en el host | `5432` |
| Base de datos y usuario | `eventspostgres` y `postgresqlsa` |
| Volumen con nombre | `postgres_data` (`/var/lib/postgresql/data`) |

La contraseña no está en el compose: el contenedor la extrae de un `secrets.json` que contiene la cadena
`PostgresConnection`. El compose exige la variable `POSTGRES_SECRETS_DIR` en `Docker/.env`, con la carpeta
donde está ese fichero; sin ella `docker compose up` falla.

Para arrancar la API de consulta, con PostgreSQL en marcha:

```bash
dotnet run --project Microservices-Events/Ticketing/Ticketing.Query/Ticketing.Query.Api
```

La cadena `ConnectionStrings:PostgresConnection` va vacía en `appsettings.json`: se aporta por *User Secrets*
(o por `secrets.json`/variables de entorno) y debe coincidir con la contraseña del contenedor. Al arrancar,
la API aplica las migraciones y crea las tablas. Perfiles y puertos:
`Ticketing.Query/Ticketing.Query.Api/Properties/launchSettings.json`.

## Tests y CI

```bash
dotnet test Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.UnitTest
```

`ci-micro-events.yaml` se dispara solo si cambia algo dentro de `Microservices-Events/` (o el propio
workflow), en `push` y `pull_request` hacia `dev` y `main`. En `ubuntu-latest` con .NET 10: restaura y
compila `Ticketing.slnx` en `Release` y lanza ese proyecto de tests. Permisos del token: solo
`contents: read`.
