# Entorno local

[← Volver al README](../README.md)

Cómo levantar el proyecto y probar el único endpoint que existe hoy.

## Requisitos

- .NET SDK 10
- Docker (Docker Desktop en Windows), con soporte de `start_interval` en el *healthcheck* (Docker Engine 25
  o posterior)

## 1. MongoDB

Desde la raíz del repo:

```bash
docker compose -f Microservices-Events/Docker/docker-compose.yml up -d
```

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

**Para parar sin perder los contenedores**, usa `docker compose -f ... stop`. `down` los elimina.

> El replica set anuncia su miembro como `host.docker.internal:27017`. En Docker Desktop (Windows y macOS)
> ese nombre resuelve desde el host. En un Linux sin Docker Desktop hay que añadirlo a `/etc/hosts`
> apuntando a `127.0.0.1`, o el driver no encontrará el primario aunque `localhost:27017` responda.

## 2. Configuración de la API

[`appsettings.json`](../Ticketing/Ticketing.Command/Ticketing.Command.Api/appsettings.json) ya apunta al
compose:

| Clave | Valor | Uso |
|---|---|---|
| `ConnectionStrings:MongoDbConnection` | `mongodb://localhost:27017/?replicaSet=rs0` | Conexión al replica set |
| `MongoSettings:Database` | `ticketingApp` | Base de datos donde vive `eventStore` |

Si falta cualquiera de las dos primeras, `AddInfrastructureServices` lanza `InvalidOperationException` al
arrancar (falla rápido, no en la primera petición).

`appsettings.Development.json` solo ajusta los niveles de log.

> La cadena de conexión no lleva credenciales porque MongoDB del compose arranca sin autenticación. Es solo
> para desarrollo local; si algún día se añade usuario, va a *User Secrets*, no a `appsettings.json`.

## 3. Ejecutar

Con MongoDB ya *healthy*:

```bash
dotnet run --project Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.Api
```

Antes de aceptar peticiones, la API crea el índice `ux_aggregate_version` (y con él la colección). Si Mongo
no responde, se queda esperando unos 30 s y termina con error.

| Perfil (`launchSettings.json`) | URLs |
|---|---|
| `http` (el de `dotnet run`) | `http://localhost:5166` |
| `https` | `https://localhost:7249` y `http://localhost:5166` |

Los dos arrancan en `Development`. Swagger (solo en `Development`): `http://localhost:5166/swagger`.

## 4. Probar el endpoint

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

## Tests y CI

```bash
dotnet test Microservices-Events/Ticketing/Ticketing.Command/Ticketing.Command.UnitTest
```

`ci-micro-events.yaml` se dispara solo si cambia algo dentro de `Microservices-Events/` (o el propio
workflow), en `push` y `pull_request` hacia `dev` y `main`. En `ubuntu-latest` con .NET 10: restaura y
compila `Ticketing.slnx` en `Release` y lanza ese proyecto de tests. Permisos del token: solo
`contents: read`.
